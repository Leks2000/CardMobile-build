using UnityEngine;

/// <summary>
/// Класс - ScriptableObject карт
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "Game/CardData")]
/// <summary>
/// Инфа о карточках
/// </summary>
public class CardData : ScriptableObject
{
    public int HP;
    public int Damage;
    public int Cost;
    public string cardInfo;

    [Header("Identity / presentation")]
    /// <summary>Уникальный id (для колоды забега и дропа из кейсов). Пусто = имя ассета.</summary>
    public string id;
    public string displayName;
    /// <summary>Арт карты (пользователь подкладывает свой PNG). null = старый арт префаба.</summary>
    public Sprite art;
    public CardRarity rarity = CardRarity.Common;
    /// <summary>Карта врага (спавнится на стороне противника).</summary>
    public bool isEnemy;
    /// <summary>Входит в стартовую колоду забега.</summary>
    public bool isStarter;
    /// <summary>[D] Сколько копий карты в стартовой колоде (если isStarter).</summary>
    public int starterCopies = 1;
    /// <summary>Можно выбить из кейса / купить.</summary>
    public bool inShopPool = true;

    /// <summary>Прокачанная «Senior»-версия (+1 ATK / +1 HP, золотая рамка). Создаётся CardUpgrade.</summary>
    public bool upgraded;

    [Header("Ability (CardAbilities)")]
    /// <summary>Особое свойство карты: дальний бой, баффы соседей, щит каждый раунд, лечение.</summary>
    public CardAbility ability = CardAbility.None;
    /// <summary>Сила свойства (+ATK / +Shield / +HP).</summary>
    public int abilityValue;

    [Header("Statuses")]
    public System.Collections.Generic.List<StatusSpec> statuses = new System.Collections.Generic.List<StatusSpec>();

    public string Id => string.IsNullOrEmpty(id) ? name.Replace("(Clone)", "") : id;
    public string Title => string.IsNullOrEmpty(displayName) ? Id : displayName;

    public void ApplyDamage(int damage)
    {
        HP -= damage;
    }
    public CardData Clone()
    {
        return Instantiate(this);
    }
}
