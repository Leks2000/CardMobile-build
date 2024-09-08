using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    [SerializeField] BossData bossData;
    [SerializeField] GameManagerOver gameManager;

    public TMP_Text takeDamage;
    public Image bossImage;

    private TMP_Text bossName;
    private TMP_Text cardHp;

    public float duration;
    public float moveDistance;

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
        AnimateDamageText(() =>
        {
            bossData.ApplyDamage(damage);

            UpdateCardDisplay();

            if (bossData.bossHP <= 0)
            {
                gameManager.GameOver();
            }
        });
    }
    private void AnimateDamageText(TweenCallback onCompleteCallback)
    {
        takeDamage.enabled = true;

        Vector3 initialPosition = takeDamage.transform.localPosition;

        Sequence damageSequence = DOTween.Sequence();

        damageSequence.Append(takeDamage.transform.DOLocalMoveY(takeDamage.transform.localPosition.y - moveDistance, duration).SetEase(Ease.OutQuad));
        damageSequence.Join(takeDamage.DOFade(0, duration).SetDelay(0.5f));

        damageSequence.OnComplete(() =>
        {
            takeDamage.enabled = false;
            takeDamage.alpha = 1;
            takeDamage.transform.localScale = Vector3.one;
            takeDamage.transform.localPosition = initialPosition;
            onCompleteCallback?.Invoke();
        });
    }

    private void AnimateHitReaction()
    {
        Sequence hitSequence = DOTween.Sequence();

        hitSequence.Join(bossImage.transform.DOShakePosition(0.3f, strength: new Vector3(0.2f, 0.2f, 0), vibrato: 10, randomness: 90));

        hitSequence.Join(bossImage.transform.DOScale(new Vector3(0.9f, 0.9f, 1f), 0.1f));
        hitSequence.Append(bossImage.transform.DOScale(Vector3.one, 0.1f));
    }
}
