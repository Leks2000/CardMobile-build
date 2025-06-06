using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Game/PlayerData")]
public class PlayerData : ScriptableObject
{
    public int playerHP;
    public int playerHPMAX;

    public void ApplyDamage(int damage)
    {
        playerHP -= damage;
    }

    public PlayerData Clone()
    {
        return Instantiate(this);
    }
}
