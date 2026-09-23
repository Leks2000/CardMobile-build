using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;

/// <summary>
/// Extra victory/defeat punch for the result panel, layered on top of GameManagerOver's own animation
/// (does not modify GameManagerOver). Sits on the GameManagerOver object and watches its result panel.
/// </summary>
public class ResultPanelFx : MonoBehaviour
{
    [Tooltip("Result panel; auto-resolved from GameManagerOver.resPanel if empty.")]
    public GameObject panel;
    [Tooltip("Result title; auto-resolved from GameManagerOver.resultGame if empty.")]
    public TMP_Text title;
    [Tooltip("Wait for GameManagerOver's drop-in bounce before punching.")]
    public float delay = 1.3f;

    private bool wasActive;

    private void Awake()
    {
        var gm = GetComponent<GameManagerOver>();
        if (gm != null)
        {
            const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            if (panel == null) panel = typeof(GameManagerOver).GetField("resPanel", f)?.GetValue(gm) as GameObject;
            if (title == null) title = typeof(GameManagerOver).GetField("resultGame", f)?.GetValue(gm) as TMP_Text;
        }
        wasActive = panel != null && panel.activeInHierarchy;
    }

    private void Update()
    {
        if (panel == null) return;
        bool active = panel.activeInHierarchy;
        if (active && !wasActive) StartCoroutine(Play());
        wasActive = active;
    }

    private IEnumerator Play()
    {
        yield return null; // title text is assigned right after SetActive(true)
        bool victory = title != null && title.text.ToUpperInvariant().Contains("VICTORY");
        CombatFx.ScreenFlash(victory ? CombatFx.VictoryGold : CombatFx.DamageRed, victory ? 0.35f : 0.3f, 0.5f);
        yield return new WaitForSeconds(delay);
        if (title == null || !title.gameObject.activeInHierarchy) yield break;

        Color baseColor = title.color;
        Color glow = victory ? CombatFx.VictoryGold : new Color(0.75f, 0.2f, 0.2f, 1f);
        CombatFx.Punch(title.transform, victory ? 0.3f : 0.15f, 0.45f);
        if (victory) CombatFx.ScreenFlash(Color.white, 0.2f, 0.35f);

        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            if (title == null) yield break;
            title.color = Color.Lerp(glow, baseColor, t / 0.6f);
            yield return null;
        }
        if (title != null) title.color = baseColor;
    }
}
