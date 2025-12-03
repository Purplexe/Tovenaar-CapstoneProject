using UnityEngine;
using TMPro;

public class MultiplayerUI : MonoBehaviour
{

    //Controls logic in MatchScene, starts with connection and then populates the board based on data from user. 
    public GameObject connectRoot;
    public GameObject boardRoot;

    public TMP_InputField joinCodeInput;
    public TMP_Text statusLabel;

    public TMP_Text LobbyJoinCode;

    private void Start()
    {
        connectRoot.SetActive(true);
        boardRoot.SetActive(false);
    }

    public async void OnHostClicked()
    {
        statusLabel.text = "Starting host";

        string code = await MultiplayerManager.Instance.StartHostAsync();

        if (string.IsNullOrEmpty(code))
        {
            statusLabel.text = "Host failed.";
            return;
        }

        statusLabel.text = "Join Code: " + code;
        ShowBoard();
        LobbyJoinCode.text = code;
    }

    public async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            statusLabel.text = "Please enter a join code.";
            return;
        }

        statusLabel.text = "Joining " + code + "...";

        bool ok = await MultiplayerManager.Instance.StartClientAsync(code);

        if (!ok)
        {
            statusLabel.text = "Failed to join.";
            return;
        }

        statusLabel.text = "Connected!";
        ShowBoard();
    }

    private void ShowBoard()
    {
        connectRoot.SetActive(false);
        boardRoot.SetActive(true);
    }
}
