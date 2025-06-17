using Assets.Scrpits.Card.Animation;
using DG.Tweening.Core.Easing;
using TMPro;
using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private DamageTextAnimator damageAnimator;

    public TMP_Text takeDamageText;
    public TMP_Text playerHpText;
    public static Player Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        playerData = PlayerDataRuntime.Instance.CurrentData;
        UpdatePlayerDisplay();
    }


    public void UpdatePlayerDisplay()
    {
        playerHpText.text = "HP: " + (playerData.playerHP + "/" + playerData.playerHPMAX).ToString();
    }

    public void TakeDamage(int damage)
    {
        takeDamageText.text = "-" + damage.ToString();
        playerData.ApplyDamage(damage);
        UpdatePlayerDisplay();

        damageAnimator.Animate(takeDamageText, damage);
    }

    public bool IsDefeated()
    {
        return playerData.playerHP <= 0;
    }
}
