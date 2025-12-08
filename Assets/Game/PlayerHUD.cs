//handles player info and sets sides
using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public TMP_Text healthText;
    public TMP_Text zielText;

    // false = You, true = Opponent
    public bool isOpponentHud = false;   

    private TovenaarGameManager gameManager;

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = TovenaarGameManager.Instance;
            if (gameManager == null) return;
        }

        // figure out which side THIS HUD should show, from client 
        Side localSide = gameManager.GetLocalSide();
        Side sideToShow = isOpponentHud
            ? (localSide == Side.Player1 ? Side.Player2 : Side.Player1)
            : localSide;

        var hp = gameManager.GetHp(sideToShow).Value;
        var zielCur = gameManager.GetZielCurrent(sideToShow).Value;
        var zielMax = gameManager.GetZielMax(sideToShow).Value;

        //does the 1/1 thing for each side, fancy
        if (healthText != null) healthText.text = $"HP: {hp}";
        if (zielText != null) zielText.text = $"Ziel: {zielCur}/{zielMax}";
    }
}
