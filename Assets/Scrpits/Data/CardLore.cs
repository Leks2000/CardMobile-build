using System.Collections.Generic;

/// <summary>
/// Короткое описание «кто это» для карт (роль в отряде), показывается в подсказке карты и при выпадении.
/// Ключ - CardData.Id. Своё описание можно задать полем CardData.role.
/// </summary>
public static class CardLore
{
    private static readonly Dictionary<string, string> Roles = new Dictionary<string, string>
    {
        // игрок
        ["JuniorWorker"] = "Basic swordsman. Cheap and reliable front-liner.",
        ["Intern"] = "Demon grunt. Cheap, angry, hits first.",
        ["SecurityGuard"] = "Tank. Lots of HP and a shield - holds the line.",
        ["StaplerSniper"] = "Back-row archer. Shoots over your own cards.",
        ["MotivationalCoach"] = "Support. Raises the attack of the ally in front.",
        ["Accountant"] = "Dark caster. Poisons enemies from the back row.",
        ["CompliancePaladin"] = "Armored bruiser. Forges new shield every turn.",
        ["HRNurse"] = "Healer. Restores HP to neighbouring allies.",
        ["ITWizard"] = "Glass cannon. Burns enemies from afar.",
        ["ScrumMaster"] = "Support. War drums boost diagonal allies.",
        ["Barista"] = "Demon seductress. Drains life to heal you.",
        // враги
        ["RatCard"] = "Enemy spearman. Basic front-line soldier.",
        ["PaperArcher"] = "Enemy marksman. Shoots over its allies.",
        ["FilingGolem"] = "Enemy brute. Huge damage, little defence.",
        ["MoldSlime"] = "Enemy plague-bringer. Spreads poison.",
        ["StaplerBat"] = "Enemy cultist. Strikes open bleeding wounds.",
    };

    public static string RoleOf(string id) => id != null && Roles.TryGetValue(id, out var r) ? r : "";
}
