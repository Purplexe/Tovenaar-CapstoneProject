//Controls YOUR SIDE, OPPONENT SIDE UI labels. 
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class RowSideLabels : MonoBehaviour
{
    public TMP_Text topRowLabel;    // Player2 row (top)
    public TMP_Text bottomRowLabel; // Player1 row (bottom)

    //colors for players, this could be cool to change later based on player prefs. 
    public Color youColor = new Color(0.25f, 0.5f, 1f);      // Blue
    public Color opponentColor = new Color(1f, 0.3f, 0.3f);  // Red

    private bool _initialized;

    private void Start()
    {
        var networkMan = NetworkManager.Singleton;

        if (networkMan == null)
        {
            return;
        }

        // If network is already running
        if (networkMan.IsHost || networkMan.IsClient || networkMan.IsServer)
        {
            InitForLocalSide();
        }

        // Also subscribe so if the network starts after loading scene
        // we still get a chance to initialize
        networkMan.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDestroy()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Only initialize when THIS client finishes connecting
        if (clientId == nm.LocalClientId && !_initialized)
        {
            InitForLocalSide();
        }
    }

    //setting client based side visuals based on logic gathered from Game manager
    private void InitForLocalSide()
    {
        var gameManageer = TovenaarGameManager.Instance;
        if (gameManageer == null)
        {
            return;
        }

        Side mySide = gameManageer.GetLocalSide();
        bool iAmPlayer1 = (mySide == Side.Player1);
        _initialized = true;

        if (iAmPlayer1)
        {
            // Player1 is bottom row
            SetRowLabel(topRowLabel, "Opponent", opponentColor);
            SetRowLabel(bottomRowLabel, "You", youColor);
        }
        else
        {
            // Player2 is top row
            SetRowLabel(topRowLabel, "You", youColor);
            SetRowLabel(bottomRowLabel, "Opponent", opponentColor);
        }
    }

    private void SetRowLabel(TMP_Text label, string text, Color color)
    {
        if (label == null) return;
        label.text = text;
        label.color = color;
    }

}
