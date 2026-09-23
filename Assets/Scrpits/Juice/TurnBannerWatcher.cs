using System.Reflection;
using UnityEngine;

/// <summary>
/// Shows "YOUR TURN" / "ENEMY TURN" banners without touching turn logic:
/// watches <see cref="EndTurnCamera"/>'s private <c>hasClicked</c> flag
/// (false→true = end turn pressed, true→false = new player turn after TurnRound()).
/// If the field ever disappears, it silently disables itself; call CombatFx.TurnBanner directly instead.
/// </summary>
public class TurnBannerWatcher : MonoBehaviour
{
    public string playerTurnText = "YOUR TURN";
    public string enemyTurnText = "ENEMY TURN";
    public Color playerTurnColor = new Color(0.55f, 1f, 0.6f, 1f);
    public Color enemyTurnColor = new Color(1f, 0.5f, 0.45f, 1f);
    public float startDelay = 0.6f;

    private EndTurnCamera endTurn;
    private FieldInfo clickedField;
    private bool last;
    private float startTimer;
    private bool startShown;

    private void Awake()
    {
        endTurn = GetComponent<EndTurnCamera>();
        if (endTurn == null) endTurn = FindAnyObjectByType<EndTurnCamera>();
        if (endTurn != null)
        {
            clickedField = typeof(EndTurnCamera).GetField("hasClicked", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }
        if (clickedField == null || clickedField.FieldType != typeof(bool))
        {
            clickedField = null;
        }
        startTimer = startDelay;
    }

    private void Update()
    {
        if (!startShown)
        {
            startTimer -= Time.deltaTime;
            if (startTimer <= 0f)
            {
                startShown = true;
                CombatFx.TurnBanner(playerTurnText, playerTurnColor);
            }
        }

        if (clickedField == null || endTurn == null) return;
        bool now = (bool)clickedField.GetValue(endTurn);
        if (now != last)
        {
            if (now) CombatFx.TurnBanner(enemyTurnText, enemyTurnColor);
            else CombatFx.TurnBanner(playerTurnText, playerTurnColor);
            last = now;
        }
    }
}
