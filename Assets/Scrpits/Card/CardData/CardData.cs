using System;
using UnityEngine;

public class CardData
{
    public int HP { get; private set; }
    public int Damage { get; private set; }
    public int Range { get; private set; }
    public enum AttackType
    {
        CloseRange,
        LongRange
    }
    public CardData(int Hp, int Dmg, AttackType TypeAttack)
    {
        HP = Hp;
        Damage = Dmg;
        Range = GetRangeFromAttackType(TypeAttack);
    }
    private int GetRangeFromAttackType(AttackType type)
    {
        switch (type)
        {
            case AttackType.CloseRange:
                return 50;
            case AttackType.LongRange:
                return 100;
            default:
                return 0;
        }
    }
    public void ApplyDamage(int damage)
    {
        HP -= damage;
    }
}
