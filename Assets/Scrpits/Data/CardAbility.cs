/// <summary>
/// Особые свойства карт игрока (логика - CardAbilities, Scrpits/Combat).
/// «Впереди» = клетка того же столбца в другом ряду твоей половины поля,
/// «по диагонали» = соседние столбцы в другом ряду.
/// </summary>
public enum CardAbility
{
    None,
    /// <summary>Стреляет из любого ряда через своих: бьёт первого врага в столбце или босса.</summary>
    Ranged,
    /// <summary>Когда сыграна: союзник впереди (или позади, если карта в первом ряду) получает +value ATK.</summary>
    BuffFront,
    /// <summary>Когда сыграна: союзники по диагонали получают +value ATK.</summary>
    BuffDiagonal,
    /// <summary>В начале каждого твоего хода получает +value Shield.</summary>
    ShieldEachRound,
    /// <summary>Когда сыграна: соседние союзники (впереди/позади, слева, справа) получают +value HP.</summary>
    HealAdjacent,
}
