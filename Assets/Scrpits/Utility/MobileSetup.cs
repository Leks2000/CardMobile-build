using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Настройки под телефон, применяются ко всем сценам без правки .unity:
/// - только альбомная ориентация, 60 FPS, экран не гаснет, один палец (без мультитача);
/// - порог перетаскивания в физических мм (тап по карте на телефоне с высоким DPI не становится drag);
/// - масштаб UI: все экранные канвасы - ScaleWithScreenSize 1920x1080 + Expand
///   (на вытянутых 19.5:9 / 20:9 и на планшетах 4:3 интерфейс влезает целиком, ничего не наезжает);
/// - safe area: элементы у краёв экрана отодвигаются от выреза камеры / закруглений / полоски «домой».
/// </summary>
public static class MobileSetup
{
    private static readonly Vector2 Reference = new Vector2(1920, 1080);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Input.multiTouchEnabled = false; // второй палец не хватает вторую карту
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.AutoRotation;

        var go = new GameObject("[MobileSetup]");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy;
        go.AddComponent<MobileSetupRunner>();
    }

    /// <summary>Порог drag ~2 мм: на телефоне 10 px по умолчанию - это доли миллиметра.</summary>
    public static void TuneEventSystem()
    {
        var es = EventSystem.current != null ? EventSystem.current : Object.FindAnyObjectByType<EventSystem>();
        if (es == null) return;
        float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
        es.pixelDragThreshold = Mathf.Max(10, Mathf.RoundToInt(dpi * 0.08f));
    }

    /// <summary>Один экранный канвас: масштаб + safe area. Повторный вызов безопасен.</summary>
    public static void Fit(Canvas canvas)
    {
        if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) return;
        var n = canvas.name;
        if (n == "[SceneFade]") return;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            // ConstantPixelSize (экран результата) на телефоне с высоким разрешением становится крошечным
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize && Mathf.Approximately(scaler.scaleFactor, 1f))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = Reference;
            }
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                // портретный референс (карта) при альбомной игре сжимал высоту на широких экранах
                if (scaler.referenceResolution.x < scaler.referenceResolution.y) scaler.referenceResolution = Reference;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }

        // атмосфера / всплывающие числа позиционируются в экранных координатах - их не двигаем
        if (n == "V_Atmosphere" || n == "V_DamagePops") return;
        var sa = canvas.GetComponent<SafeAreaNudge>();
        if (sa == null) canvas.gameObject.AddComponent<SafeAreaNudge>();
    }
}

/// <summary>Живёт между сценами: после загрузки сцены (и затем периодически) настраивает новые канвасы.</summary>
public class MobileSetupRunner : MonoBehaviour
{
    private float next;
    private int frames;

    private void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;

    private void OnLoaded(Scene s, LoadSceneMode m)
    {
        // сразу (до Start сцены - меньше «прыжков» раскладки) и ещё раз через пару кадров,
        // когда UI, построенный кодом в Start, уже на месте
        FitAll();
        frames = 2;
        next = 0f;
    }

    private static void FitAll()
    {
        MobileSetup.TuneEventSystem();
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            MobileSetup.Fit(c);
    }

    private void Update()
    {
        if (frames > 0) { frames--; if (frames > 0) return; }
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 1f; // канвасы, созданные позже (всплывашки, меню)
        FitAll();
    }
}

/// <summary>
/// Safe area без перестройки иерархии: прямые дети канваса (и корня магазина ShopRoot), прижатые к краю,
/// сдвигаются внутрь на ширину выреза; растянутые вдоль края - поджимаются. Полноэкранные фоны,
/// затемнения и контейнеры не трогаются. Новый элемент сдвигается один раз, когда появился; при повороте
/// экрана всё пересчитывается от исходных позиций.
/// </summary>
public class SafeAreaNudge : MonoBehaviour
{
    private struct Orig { public Vector2 pos, size; }

    private static readonly HashSet<string> Containers = new HashSet<string> { "ShopRoot" };

    private readonly Dictionary<RectTransform, Orig> orig = new Dictionary<RectTransform, Orig>();
    private Rect lastSafe;
    private Vector2Int lastScreen;
    private float nextScan;
    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        Apply(true);
    }

    private void LateUpdate()
    {
        bool changed = Screen.safeArea != lastSafe || Screen.width != lastScreen.x || Screen.height != lastScreen.y;
        if (changed) { Apply(true); return; }
        if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 0.25f; Apply(false); }
    }

    /// <param name="all">true - пересчитать всё от исходных позиций (поворот), false - только новые элементы.</param>
    private void Apply(bool all)
    {
        if (canvas == null) return;
        lastSafe = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        float k = canvas.scaleFactor > 0 ? canvas.scaleFactor : 1f;
        var safe = Screen.safeArea;
        var insets = new Vector4(safe.xMin / k, (Screen.width - safe.xMax) / k, safe.yMin / k, (Screen.height - safe.yMax) / k);
        if (all)
        {
            var dead = new List<RectTransform>();
            foreach (var kv in orig) if (kv.Key == null) dead.Add(kv.Key);
            foreach (var d in dead) orig.Remove(d);
        }
        Process(transform, insets, all);
    }

    private void Process(Transform parent, Vector4 insets, bool all)
    {
        foreach (Transform child in parent)
        {
            if (!(child is RectTransform rt)) continue;
            if (Containers.Contains(rt.name)) { Process(rt, insets, all); continue; }
            bool known = orig.TryGetValue(rt, out var o);
            if (known && !all) continue;
            if (!known)
            {
                o = new Orig { pos = rt.anchoredPosition, size = rt.sizeDelta };
                orig[rt] = o;
            }
            Nudge(rt, o, insets);
        }
    }

    private static void Nudge(RectTransform rt, Orig o, Vector4 insets)
    {
        float left = insets.x, right = insets.y, bottom = insets.z, top = insets.w;
        bool stretchX = rt.anchorMin.x <= 0.01f && rt.anchorMax.x >= 0.99f;
        bool stretchY = rt.anchorMin.y <= 0.01f && rt.anchorMax.y >= 0.99f;
        if (stretchX && stretchY) return; // фон / затемнение / область на весь экран

        Vector2 pos = o.pos, size = o.size;
        if (stretchX) { pos.x += (left - right) * 0.5f; size.x -= left + right; }
        else if (rt.anchorMax.x <= 0.34f) pos.x += left;
        else if (rt.anchorMin.x >= 0.66f) pos.x -= right;

        if (stretchY) { pos.y += (bottom - top) * 0.5f; size.y -= bottom + top; }
        else if (rt.anchorMax.y <= 0.34f) pos.y += bottom;
        else if (rt.anchorMin.y >= 0.66f) pos.y -= top;

        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
