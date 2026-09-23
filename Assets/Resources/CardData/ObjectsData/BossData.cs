using UnityEngine;

/// <summary>
/// Класс - ScriptableObject Босс
/// </summary>
[CreateAssetMenu(fileName = "NewBossData", menuName = "Game/BossData")]
public class BossData : ScriptableObject
{
    public float bossHP;
    public float attackPower;

    [Header("Presentation (visual agent reads, depth agent fills)")]
    public string displayName;
    /// <summary>Спрайт босса. null = спрайт, уже стоящий в сцене.</summary>
    public Sprite sprite;
    /// <summary>Тонировка/аура босса (элита и финальный босс отличаются цветом).</summary>
    public Color tint = Color.white;
    public float maxHP;

    public void ApplyDamage(int damage)
    {
        bossHP -= damage;
    }
    public BossData Clone()
    {
        return Instantiate(this);
    }
}
