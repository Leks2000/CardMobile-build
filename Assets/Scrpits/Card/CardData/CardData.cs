using UnityEngine;

/// <summary>
/// Класс - ScriptableObject карт
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "Game/CardData")]
public class CardData : ScriptableObject
{
    public int HP;
    public int Damage;
    public int Cost;
    public string cardInfo;
    public void ApplyDamage(int damage)
    {
        HP -= damage;
    }
    public CardData Clone()
    {
        return Instantiate(this);
    }
}
