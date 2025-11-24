using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerLabelsUI : MonoBehaviour
{
    public TMP_Text topLabel;
    public TMP_Text bottomLabel;

    private void Start()
    {
        // If we aren't connected yet, just default
        if (NetworkManager.Singleton == null)
        {
            topLabel.text = "Opponent";
            bottomLabel.text = "You";
            return;
        }

        bool isLocalPlayer1 = NetworkManager.Singleton.LocalClientId == 0;

        if (isLocalPlayer1)
        {
            // Host / P1: your row is the bottom one
            topLabel.text = "Opponent";
            bottomLabel.text = "You";
        }
        else
        {
            // Client / P2: your row is the top one
            topLabel.text = "You";
            bottomLabel.text = "Opponent";
        }
    }
}
