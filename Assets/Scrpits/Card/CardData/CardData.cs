using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Card Data")]
public class CardData : ScriptableObject
{
    public int HP;
    public int Damage;
    public float Cost;
    public int distanceToAttack;
    public void ApplyDamage(int damage)
    {
        HP -= damage;
    }
    public CardData Clone()
    {
        return Instantiate(this);
    }
}
