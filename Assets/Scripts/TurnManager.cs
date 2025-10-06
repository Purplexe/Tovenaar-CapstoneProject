using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance {get; private set; }
    public PlayerSide CurrentPlayer = PlayerSide.Player1;

    void Awake() { instance = this; }

    public bool IsPlayersTurn(PlayerSide side) => side == CurrentPlayer;

    public void EndTurn()
    {
        CurrentPlayer = (CurrentPlayer == PlayerSide.Player1) ? PlayerSide.Player2 : PlayerSide.Player1;
        BoardManager.I.ResolveCombat();

        // Example: give Ziel per new turn
        PlayerResourceManager.I.Refill(1);
    }


}
