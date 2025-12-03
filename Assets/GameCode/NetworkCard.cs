using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkCard : NetworkBehaviour
{
    // -------- Networked stats / metadata --------

    public NetworkVariable<int> Attack = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Health = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Cost = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> Name = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> Rarity = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString128Bytes> RulesText = new NetworkVariable<FixedString128Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> CardUid = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Side> OwnerSide = new NetworkVariable<Side>(
        Side.Player1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> LaneIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> MaxHealth = new NetworkVariable<int>(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> FrozenForRounds = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    // -------- Visuals --------

    [Header("Visual prefab (UI BoardCard)")]
    public GameObject boardCardPrefab;    // your existing BoardCard prefab

    private GameObject visualInstance;
    private BoardCardVisual visual;       // small view script below

    // --------------------------------------------------------------------
    // Called ONLY by the server (GameManager) BEFORE Spawn()
    // --------------------------------------------------------------------
    public void ServerInitFromDto(DeckCardDto dto, Side owner, int laneIndex)
    {
        // IMPORTANT: do NOT guard with IsServer here. We WANT this to run
        // before Spawn() while this object is still not "IsServer".
        // Only GameManager (server) calls this.

        FixedString64Bytes uid = string.IsNullOrEmpty(dto.card_uid)
            ? (FixedString64Bytes)""
            : (FixedString64Bytes)dto.card_uid;

        FixedString64Bytes name = string.IsNullOrEmpty(dto.name)
            ? uid
            : (FixedString64Bytes)dto.name;

        CardUid.Value = uid;
        Name.Value = name;
        Rarity.Value = string.IsNullOrEmpty(dto.rarity)
            ? (FixedString64Bytes)""
            : (FixedString64Bytes)dto.rarity;

        RulesText.Value = string.IsNullOrEmpty(dto.rules_text)
            ? (FixedString128Bytes)""
            : (FixedString128Bytes)dto.rules_text;

        Attack.Value = dto.attack;
        MaxHealth.Value = dto.health;
        Health.Value = dto.health;
        Cost.Value = dto.cost;
        FrozenForRounds.Value = 0;


        OwnerSide.Value = owner;
        LaneIndex.Value = laneIndex;
    }

    // --------------------------------------------------------------------
    // Network spawn
    // --------------------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Build / move the UI card on this client
        CreateOrMoveVisual();

        // Hook stats -> UI
        Attack.OnValueChanged += (_, __) => RefreshVisual();
        Health.OnValueChanged += (_, __) => RefreshVisual();
        Cost.OnValueChanged += (_, __) => RefreshVisual();
        Name.OnValueChanged += (_, __) => RefreshVisual();
        Rarity.OnValueChanged += (_, __) => RefreshVisual();
        RulesText.OnValueChanged += (_, __) => RefreshVisual();

        RefreshVisual();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (visualInstance != null)
        {
            Destroy(visualInstance);
            visualInstance = null;
            visual = null;
        }
    }

    // --------------------------------------------------------------------
    // Visual helpers
    // --------------------------------------------------------------------
    private void CreateOrMoveVisual()
    {
        if (boardCardPrefab == null)
        {
            Debug.LogError("[NetworkCard] boardCardPrefab not set.");
            return;
        }

        var gm = TovenaarGameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[NetworkCard] No TovenaarGameManager instance.");
            return;
        }

        Transform parent = gm.GetLaneTransformForVisual(OwnerSide.Value, LaneIndex.Value);
        if (parent == null)
        {
            Debug.LogError($"[NetworkCard] No lane transform for {OwnerSide.Value} lane {LaneIndex.Value}");
            return;
        }

        if (visualInstance == null)
        {
            visualInstance = GameObject.Instantiate(boardCardPrefab, parent);
            var rt = visualInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            visual = visualInstance.GetComponent<BoardCardVisual>();
            if (visual == null)
            {
                Debug.LogWarning("[NetworkCard] BoardCardVisual component missing on boardCardPrefab.");
            }
        }
        else
        {
            visualInstance.transform.SetParent(parent, false);
        }
    }

    private void RefreshVisual()
    {
        if (visual == null)
            return;

        visual.SetData(
            Name.Value.ToString(),
            Attack.Value,
            Health.Value,
            Cost.Value,
            Rarity.Value.ToString(),
            RulesText.Value.ToString(),
            CardUid.Value.ToString()
        );
    }

    // --------------------------------------------------------------------
    // Combat API (unchanged from your previous logic)
    // --------------------------------------------------------------------
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer)
            return;

        Health.Value -= damage;
        if (Health.Value <= 0)
        {
            var gm = TovenaarGameManager.Instance;
            if (gm != null)
            {
                gm.Server_OnCardDespawn(this, OwnerSide.Value, LaneIndex.Value);
            }

            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount)
    {
        if (!IsServer || amount <= 0)
            return;

        int max = MaxHealth.Value > 0 ? MaxHealth.Value : Health.Value;
        Health.Value = Mathf.Min(Health.Value + amount, max);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenServerRpc(int rounds)
    {
        if (!IsServer)
            return;

        if (rounds > FrozenForRounds.Value)
            FrozenForRounds.Value = rounds;
    }

}
