using Assets.Scrpits.Card.Animation;
using TMPro;
using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameManagerOver gameManager;
    [SerializeField] private DamageTextAnimator damageAnimator;

    public TMP_Text takeDamageText;
    public TMP_Text playerHpText;
    public static Player Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (playerData != null)
        {
            playerData = playerData.Clone();
        }
        UpdatePlayerDisplay();
    }

    public void UpdatePlayerDisplay()
    {
        playerHpText.text = "HP: " + playerData.playerHP.ToString();
    }

    public void TakeDamage(int damage)
    {
        takeDamageText.text = "-" + damage.ToString();
        playerData.ApplyDamage(damage);
        UpdatePlayerDisplay();

        damageAnimator.Animate(takeDamageText, damage,
        () =>
        {
            if (playerData.playerHP <= 0)
            {
                gameManager.GameOver(false); // или gameManager.LoseGame();
            }
        });
    }

}
