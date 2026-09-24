using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Плавный переход между сценами: затемнение поверх всего UI -> действие (загрузка сцены) ->
/// после загрузки новой сцены затемнение само растворяется. Живёт между сценами, пока идёт переход.
/// </summary>
public static class SceneFade
{
    private static Image black;

    /// <summary>Затемнить экран за <paramref name="duration"/> и выполнить <paramref name="then"/>.</summary>
    public static void Out(float duration, Action then)
    {
        var img = Ensure();
        img.raycastTarget = true; // во время перехода клики не проходят
        img.DOKill();
        img.DOFade(1f, duration).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() =>
        {
            try { then?.Invoke(); }
            catch (Exception e)
            {
                // переход сорвался - не оставлять игрока с чёрным экраном
                Debug.LogException(e);
                img.raycastTarget = false;
                img.DOFade(0f, 0.3f).SetUpdate(true);
            }
        });
    }

    private static Image Ensure()
    {
        if (black != null) return black;
        var go = new GameObject("[SceneFade]", typeof(RectTransform));
        UnityEngine.Object.DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        go.AddComponent<GraphicRaycaster>();
        var imgGo = new GameObject("Black", typeof(RectTransform));
        imgGo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        black = imgGo.AddComponent<Image>();
        black.color = new Color(0, 0, 0, 0);
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        return black;
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (black == null) return;
        var img = black;
        img.DOKill();
        img.raycastTarget = false; // сцена загружена - клики уже не блокируем
        img.DOFade(0f, 0.45f).SetDelay(0.05f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() =>
        {
            SceneManager.sceneLoaded -= OnLoaded;
            if (img != null) UnityEngine.Object.Destroy(img.transform.parent.gameObject);
            black = null;
        });
    }
}
