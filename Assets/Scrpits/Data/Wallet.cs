using System;
using UnityEngine;

/// <summary>
/// Единая валюта игры (монеты). Хранится в PlayerPrefs "Coins" - та же, что и раньше.
/// </summary>
public static class Wallet
{
    private const string Key = "Coins";

    public static event Action<int> Changed;

    public static int Coins => PlayerPrefs.GetInt(Key, 0);

    public static void Add(int amount)
    {
        if (amount == 0) return;
        PlayerPrefs.SetInt(Key, Mathf.Max(0, Coins + amount));
        PlayerPrefs.Save();
        Changed?.Invoke(Coins);
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        Add(-amount);
        return true;
    }

    public static void Set(int amount)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, amount));
        PlayerPrefs.Save();
        Changed?.Invoke(Coins);
    }
}
