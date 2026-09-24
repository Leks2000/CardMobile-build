using Assets.Scrpits.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scrpits.Shop
{
    /// <summary>
    /// Поднимает магазин в CardShopScene кодом: Canvas (1920x1080) + EventSystem + <see cref="CaseShopController"/>,
    /// если их нет в сцене. Сцена остаётся «пустой» (камера + фон), весь UI строит контроллер.
    /// </summary>
    public static class ShopSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != RunState.ShopSceneName) return;
            Time.timeScale = 1f;
            if (Object.FindAnyObjectByType<CaseShopController>() != null) return;

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                SceneManager.MoveGameObjectToScene(es, scene);
            }

            var canvasGo = new GameObject("ShopCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("ShopRoot", typeof(RectTransform));
            rootGo.layer = 5;
            var rt = (RectTransform)rootGo.transform;
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rootGo.AddComponent<CaseShopController>();
            Debug.Log("[SHOP] Shop UI built at runtime");
        }
    }
}
