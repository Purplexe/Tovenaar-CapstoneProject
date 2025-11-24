using UnityEngine;
using TMPro;
using Unity.Netcode;

public class TurnLabelUI : MonoBehaviour
{
    public TMP_Text label;

    private void OnEnable()
    {
        CardGameManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        CardGameManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(Side whoseTurn)
    {
        if (CardGameManager.Instance == null || label == null)
            return;

        Side mySide = CardGameManager.Instance.GetLocalSide();

        bool isMyTurn = (whoseTurn == mySide);

        label.text = isMyTurn ? "Your Turn" : "Opponent's Turn";
    }
}
