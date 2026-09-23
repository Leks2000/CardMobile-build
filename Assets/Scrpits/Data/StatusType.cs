using System;

/// <summary>
/// Статусы боя. Логику реализует StatusHolder (Scrpits/Combat), иконки рисует визуал.
/// Bleed/Poison - урон в начале раунда, Shield - поглощает урон,
/// Lifesteal - лечит игрока на % нанесённого урона, Thorns - отражает урон атакующему.
/// </summary>
public enum StatusType { Bleed, Poison, Shield, Lifesteal, Thorns }

/// <summary>
/// Описание статуса на карте: что карта накладывает при ударе (onHit) или имеет сама (self).
/// </summary>
[Serializable]
public struct StatusSpec
{
    public StatusType type;
    public int value;
    /// <summary>true - статус на саму карту при появлении (Shield/Lifesteal/Thorns), false - накладывается на цель при ударе (Bleed/Poison).</summary>
    public bool self;
}
