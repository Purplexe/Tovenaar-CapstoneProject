using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerLabelsUI : MonoBehaviour
{
    //Using to get player 
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
            // Host = Bottom
            topLabel.text = "Opponent";
            bottomLabel.text = "You";
        }
        else
        {
            // Client = Top
            topLabel.text = "You";
            bottomLabel.text = "Opponent";
        }
    }
}
