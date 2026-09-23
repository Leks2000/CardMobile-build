# MobileDundgeon (CardMobile / Dungeon Card Game)

Unity 6000.3.23f1 (6.3 LTS), URP, uGUI + TextMeshPro, DOTween (Assets/Plugins/Demigiant).
Do NOT upgrade Unity or add unrelated packages.

## Live Editor connection (always use this, never hand-edit .unity/.prefab YAML)

- Bridge package: `com.unity.pipeline` (already in Packages/manifest.json).
- MCP: `unity-editor-mcp` is registered in Claude Code (user scope), pinned to this project.
- The user runs everything as Administrator; the Editor must be launched from an elevated shell too:
  `unity open "E:\PROjECTS\Games\Unity\MobileDundgeon"` then wait for `unity status` → state `ready` (~60s).
- If `unity status` shows nothing but Unity is open: run `unity pipeline list`.
  `safeMode: true` → fix compile errors in .cs. Package not resolved → click into the Unity window (manifest is re-read on focus).

Useful commands (run `unity command` for the full list):

```bash
unity status
unity command console                      # errors/warnings + compile state
unity command recompile ; unity command recompile_status
unity command open_scene --path Assets/Resources/Scenes/Scene/EnemyScene.unity
unity command get_scene_hierarchy
unity command editor_play / editor_stop / editor_status
unity command capture_game_view
unity command eval 'return UnityEngine.Application.isPlaying;'
```

Run `eval` from **Bash**, not PowerShell (PowerShell mangles the quotes). For long C#, use `eval_file`.

## Layout

- Scripts: `Assets/Scrpits/` (sic) — Card/, GameManagers/, Turn/, Location/, Utility/
- Data (ScriptableObjects + their classes): `Assets/Resources/CardData/`, `Assets/Resources/ScriptableObjects/`
- Scenes: `Assets/Resources/Scenes/Scene/` — MainMenuScene, EnemyScene (battle), CardShopScene
- Card prefabs: `Assets/Resources/Objects/`
