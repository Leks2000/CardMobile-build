using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardCostStatus : MonoBehaviour
{
    public int totalMana = 3;
    List<int> cost = new List<int>();
    public TextMeshProUGUI manaValue;

    public void getStatus(int value)
    {
        cost.Add(value);
    }
    public void returnManaText(int value)
    {
        totalMana -= value;
        updateText();
        setStatusPreparedness();
    }
    public void updateText()
    {
        manaValue.text = totalMana.ToString();
    }
    private void setStatusPreparedness()
    {
        cost.ForEach(costItem =>
        {
            if (costItem > totalMana)
            {
                Debug.Log(costItem);
            }
        });
    }
}
