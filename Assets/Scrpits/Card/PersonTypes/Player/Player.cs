using Assets.Scrpits.Card.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private DamageTextAnimator damageAnimator;

    public TMP_Text takeDamageText;
    public TMP_Text playerHpText;
    public static Player Instance { get; private set; }
    private HpBarView hpBar; // [V]

    /// <summary>[V] Read-only HP for HUD.</summary>
    public int HP => playerData != null ? playerData.playerHP : 0;
    public int MaxHP => playerData != null ? playerData.playerHPMAX : 0;

    private void Awake()
    {
        Instance = this;

        if (playerData != null)
        {
            playerData = playerData.Clone();
        }
        hpBar = GetComponentInChildren<HpBarView>(true);
        UpdatePlayerDisplay();
        if (damageAnimator != null && playerHpText != null)
        {
            // Hit feedback on the HP plate (parent Image of the HP text).
            var plate = playerHpText.transform.parent;
            damageAnimator.SetHitTargets(plate, plate != null ? plate.GetComponent<Image>() : null);
        }
    }

    public void UpdatePlayerDisplay()
    {
        if (hpBar != null) hpBar.Set(playerData.playerHP, playerData.playerHPMAX); // [V] bar + "hp/max" label
        else playerHpText.text = "HP: " + (playerData.playerHP + "/" + playerData.playerHPMAX).ToString();
    }

    public void TakeDamage(int damage)
    {
        takeDamageText.text = "-" + damage.ToString();
        playerData.ApplyDamage(damage);
        UpdatePlayerDisplay();

        damageAnimator.Animate(takeDamageText, damage);
        CombatFx.ScreenFlash(CombatFx.DamageRed, 0.22f, 0.35f);
    }

    public bool IsDefeated()
    {
        return playerData.playerHP <= 0;
    }
}
