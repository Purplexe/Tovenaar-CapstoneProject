using UnityEngine;
using TMPro;
using Unity.Netcode;

public class TurnLabelUI : MonoBehaviour
{
    public TMP_Text label;


    //determining whos side is whos
    private void OnEnable()
    {
        TovenaarGameManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        TovenaarGameManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(Side whoseTurn)
    {
        if (TovenaarGameManager.Instance == null || label == null)
            return;

        Side mySide = TovenaarGameManager.Instance.GetLocalSide();

        bool isMyTurn = (whoseTurn == mySide);

        label.text = isMyTurn ? "Your Turn" : "Opponent's Turn";
    }
}
