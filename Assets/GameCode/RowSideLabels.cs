using TMPro;
using UnityEngine;
using Unity.Netcode;

public class RowSideLabels : MonoBehaviour
{
    [Header("Row labels (top = Player2 row, bottom = Player1 row)")]
    public TMP_Text topRowLabel;    // Player2 row (top)
    public TMP_Text bottomRowLabel; // Player1 row (bottom)

    [Header("Optional: lane labels")]
    public TMP_Text[] topLaneLabels;    // For coloring only
    public TMP_Text[] bottomLaneLabels; // For coloring only

    [Header("Colors")]
    public Color youColor = new Color(0.25f, 0.5f, 1f);      // Blue
    public Color opponentColor = new Color(1f, 0.3f, 0.3f);  // Red

    private bool _initialized;

    private void Start()
    {
        var nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogWarning("[RowSideLabels] No NetworkManager.Singleton.");
            return;
        }

        // If network is already running (e.g. we loaded match AFTER hosting/joining)
        if (nm.IsHost || nm.IsClient || nm.IsServer)
        {
            InitForLocalSide();
        }

        // Also subscribe so if the network starts AFTER this scene loads,
        // we still get a chance to initialize.
        nm.OnClientConnectedCallback += HandleClientConnected;
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

    private void InitForLocalSide()
    {
        var gm = TovenaarGameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[RowSideLabels] No TovenaarGameManager instance.");
            return;
        }

        Side mySide = gm.GetLocalSide();
        bool iAmPlayer1 = (mySide == Side.Player1);
        _initialized = true;

        Debug.Log($"[RowSideLabels] INIT Local side = {mySide}, iAmPlayer1 = {iAmPlayer1}");

        if (iAmPlayer1)
        {
            // Player1 is bottom row
            SetRowLabel(topRowLabel, "Opponent", opponentColor);
            SetRowLabel(bottomRowLabel, "You", youColor);

            ColorLaneLabels(topLaneLabels, opponentColor);
            ColorLaneLabels(bottomLaneLabels, youColor);
        }
        else
        {
            // Player2 is top row
            SetRowLabel(topRowLabel, "You", youColor);
            SetRowLabel(bottomRowLabel, "Opponent", opponentColor);

            ColorLaneLabels(topLaneLabels, youColor);
            ColorLaneLabels(bottomLaneLabels, opponentColor);
        }
    }

    private void SetRowLabel(TMP_Text label, string text, Color color)
    {
        if (label == null) return;
        label.text = text;
        label.color = color;
    }

    private void ColorLaneLabels(TMP_Text[] labels, Color color)
    {
        if (labels == null) return;
        foreach (var t in labels)
        {
            if (t == null) continue;
            t.color = color;
        }
    }
}
