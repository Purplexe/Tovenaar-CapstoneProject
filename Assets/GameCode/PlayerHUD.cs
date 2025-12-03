using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text healthText;
    public TMP_Text zielText;

    [Header("Which player does this HUD represent?")]
    public bool isOpponentHud = false;   // false = "You", true = "Opponent"

    private TovenaarGameManager gm;

    private void Update()
    {
        if (gm == null)
        {
            gm = TovenaarGameManager.Instance;
            if (gm == null) return;
        }

        // figure out which side THIS HUD should show, from the local client's POV
        Side localSide = gm.GetLocalSide();
        Side sideToShow = isOpponentHud
            ? (localSide == Side.Player1 ? Side.Player2 : Side.Player1)
            : localSide;

        var hp = gm.GetHp(sideToShow).Value;
        var zielCur = gm.GetZielCurrent(sideToShow).Value;
        var zielMax = gm.GetZielMax(sideToShow).Value;

        if (healthText != null) healthText.text = $"HP: {hp}";
        if (zielText != null) zielText.text = $"Ziel: {zielCur}/{zielMax}";
    }
}
