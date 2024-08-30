using TMPro;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [SerializeField] BossData bossData;
    [SerializeField] GameManagerOver gameManager;
    private TMP_Text bossName;
    private TMP_Text cardHp;

    private void Awake()
    {
        if (bossData != null)
        {
            bossData = bossData.Clone();
        }
        cardHp = transform.Find("BossHP/HP").GetComponent<TMP_Text>();
        UpdateCardDisplay();
    }
    public void UpdateCardDisplay()
    {
        cardHp.text = ("HP: " + bossData.bossHP.ToString());
    }
    public void TakeDamage(int damage)
    {
        bossData.ApplyDamage(damage);
        UpdateCardDisplay();
        if (bossData.bossHP <= 0)
        {
            gameManager.GameOver();
        }
    }
}
