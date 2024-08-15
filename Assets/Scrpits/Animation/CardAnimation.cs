using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CardAnimation : MonoBehaviour
{

    private RectTransform cardRectTransform;
    [SerializeField] private float animationDuration = 0.5f;

    private void Awake()
    {
        cardRectTransform = GetComponent<RectTransform>();
    }

    public IEnumerator AnimateCardCoroutine(Transform cardTr)
    {
        Vector3 endPosition = cardRectTransform.position;
        gameObject.transform.SetParent(cardTr);
        cardRectTransform.rotation = Quaternion.identity;
        cardRectTransform.DOMove(endPosition, animationDuration).SetEase(Ease.InOutQuad);

        cardRectTransform.position = endPosition;

        yield return new WaitForSeconds(animationDuration);
    }
}
