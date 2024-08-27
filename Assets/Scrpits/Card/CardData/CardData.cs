using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Game/CardData")]
public class CardData : ScriptableObject
{
    public int HP;
    public int Damage;
    public float Cost;
    public void ApplyDamage(int damage)
    {
        HP -= damage;
    }
    public CardData Clone()
    {
        return Instantiate(this);
    }
}
