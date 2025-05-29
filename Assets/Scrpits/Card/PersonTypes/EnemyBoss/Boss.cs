using Assets.Scrpits.Card.Animation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Класс, представляющий босса в игре. Управляет отображением данных карточки и взаимодействиями.
/// </summary>
/// <remarks>Обновление визуала <see cref="UpdateCardDisplay"/>, получение урона <see cref="TakeDamage"/></remarks>
public class Boss : MonoBehaviour
{
    [SerializeField] BossData bossData;
    [SerializeField] private DamageTextAnimator damageAnimator;

    public TMP_Text takeDamage;
    public Image bossImage;

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
        takeDamage.text = "-" + damage.ToString();
        bossData.ApplyDamage(damage);
        UpdateCardDisplay();
        damageAnimator.Animate(takeDamage, damage);
    }

    public bool IsDefeated()
    {
        return bossData.bossHP <= 0;
    }

}
