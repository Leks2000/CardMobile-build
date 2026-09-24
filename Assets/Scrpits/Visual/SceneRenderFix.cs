using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Чинит свет из старых сцен под URP: у Light без UniversalAdditionalLightData, но с cookie,
/// URP падает в LightCookieManager (NullReferenceException -> "Render Graph Execution error" -> чёрный экран,
/// так было в CardShopScene). Добавляем данные света и снимаем cookie (в игре они не нужны).
/// </summary>
public static class SceneRenderFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FixLights();

    public static void FixLights()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.cookie != null)
            {
                Debug.Log($"[FIX] Removed cookie from light '{light.name}' ({light.gameObject.scene.name})");
                light.cookie = null;
            }
            if (!light.TryGetComponent<UniversalAdditionalLightData>(out _))
            {
                light.gameObject.AddComponent<UniversalAdditionalLightData>();
            }
        }
    }
}
