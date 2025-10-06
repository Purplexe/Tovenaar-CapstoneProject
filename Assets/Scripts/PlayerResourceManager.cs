using UnityEngine;

public class PlayerResourceManager : MonoBehaviour
{
    public static PlayerResourceManager I { get; private set; }

    public int player1Ziel = 5;
    public int player2Ziel = 5;

    void Awake() { I = this; }

    public int GetZiel(PlayerSide side)
        => side == PlayerSide.Player1 ? player1Ziel : player2Ziel;

    public bool CanAfford(PlayerSide side, int cost)
        => GetZiel(side) >= cost;

    public void Spend(PlayerSide side, int cost)
    {
        if (side == PlayerSide.Player1) player1Ziel -= cost;
        else player2Ziel -= cost;
    }

    public void Refill(int amount = 5)
    {
        player1Ziel += amount;
        player2Ziel += amount;
    }
}
