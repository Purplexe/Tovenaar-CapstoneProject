using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TovenaarGameManager : NetworkBehaviour
{
    public static TovenaarGameManager Instance;

    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text turnLabel;
    [SerializeField] private TMP_Text myRoleLabel;   // "Host" / "Client"
    [SerializeField] private TMP_Text myDeckLabel;   // deck id

    // 0 = not started, 1 = host turn, 2 = client turn
    private NetworkVariable<int> currentTurn = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> playerCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Show my role
        if (NetworkManager.Singleton.IsHost)
            myRoleLabel.text = "Role: Host";
        else if (NetworkManager.Singleton.IsClient)
            myRoleLabel.text = "Role: Client";
        else
            myRoleLabel.text = "Role: Offline";

        myDeckLabel.text = "Deck ID: " + TovenaarLoginManager.Instance.SelectedDeckId;

        currentTurn.OnValueChanged += OnTurnChanged;
        playerCount.OnValueChanged += OnPlayerCountChanged;

        if (IsServer)
        {
            // server tracks when clients connect
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        statusLabel.text = "Waiting for players...";
    }

    private void OnDestroy()
    {
        currentTurn.OnValueChanged -= OnTurnChanged;
        playerCount.OnValueChanged -= OnPlayerCountChanged;

        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // Called on server when any client connects (host counts as a client too)
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        playerCount.Value++;

        if (playerCount.Value == 2)
        {
            // We have 2 players → start game, host goes first
            currentTurn.Value = 1; // 1 = host's turn
            statusLabel.text = "Game started!";
        }
    }

    private void OnPlayerCountChanged(int prev, int current)
    {
        // Could show "Players connected: x" later if you want
    }

    private void OnTurnChanged(int prev, int current)
    {
        switch (current)
        {
            case 0:
                turnLabel.text = "Turn: (not started)";
                break;
            case 1:
                turnLabel.text = "Turn: Host";
                break;
            case 2:
                turnLabel.text = "Turn: Client";
                break;
        }
    }

    // Called by a UI button "End Turn"
    public void OnEndTurnButton()
    {
        if (!IsOwnerMyTurn())
        {
            statusLabel.text = "It's not your turn!";
            return;
        }

        if (IsServer)
        {
            // Host can update turn directly
            AdvanceTurnServerRpc();
        }
        else
        {
            // Client asks server to advance
            RequestEndTurnServerRpc();
        }
    }

    private bool IsOwnerMyTurn()
    {
        if (NetworkManager.Singleton.IsHost)
            return currentTurn.Value == 1;
        if (NetworkManager.Singleton.IsClient)
            return currentTurn.Value == 2;
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        // validate later (e.g. has performed a legal move)
        AdvanceTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void AdvanceTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (currentTurn.Value == 1)
            currentTurn.Value = 2;
        else if (currentTurn.Value == 2)
            currentTurn.Value = 1;
    }

    // Called by "Exit to Menu" button
    public void OnExitToMenu()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                NetworkManager.Singleton.Shutdown();
            else if (NetworkManager.Singleton.IsClient)
                NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("Menu"); // or MainMenu scene name
    }
}
