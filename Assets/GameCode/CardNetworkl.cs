/*
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkCard : NetworkBehaviour
{
    // ---------------- NetVars ----------------

    public NetworkVariable<int> Attack = new NetworkVariable<int>();
    public NetworkVariable<int> Health = new NetworkVariable<int>();
    public NetworkVariable<int> Cost = new NetworkVariable<int>();

    public NetworkVariable<int> OwnerSide = new NetworkVariable<int>();  // 0 = P1, 1 = P2
    public NetworkVariable<int> LaneIndex = new NetworkVariable<int>();  // 0..3

    // Strings as FixedStrings so they can be NetVars
    public NetworkVariable<FixedString64Bytes> NetUid = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<FixedString64Bytes> NetName = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<FixedString32Bytes> NetRarity = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<FixedString128Bytes> NetRules = new NetworkVariable<FixedString128Bytes>();

    // ---------------- Non-networked ----------------

    private BoardCardVisual _visual;  // UI instance on this client

    // Called by GameManager on the SERVER before Spawn()
    public void InitFromDto(DeckCardDto data, Side owner, int laneIndex)
    {
        bool runtimeIsServer = NetworkManager.Singleton != null &&
                               NetworkManager.Singleton.IsServer;
        if (!runtimeIsServer)
            return;

        OwnerSide.Value = (int)owner;
        LaneIndex.Value = laneIndex;

        Attack.Value = data.attack;
        Health.Value = data.health;
        Cost.Value = data.cost;

        string uid = string.IsNullOrEmpty(data.card_uid) ? "" : data.card_uid;
        string name = string.IsNullOrEmpty(data.name) ? uid : data.name;

        NetUid.Value = uid;
        NetName.Value = name;
        NetRarity.Value = string.IsNullOrEmpty(data.rarity) ? "" : data.rarity;
        NetRules.Value = string.IsNullOrEmpty(data.rules_text) ? "" : data.rules_text;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsClient)
            return;

        // Find which lane UI this card should appear in for THIS client.
        BoardLayout layout = BoardLayout.Instance;
        if (layout == null)
        {
            Debug.LogError("[NetworkCard] BoardLayout.Instance is null.");
            return;
        }

        Transform laneUI = layout.GetLaneTransformForCardOwner(
            (Side)OwnerSide.Value,
            LaneIndex.Value
        );

        if (laneUI == null)
        {
            Debug.LogError("[NetworkCard] Lane UI transform is null.");
            return;
        }

        // Instantiate the visual card under that lane
        GameObject visualObj = GameObject.Instantiate(layout.boardCardPrefab, laneUI);
        _visual = visualObj.GetComponent<BoardCardVisual>();

        if (_visual == null)
        {
            Debug.LogError("[NetworkCard] BoardCardVisual missing on prefab.");
        }
        else
        {
            _visual.Bind(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Kill the local UI
        if (_visual != null)
        {
            Destroy(_visual.gameObject);
            _visual = null;
        }

        // Tell GameManager to clear this board slot (server only)
        if (IsServer && TovenaarGameManager.Instance != null)
        {
            TovenaarGameManager.Instance.ClearBoardSlot(
                (Side)OwnerSide.Value,
                LaneIndex.Value,
                this
            );
        }
    }

    // Server-side damage helper
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer) return;

        Health.Value -= damage;
        if (Health.Value <= 0)
        {
            NetworkObject.Despawn(true);
        }
    }
}
*/