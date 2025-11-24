using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum Side
{
    Player1 = 0,
    Player2 = 1
}

//Data object used to get hand stuff. 
[Serializable]
public struct HandCardData : INetworkSerializable
{
    public string card_uid;
    public string name;
    public int cost;
    public int attack;
    public int health;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter //prevents Nulls from disrupting
    {
        serializer.SerializeValue(ref card_uid);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref attack);
        serializer.SerializeValue(ref health);
    }
}

public class CardGameManager : NetworkBehaviour
{
    public static CardGameManager Instance { get; private set; }

    // Fired whenever the turn changes (on all clients)
    public static Action<Side> OnTurnChanged;

    // Fired when game ends (on all clients)
    public static Action<Side> OnGameOver;

    
    public Transform[] player1Lanes;     // bottom row
    public Transform[] player2Lanes;     // top row

    public GameObject cardPrefab;

    public int startingHp = 20;
    public int zielCap = 10;

    // Network state for variables

    private NetworkVariable<Side> currentTurn = new NetworkVariable<Side>(
        Side.Player1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> p1Hp = new NetworkVariable<int>(
        20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> p2Hp = new NetworkVariable<int>(
        20, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> p1ZielCurrent = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> p2ZielCurrent = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> p1ZielMax = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> p2ZielMax = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server Only States

    // pull deck list
    private readonly List<DeckCardDto> deckP1 = new List<DeckCardDto>();
    private readonly List<DeckCardDto> deckP2 = new List<DeckCardDto>();

    // cards currently in each players hand
    private readonly List<DeckCardDto> handP1 = new List<DeckCardDto>();
    private readonly List<DeckCardDto> handP2 = new List<DeckCardDto>();

    private bool gameStarted = false; // wait to start game until both playas are in 


    private void Awake()
    {
        //singleton 
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //listening for clients connecting

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        //subscribe to turn change event
        currentTurn.OnValueChanged += HandleTurnChanged;
    }

    public override void OnNetworkDespawn()
    {
        //clean event hook
        currentTurn.OnValueChanged -= HandleTurnChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    //start game here

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || NetworkManager.Singleton == null)
            return;

        // Wait until both players are connected before starting
        //1v1
        if (NetworkManager.Singleton.ConnectedClientsIds.Count == 2)
        {
            TryStartGame();
        }
    }

    
    private void TryStartGame()
    {
        if (!IsServer) return;
        if (gameStarted) return; //already started

        int players = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.ConnectedClientsIds.Count
            : -1;

        Debug.Log($"[GM] TryStartGame: deckP1={deckP1.Count}, deckP2={deckP2.Count}, players={players}");

        //get decks and players
        if (deckP1.Count == 0 || deckP2.Count == 0) return;
        if (players < 2) return;

        gameStarted = true;
        Debug.Log("[GM] GAME STARTED");
        // Init HP + ziel

        //intialize player health + ziel (da resource of the game)
        p1Hp.Value = startingHp;
        p2Hp.Value = startingHp;

        p1ZielMax.Value = 1;
        p2ZielMax.Value = 1;
        p1ZielCurrent.Value = 1;
        p2ZielCurrent.Value = 1;

        // draw 5 cards
        DrawCards(Side.Player1, 5);
        DrawCards(Side.Player2, 5);

        //Host will always go first because im lazy
        currentTurn.Value = Side.Player1;
        OnTurnChanged?.Invoke(Side.Player1);

        Debug.Log("GAME STARTED.");
    }

    //Called by deckloader after deck is loaded from DB
    public void SetDeckForSide(Side side, DeckCardDto[] cards)
    {
        if (!IsServer) return;

        var deck = GetDeck(side);
        deck.Clear();
        deck.AddRange(cards);

        //Needed both players to be able to connect so i just mirror player 1 deck. Change ASAP. Will be easy
        //=============================================================================
        Side otherSide = side == Side.Player1 ? Side.Player2 : Side.Player1;
        var otherDeck = GetDeck(otherSide);
        if (otherDeck.Count == 0)
        {
            otherDeck.Clear();
            otherDeck.AddRange(cards);
        }

        TryStartGame();
    }

    //This is Laneclicks domain on the local client. Using the currently selected hand card, attempt to place into spot assuming its not occupied and u have enough 
    public void RequestPlayCard(int laneIndex, bool isPlayer1Lane, int handIndex)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        Side mySide = GetLocalSide();

        // Must play on your own row
        if ((mySide == Side.Player1) != isPlayer1Lane)
        {
            Debug.Log("You can only play on your own row.");
            return;
        }

        // Must play on your turn
        if (mySide != currentTurn.Value)
        {
            Debug.Log("Not your turn.");
            return;
        }

        if (handIndex < 0) //select a card dummy
        {
            Debug.Log("No card selected.");
            return;
        }

        //validate on server
        PlayCardServerRpc(laneIndex, mySide, handIndex);
    }

    public Side GetLocalSide()
    {
        // Host (first client) is Player1, second client is Player2
        return NetworkManager.Singleton.LocalClientId == 0 ? Side.Player1 : Side.Player2;
    }

 
    [ServerRpc(RequireOwnership = false)]
    private void PlayCardServerRpc(int laneIndex, Side side, int handIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        //figure our who owns
        Transform laneParent = side == Side.Player1 ? player1Lanes[laneIndex] : player2Lanes[laneIndex];
        if (laneParent.childCount > 0)
        {
            Debug.Log("Lane already occupied.");
            return;
        }

        var hand = GetHand(side);
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            Debug.Log("Invalid hand index.");
            return;
        }

        DeckCardDto cardData = hand[handIndex];

        // ZIEL check
        var zielCurrentVar = GetZielCurrent(side);
        if (zielCurrentVar.Value < cardData.cost)
        {
            Debug.Log("Not enough ziel.");
            return;
        }

        // Spend ziel
        zielCurrentVar.Value -= cardData.cost;

        // Remove from hand and update client UI
        hand.RemoveAt(handIndex);
        SendHandToClient(side);

        // Spawn the minion on the board
        SpawnCardInLane(laneParent, cardData);

        // Combat in this lane
        ResolveLaneCombat(laneIndex);

        // Direct face damage from any unblocked cards on this side
        ApplyDirectDamageForSide(side);

        // Check win before giving the next player a turn
        if (!CheckForWin())
        {
            AdvanceTurn();
        }
    }

    private void DrawCards(Side side, int count)
    {
        if (!IsServer) return;

        var deck = GetDeck(side);
        var hand = GetHand(side);

        Debug.Log($"[GM] DrawCards({side}, {count}) before draw: deck={deck.Count}, hand={hand.Count}");

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
                break;

            var card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
        }

        Debug.Log($"[GM] DrawCards({side}) after draw: deck={deck.Count}, hand={hand.Count}");

        SendHandToClient(side);
    }


    private void SendHandToClient(Side side)
    {
        if (!IsServer)
            return;

        var hand = GetHand(side);
        HandCardData[] data = new HandCardData[hand.Count];

        for (int i = 0; i < hand.Count; i++)
        {
            var c = hand[i];

            //  Guarantee no null strings go into HandCardData
            string uid = string.IsNullOrEmpty(c.card_uid) ? string.Empty : c.card_uid;
            string name = string.IsNullOrEmpty(c.name) ? uid : c.name; // fallback: show uid as name

            data[i] = new HandCardData
            {
                card_uid = uid,
                name = name,
                cost = c.cost,
                attack = c.attack,
                health = c.health
            };
        }

        Debug.Log($"[GM] SendHandToClient({side}) sending {data.Length} cards");

        ulong targetClientId = side == Side.Player1 ? 0UL : 1UL;
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientId }
            }
        };

        UpdateHandClientRpc(data, side, rpcParams);
    }

    [ClientRpc]
    private void UpdateHandClientRpc(HandCardData[] cards, Side side, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[GM] UpdateHandClientRpc on client {NetworkManager.Singleton.LocalClientId} for side {side} with {cards.Length} cards");

        if (GetLocalSide() != side)
        {
            Debug.Log("[GM] This client is not that side; ignoring hand update.");
            return;
        }

        if (HandUI.Instance != null)
        {
            Debug.Log("[GM] Calling HandUI.SetHand");
            HandUI.Instance.SetHand(cards);
        }
        else
        {
            Debug.LogWarning("[GM] HandUI.Instance is NULL");
        }
    }


    // -------------------------------------------------------------------------
    // Turn management
    // -------------------------------------------------------------------------

    private void HandleTurnChanged(Side oldTurn, Side newTurn)
    {
        OnTurnChanged?.Invoke(newTurn);
    }

    private void AdvanceTurn()
    {
        if (!IsServer) return;

        Side next = currentTurn.Value == Side.Player1 ? Side.Player2 : Side.Player1;
        currentTurn.Value = next;

        StartTurn(next);
    }

    private void StartTurn(Side side)
    {
        if (!IsServer) return;

        var zielMaxVar = GetZielMax(side);
        var zielCurVar = GetZielCurrent(side);

        if (zielMaxVar.Value < zielCap)
            zielMaxVar.Value++;

        zielCurVar.Value = zielMaxVar.Value;

        // Draw 1 card at the start of your turn
        DrawCards(side, 1);
    }

    // -------------------------------------------------------------------------
    // Combat & win logic
    // -------------------------------------------------------------------------

    private void SpawnCardInLane(Transform laneParent, DeckCardDto data)
    {
        GameObject card = Instantiate(cardPrefab, laneParent);
        card.transform.localPosition = Vector3.zero;

        var cardNet = card.GetComponent<CardNetwork>();
        if (cardNet != null)
        {
            cardNet.baseAttack = data.attack;
            cardNet.baseHealth = data.health;
            cardNet.cardName = data.name;

            // Local sprite lookup by card_uid
            var sprite = Resources.Load<Sprite>($"Cards/{data.card_uid}");
            if (sprite != null && cardNet.artImage != null)
            {
                cardNet.artImage.sprite = sprite;
            }
        }

        var netObj = card.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }
        else
        {
            Debug.LogError("Card prefab is missing NetworkObject.");
            Destroy(card);
        }
    }

    private void ResolveLaneCombat(int laneIndex)
    {
        Transform p1Lane = player1Lanes[laneIndex];
        Transform p2Lane = player2Lanes[laneIndex];

        CardNetwork p1Card = p1Lane.GetComponentInChildren<CardNetwork>();
        CardNetwork p2Card = p2Lane.GetComponentInChildren<CardNetwork>();

        if (p1Card == null || p2Card == null)
            return;

        int p1Atk = p1Card.Attack.Value;
        int p2Atk = p2Card.Attack.Value;

        p1Card.TakeDamageServerRpc(p2Atk);
        p2Card.TakeDamageServerRpc(p1Atk);

        Debug.Log($"Combat in lane {laneIndex}: P1({p1Atk}) vs P2({p2Atk})");
    }

    /// <summary>
    /// Active side's unblocked cards in lanes deal direct damage to the opponent.
    /// </summary>
    private void ApplyDirectDamageForSide(Side activeSide)
    {
        if (!IsServer) return;

        for (int i = 0; i < player1Lanes.Length; i++)
        {
            Transform myLane = activeSide == Side.Player1 ? player1Lanes[i] : player2Lanes[i];
            Transform theirLane = activeSide == Side.Player1 ? player2Lanes[i] : player1Lanes[i];

            var myCard = myLane.GetComponentInChildren<CardNetwork>();
            var theirCard = theirLane.GetComponentInChildren<CardNetwork>();

            if (myCard != null && theirCard == null)
            {
                int dmg = myCard.Attack.Value;
                var oppHp = GetHp(activeSide == Side.Player1 ? Side.Player2 : Side.Player1);
                oppHp.Value -= dmg;
                Debug.Log($"Direct damage: {activeSide} deals {dmg} to opponent (lane {i})");
            }
        }
    }

    /// <returns>true if game ended</returns>
    private bool CheckForWin()
    {
        if (!IsServer) return false;

        if (p1Hp.Value <= 0 && p2Hp.Value <= 0) //Player health above 0? 
        {
            GameOverClientRpc(Side.Player1); //Tie
            return true;
        }
        else if (p1Hp.Value <= 0)
        {
            GameOverClientRpc(Side.Player2); //Player 2 wins!
            return true;
        }
        else if (p2Hp.Value <= 0)
        {
            GameOverClientRpc(Side.Player1); // Player 1 wins!
            return true;
        }

        return false; // Play ball
    }

    [ClientRpc]
    private void GameOverClientRpc(Side winner, ClientRpcParams rpcParams = default)
    {
        OnGameOver?.Invoke(winner); //No logic here yet, still need to get everything populated and working correctly. 
    }

   //Helper Accessors assigned to players

    private List<DeckCardDto> GetDeck(Side side) =>
        side == Side.Player1 ? deckP1 : deckP2;

    private List<DeckCardDto> GetHand(Side side) =>
        side == Side.Player1 ? handP1 : handP2;

    private NetworkVariable<int> GetHp(Side side) =>
        side == Side.Player1 ? p1Hp : p2Hp;

    private NetworkVariable<int> GetZielCurrent(Side side) =>
        side == Side.Player1 ? p1ZielCurrent : p2ZielCurrent;

    private NetworkVariable<int> GetZielMax(Side side) =>
        side == Side.Player1 ? p1ZielMax : p2ZielMax;
}
