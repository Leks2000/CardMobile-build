using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCostStatus : MonoBehaviour
{
    public int totalMana = 3;
    List<GameObject> cards = new List<GameObject>();
    private Dictionary<GameObject, List<Image>> cardImagesCache = new Dictionary<GameObject, List<Image>>();
    public TextMeshProUGUI manaValue;

    public void Start()
    {
        setStatusPreparedness();
    }
    public void addCard(GameObject card)
    {
        cards.Add(card);
        List<Image> images = new List<Image>(card.GetComponentsInChildren<Image>());
        cardImagesCache[card] = images;
    }
    public void deleteCard(GameObject card)
    {
        cards.Remove(card);
        cardImagesCache.Remove(card);
        ReduceMana(card.GetComponent<Card>().CardInfo.Cost);
    }
    private void ReduceMana(int value)
    {
        totalMana -= value;
        updateText();
    }
    public void updateText()
    {
        manaValue.text = totalMana.ToString();
        setStatusPreparedness();
    }
    private void setStatusPreparedness()
    {
        cards.ForEach(costItem =>
        {
            Card cardComponent = costItem.GetComponent<Card>();
            if (cardComponent != null && cardComponent.CardInfo != null)
            {
                CardData cardData = costItem.GetComponent<Card>().CardInfo;
                bool isCardDisabled = cardData.Cost > totalMana;

                SetCardInteractivity(costItem, !isCardDisabled);
                SetCardTransparency(costItem, isCardDisabled ? 0.6f : 1f);
                SetCardSpehre(costItem, !isCardDisabled);
            }
        });
    }

    private void SetCardSpehre(GameObject card, bool isEnabled)
    {
        card.GetComponentInChildren<SphereCollider>().enabled = isEnabled;
    }

    private void SetCardInteractivity(GameObject card, bool isEnabled)
    {
        if (cardImagesCache.TryGetValue(card, out List<Image> images))
        {
            foreach (Image img in images)
            {
                img.raycastTarget = isEnabled;
            }
        }
    }
    private void SetCardTransparency(GameObject card, float alphaValue)
    {
        if (cardImagesCache.TryGetValue(card, out List<Image> images))
        {
            foreach (Image img in images)
            {
                Color tempColor = img.color;
                tempColor.a = alphaValue;
                img.color = tempColor;
            }
        }
    }
}
