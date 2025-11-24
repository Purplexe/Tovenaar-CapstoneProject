using Unity.Netcode;
using UnityEngine;
using TMPro;

public class TovenaarNetworkUI : MonoBehaviour
{
    public TMP_Text infoLabel;

    public void OnStartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            infoLabel.text = "Host started. Waiting for client...";
        }
        else
        {
            infoLabel.text = "Failed to start host.";
        }
    }

    public void OnStartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            infoLabel.text = "Client connecting...";
        }
        else
        {
            infoLabel.text = "Failed to start client.";
        }
    }
}
