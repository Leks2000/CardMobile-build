using UnityEngine;

/// <summary>
/// Класс - ScriptableObject Босс
/// </summary>
[CreateAssetMenu(fileName = "NewBossData", menuName = "Game/BossData")]
public class BossData : ScriptableObject
{
    public float bossHP;
    public float attackPower;

    public void ApplyDamage(int damage)
    {
        bossHP -= damage;
    }
    public BossData Clone()
    {
        return Instantiate(this);
    }
}
