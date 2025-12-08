//networked cards. Network card prefab is all just numbers which your game pulls from to display card locally
//I know it says   [ServerRpc(RequireOwnership = false)] is deprecated, but I just kept it in since the newer version is .... finnicky

using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkCard : NetworkBehaviour
{
 
    //all card stats
    //fortmat follows default value, read permission, write permission. everyone can read but only server can write 
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

    //board card to be populated w information
    public GameObject boardCardPrefab;

    private GameObject visualInstance;
    private BoardCardVisual visual; //accessing 

    //called by Game manager before network object is spawned
    public void ServerInitFromDto(DeckCardDto dto, Side owner, int laneIndex)
    {
        //taking data from deck list as well as the owner
        //datatype HAS to be FixedString for Netcode

        //writing everything into network variables

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Build / move the UI card on this client
        CreateOrMoveVisual();

        // Hook stats into the ui by subscriping to network variables
        Attack.OnValueChanged += (_, __) => RefreshVisual();
        Health.OnValueChanged += (_, __) => RefreshVisual();
        Cost.OnValueChanged += (_, __) => RefreshVisual();
        Name.OnValueChanged += (_, __) => RefreshVisual();
        Rarity.OnValueChanged += (_, __) => RefreshVisual();
        RulesText.OnValueChanged += (_, __) => RefreshVisual();
        //initialize visuals of the card xd
        RefreshVisual();
    }
    //kill
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
//called from Network spawn
    private void CreateOrMoveVisual()
    {
        if (boardCardPrefab == null)
        {
            return;
        }

        var gameManager = TovenaarGameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        //get parent lane onject
        Transform parent = gameManager.GetLaneTransformForVisual(OwnerSide.Value, LaneIndex.Value);

        if (parent == null)
        {
            return;
        }

        //if no card, create one
        if (visualInstance == null)
        {
            visualInstance = GameObject.Instantiate(boardCardPrefab, parent);
            //rectTransform, where the card should be
            var rt = visualInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            visual = visualInstance.GetComponent<BoardCardVisual>();
        }
        else
        {
            visualInstance.transform.SetParent(parent, false);
        }
    }

    //changes look of card based on values changing, card health increase/decrease or idk whatever
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

    [ServerRpc(RequireOwnership = false)]
    //taking damage, and killing card off correctly if it dies. Network obhj
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer)
            return;

        Health.Value -= damage;
        if (Health.Value <= 0)
        {
            var gameManager = TovenaarGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.Server_OnCardDespawn(this, OwnerSide.Value, LaneIndex.Value);
            }

            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    //heals
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount)
    {
        if (!IsServer || amount <= 0)
            return;
        //checking max health since we dont want to overwrite
        int max = MaxHealth.Value > 0 ? MaxHealth.Value : Health.Value;
        Health.Value = Mathf.Min(Health.Value + amount, max);
    }

    //setting frozen status
    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenServerRpc(int rounds)
    {
        if (!IsServer)
            return;

        if (rounds > FrozenForRounds.Value)
            FrozenForRounds.Value = rounds;
    }

}
