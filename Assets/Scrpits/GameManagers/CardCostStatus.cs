using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardCostStatus : MonoBehaviour
{
    public int totalMana = 3;
    /// <summary>[D] Мана в начале каждого раунда (реликвия Espresso: 4).</summary>
    public int maxMana = 3;
    protected List<GameObject> card = new();
    public TextMeshProUGUI manaValue;

    public void getStatus(GameObject value)
    {
        card.Add(value);
        setStatusPreparedness();
    }
    public void returnManaText(GameObject value)
    {
        totalMana -= value.GetComponent<Card>().CardData.Cost;
        card.Remove(value);
        updateText();
        setStatusPreparedness();
    }
    public void updateText()
    {
        manaValue.text = totalMana.ToString() + "/" + maxMana;
    }
    public void setStatusPreparedness()
    {
        card.RemoveAll(c => c == null);
        card.ForEach(cardIndex =>
        {
            var cardTrans = cardIndex.GetComponent<CanvasGroup>();
            var carDrag = cardIndex.GetComponent<CardDrag>();

            if (cardIndex.GetComponent<Card>().CardData.Cost > totalMana)
            {
                carDrag.enabled = false;
                cardTrans.alpha = 0.75f;
            }
            else
            {
                carDrag.enabled = true;
                cardTrans.alpha = 1f;
            }
        });
    }
}
