using UnityEngine;

/// <summary>
/// [D] Создаёт карту из CardData на базовом префабе (PlayerCardBase / EnemyCardBase).
/// Данные назначаются ДО Card.Awake: префаб инстанцируется под неактивный "склад",
/// получает данные и только потом переносится в целевой родитель (там срабатывает Awake).
/// </summary>
public static class CardFactory
{
    private const string PlayerBase = "Objects/PlayerCardBase";
    private const string EnemyBase = "Objects/EnemyCardBase";
    private const string PlayerFallback = "Objects/JuniorWorker";
    private const string EnemyFallback = "Objects/RatCard";

    private static Transform stash;

    public static GameObject BasePrefab(bool enemy)
    {
        var prefab = Resources.Load<GameObject>(enemy ? EnemyBase : PlayerBase);
        return prefab != null ? prefab : Resources.Load<GameObject>(enemy ? EnemyFallback : PlayerFallback);
    }

    public static Card Spawn(CardData data, Transform parent)
    {
        if (data == null) return null;
        var go = Object.Instantiate(BasePrefab(data.isEnemy), Stash(), false);
        go.name = data.Id;
        var card = go.GetComponent<Card>();
        card.SetSourceData(data);
        go.transform.SetParent(parent, false); // становится активным -> Card.Awake с нужными данными

        var view = go.GetComponent<CardView>();
        if (view != null) view.Bind(card.CardData);
        return card;
    }

    /// <summary>Неактивный контейнер в текущей сцене: дети не получают Awake, пока их не перенесут.</summary>
    private static Transform Stash()
    {
        if (stash == null)
        {
            var go = new GameObject("[CardFactory stash]");
            go.SetActive(false);
            stash = go.transform;
        }
        return stash;
    }
}
