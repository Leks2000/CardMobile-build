using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attaches the juice components to scene objects at runtime (idempotent, only adds when missing).
/// Done in code rather than baked into scenes so it survives concurrent scene edits and also covers
/// objects spawned later by scene loads. Nothing here changes gameplay.
/// </summary>
public static class JuiceInstaller
{
    public static bool Enabled = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

    public static void Install()
    {
        if (!Enabled) return;

        // Board slots the player drops cards into.
        foreach (var drop in Object.FindObjectsByType<DropCard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (drop.GetComponent<SlotFx>() == null) drop.gameObject.AddComponent<SlotFx>().mode = SlotFx.Mode.Drop;
        }

        // Enemy spawn slots (EnemySpawnCardLogic spawns into its children named "Image").
        foreach (var spawner in Object.FindObjectsByType<EnemySpawnCardLogic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (Transform slot in spawner.transform)
            {
                if (slot.name == "Image" && slot.GetComponent<SlotFx>() == null)
                {
                    var fx = slot.gameObject.AddComponent<SlotFx>();
                    fx.mode = SlotFx.Mode.Spawn;
                }
            }
        }

        // Tap feedback on every uGUI Button and on Button_UI (End Turn, deck...).
        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.GetComponent<ButtonPunch>() == null) b.gameObject.AddComponent<ButtonPunch>();
        }
        foreach (var b in Object.FindObjectsByType<Button_UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.GetComponent<ButtonPunch>() == null) b.gameObject.AddComponent<ButtonPunch>();
        }

        // Turn banners.
        foreach (var etc in Object.FindObjectsByType<EndTurnCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (etc.GetComponent<TurnBannerWatcher>() == null) etc.gameObject.AddComponent<TurnBannerWatcher>();
        }

        // [V] Victory / defeat presentation now lives in ResultOverlayView (added by GameManagerOver).

        // [V] Card draw trail on the hand root.
        foreach (var gcm in Object.FindObjectsByType<GameControlManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (gcm.playerDeck != null && gcm.playerDeck.GetComponent<HandDrawFx>() == null) gcm.playerDeck.gameObject.AddComponent<HandDrawFx>();
        }
    }
}
