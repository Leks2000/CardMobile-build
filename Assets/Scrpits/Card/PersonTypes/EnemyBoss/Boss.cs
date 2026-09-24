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
    private BossView view; // [V] presentation (aura, float, HP bar, hit reaction)

    /// <summary>[V] Read-only access to the (cloned) boss data.</summary>
    public BossData Data => bossData;
    public float HP => bossData != null ? bossData.bossHP : 0f;
    public float MaxHP => bossData == null ? 0f : (bossData.maxHP > 0 ? bossData.maxHP : Mathf.Max(bossData.bossHP, maxSeen));
    private float maxSeen;

    private void Awake()
    {
        if (bossData != null)
        {
            bossData = bossData.Clone();
        }
        cardHp = transform.Find("BossHP/HP").GetComponent<TMP_Text>();
        view = GetComponent<BossView>();
        if (view != null) view.ApplyData(bossData);
        UpdateCardDisplay();
        if (damageAnimator != null && bossImage != null)
        {
            damageAnimator.SetHitTargets(bossImage.transform, bossImage);
        }
    }
    public void UpdateCardDisplay()
    {
        if (bossData == null) return;
        maxSeen = Mathf.Max(maxSeen, bossData.bossHP);
        if (view != null) view.SetHp(bossData.bossHP, MaxHP); // bar + "hp/max" label
        else cardHp.text = ("HP: " + bossData.bossHP.ToString());
    }

    /// <summary>[V] Sent by Encounters after it swaps in the node's BossData.</summary>
    private void OnBossDataChanged()
    {
        maxSeen = 0f;
        if (view != null) { view.ApplyData(bossData); view.SetHp(HP, MaxHP, false); }
    }
    public void TakeDamage(int damage)
    {
        takeDamage.text = "-" + damage.ToString();
        bossData.ApplyDamage(damage);
        UpdateCardDisplay();
        damageAnimator.Animate(takeDamage, damage);
        if (view != null) view.PlayHit(damage);
        else if (cardHp != null) CombatFx.Punch(cardHp.transform.parent, 0.12f, 0.25f);
        if (bossData.bossHP < 0 && bossImage != null)
        {
            // [Rewards] сверхурон = доп. монеты после боя
            ImpactFx.Sparkle(bossImage.transform, UiTheme.Accent, 6 + Mathf.Min(10, damage), 1.2f);
        }
        if (IsDefeated() && bossImage != null)
        {
            CombatFx.ScreenFlash(Color.white, 0.3f, 0.4f);
            if (view != null) view.PlayDeath();
        }
    }

    public bool IsDefeated()
    {
        return bossData.bossHP <= 0;
    }

}
