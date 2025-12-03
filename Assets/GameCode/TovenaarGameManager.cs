using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum Side
{
    Player1 = 0,
    Player2 = 1
}

// Lightweight hand payload sent from server -> client
[Serializable]
public struct HandCardData : INetworkSerializable
{
    public string card_uid;
    public string name;
    public int cost;
    public int attack;
    public int health;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref card_uid);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref attack);
        serializer.SerializeValue(ref health);
    }
}

/// <summary>
/// Server-side game authority for a single match.
/// Lives in the MatchScene next to NetworkManager.
/// </summary>
public class TovenaarGameManager : NetworkBehaviour
{
    // -------------------------------------------------------------------------
    // Static helpers
    // -------------------------------------------------------------------------

    public static TovenaarGameManager Instance { get; private set; }
    public static System.Random rng = new System.Random();

    public static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    // -------------------------------------------------------------------------
    // Events (used by UI)
    // -------------------------------------------------------------------------

    public static Action<Side> OnTurnChanged;
    public static Action<Side> OnGameOver;
    public static Action<string> OnActionLog;

    // -------------------------------------------------------------------------
    // Inspector references
    // -------------------------------------------------------------------------

    [Header("Board Layout (scene transforms only)")]
    public Transform[] player1Lanes;   // bottom row in scene
    public Transform[] player2Lanes;   // top row in scene

    [Header("Prefabs")]
    // Logic prefab for cards on the board (must have NetworkObject + NetworkCard)
    public NetworkCard networkCardPrefab;

    // Optional: used by whatever script spawns the visual board card
    public GameObject boardCardVisualPrefab;

    // -------------------------------------------------------------------------
    // Match config
    // -------------------------------------------------------------------------

    public int startingHp = 20;
    public int zielCap = 10;

    // Round = P1 turn -> P2 turn -> combat
    private int roundNumber = 1;

    // Mana Surge status (per side)
    private int manaSurgeTurnsP1 = 0;
    private int manaSurgeAmountP1 = 0;
    private int manaSurgeTurnsP2 = 0;
    private int manaSurgeAmountP2 = 0;

    // -------------------------------------------------------------------------
    // Networked state (visible to all, writable by server)
    // -------------------------------------------------------------------------

    private NetworkVariable<Side> currentTurn = new NetworkVariable<Side>(
        Side.Player1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p1Hp = new NetworkVariable<int>(
        20,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p2Hp = new NetworkVariable<int>(
        20,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p1ZielCurrent = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p2ZielCurrent = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p1ZielMax = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p2ZielMax = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // -------------------------------------------------------------------------
    // Server-only state
    // -------------------------------------------------------------------------

    // Full decks as loaded from DB
    private readonly List<DeckCardDto> deckP1 = new List<DeckCardDto>();
    private readonly List<DeckCardDto> deckP2 = new List<DeckCardDto>();

    // Hands tracked only on server, mirrored to clients through HandUI
    private readonly List<DeckCardDto> handP1 = new List<DeckCardDto>();
    private readonly List<DeckCardDto> handP2 = new List<DeckCardDto>();

    // Cards currently on each row (by lane index)
    private NetworkCard[] boardP1;
    private NetworkCard[] boardP2;

    private bool gameStarted = false;

    // -------------------------------------------------------------------------
    // Unity / Netcode lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;

        int p1Len = player1Lanes != null ? player1Lanes.Length : 0;
        int p2Len = player2Lanes != null ? player2Lanes.Length : 0;

        boardP1 = new NetworkCard[p1Len];
        boardP2 = new NetworkCard[p2Len];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        currentTurn.OnValueChanged += HandleTurnChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentTurn.OnValueChanged -= HandleTurnChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // -------------------------------------------------------------------------
    // Connection & game start
    // -------------------------------------------------------------------------

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || NetworkManager.Singleton == null)
            return;

        int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (connectedCount == 2)
        {
            TryStartGame();
        }
    }

    /// <summary>
    /// Starts the match once both decks are submitted and both players are connected.
    /// </summary>
    private void TryStartGame()
    {
        if (!IsServer || gameStarted)
            return;

        int players = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.ConnectedClientsIds.Count
            : -1;

        if (deckP1.Count == 0 || deckP2.Count == 0)
            return;
        if (players < 2)
            return;

        gameStarted = true;

        // HP + initial Ziel
        p1Hp.Value = startingHp;
        p2Hp.Value = startingHp;

        p1ZielMax.Value = 1;
        p2ZielMax.Value = 1;
        p1ZielCurrent.Value = 1;
        p2ZielCurrent.Value = 1;

        // Opening hands
        DrawCards(Side.Player1, 5);
        DrawCards(Side.Player2, 5);

        // Host (P1) goes first
        roundNumber = 1;
        currentTurn.Value = Side.Player1;
        OnTurnChanged?.Invoke(Side.Player1);

        StartTurn(Side.Player1);

        Debug.Log("GAME STARTED: Player1's turn.");
        LogAction("Game started. Player1's turn.");
    }

    /// <summary>
    /// Called by deck loader after DB fetch.
    /// </summary>
    public void SetDeckForSide(Side side, DeckCardDto[] cards)
    {
        if (!IsServer)
            return;

        List<DeckCardDto> deck = GetDeck(side);
        deck.Clear();
        if (cards != null)
            deck.AddRange(cards);

        Shuffle(deck);
        Debug.Log($"[GM] SetDeckForSide({side}) deck size = {deck.Count}");

        TryStartGame();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitDeckServerRpc(DeckCardDto[] cards, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning("[GM] SubmitDeckServerRpc received empty deck.");
            return;
        }

        ulong senderId = rpcParams.Receive.SenderClientId;
        Side side = (senderId == 0) ? Side.Player1 : Side.Player2;

        Debug.Log($"[GM] SubmitDeckServerRpc from client {senderId} mapped to side {side} " +
                  $"with {cards.Length} cards.");

        SetDeckForSide(side, cards);
    }

    // -------------------------------------------------------------------------
    // Hand / draw
    // -------------------------------------------------------------------------

    private void DrawCards(Side side, int count)
    {
        if (!IsServer)
            return;

        List<DeckCardDto> deck = GetDeck(side);
        List<DeckCardDto> hand = GetHand(side);

        Debug.Log($"[GM] DrawCards({side}, {count}) before draw: deck={deck.Count}, hand={hand.Count}");

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
                break;

            DeckCardDto card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
        }

        Debug.Log($"[GM] DrawCards({side}) after draw: deck={deck.Count}, hand={hand.Count}");
        SendHandToClient(side);
    }

    /// <summary>
    /// Sends the entire hand for a side down to that client.
    /// </summary>
    private void SendHandToClient(Side side)
    {
        if (!IsServer)
            return;

        List<DeckCardDto> hand = GetHand(side);
        HandCardData[] data = new HandCardData[hand.Count];

        for (int i = 0; i < hand.Count; i++)
        {
            DeckCardDto c = hand[i];

            string uid = string.IsNullOrEmpty(c.card_uid) ? string.Empty : c.card_uid;
            string displayName = string.IsNullOrEmpty(c.name) ? uid : c.name;

            data[i] = new HandCardData
            {
                card_uid = uid,
                name = displayName,
                cost = c.cost,
                attack = c.attack,
                health = c.health
            };
        }

        Debug.Log($"[GM] SendHandToClient({side}) sending {data.Length} cards");

        ulong targetClientId = (side == Side.Player1) ? 0UL : 1UL;

        ClientRpcParams rpcParams = new ClientRpcParams
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
        Debug.Log($"[GM] UpdateHandClientRpc on client {NetworkManager.Singleton.LocalClientId} " +
                  $"for side {side} with {cards.Length} cards");

        Side localSide = GetLocalSide();
        if (localSide != side)
        {
            Debug.Log("[GM] This client is not that side; ignoring hand update.");
            return;
        }

        if (HandUI.Instance != null)
        {
            HandUI.Instance.SetHand(cards);
        }
        else
        {
            Debug.LogWarning("[GM] HandUI.Instance is NULL");
        }
    }

    private void AddCardToHand(Side side, DeckCardDto card)
    {
        if (!IsServer)
            return;

        List<DeckCardDto> hand = GetHand(side);
        hand.Add(card);
        SendHandToClient(side);
    }

    // Used by Wolf Den to generate wolves on the fly
    private DeckCardDto CreateForestWolfDto()
    {
        return new DeckCardDto
        {
            card_id = 0,
            card_uid = "FOREST_WOLF",
            name = "Forest Wolf",
            type = "Monster",
            cost = 2,
            health = 4,
            attack = 3,
            rarity = "Common",
            rules_text = "Target: Single Enemy"
        };
    }

    // -------------------------------------------------------------------------
    // Turn management
    // -------------------------------------------------------------------------

    private void HandleTurnChanged(Side oldTurn, Side newTurn)
    {
        OnTurnChanged?.Invoke(newTurn);
    }

    /// <summary>
    /// Called by UI when player hits "End Turn".
    /// </summary>
    public void RequestEndTurn()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        Side mySide = GetLocalSide();
        if (mySide != currentTurn.Value)
        {
            ErrorUI.Instance?.ShowError("It's not your turn!");
            return;
        }

        EndTurnServerRpc(mySide);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc(Side requestingSide)
    {
        if (!IsServer)
            return;

        if (currentTurn.Value != requestingSide)
        {
            Debug.Log("EndTurnServerRpc rejected: wrong side.");
            return;
        }

        ApplyEndOfTurnPassives(requestingSide);

        if (requestingSide == Side.Player1)
        {
            // Pass to Player2
            currentTurn.Value = Side.Player2;
            OnTurnChanged?.Invoke(Side.Player2);
            StartTurn(Side.Player2);
            LogAction("Player1 ended their turn. Player2's turn begins.");
        }
        else
        {
            // Player2 turn -> combat -> win check -> next round
            LogAction("Player2 ended their turn. Combat phase begins.");

            ResolveEndOfRoundCombat();

            if (CheckForWin())
                return;

            roundNumber++;
            currentTurn.Value = Side.Player1;
            OnTurnChanged?.Invoke(Side.Player1);
            StartTurn(Side.Player1);
            LogAction($"Round {roundNumber} begins. Player1's turn.");
        }
    }

   
    // Server side start-of-turn logic: Ziel, draw, passives.
    
    private void StartTurn(Side side)
    {
        if (!IsServer)
            return;

        NetworkVariable<int> zielMaxVar = GetZielMax(side);
        NetworkVariable<int> zielCurVar = GetZielCurrent(side);

        // Increase max Ziel each round (after round 1), up to cap
        if (roundNumber > 1 && zielMaxVar.Value < zielCap)
        {
            zielMaxVar.Value++;
        }

        // Refill to max
        zielCurVar.Value = zielMaxVar.Value;

        // Draw one card every turn
        DrawCards(side, 1);

        // Board passives + temporary mana buffs
        ApplyStartOfTurnPassives(side);
        ApplyManaSurgeBonus(side);

        Debug.Log($"[GM] StartTurn({side}) round={roundNumber}, zielMax={zielMaxVar.Value}, zielCur={zielCurVar.Value}");
        LogAction($"{side} turn begins. Ziel = {zielCurVar.Value}/{zielMaxVar.Value}.");
    }

    
    // Creatures/buildings that tick at start of the owner's turn.
    
    private void ApplyStartOfTurnPassives(Side side)
    {
        if (!IsServer)
            return;

        NetworkCard[] myBoard = GetBoardArray(side);
        if (myBoard == null)
            return;

        int swampWitchCount = 0;
        int crystalTowerCount = 0;
        int wolfDenCount = 0;

        foreach (var card in myBoard)
        {
            if (card == null) continue;
            var uid = card.CardUid.Value;

            if (uid == "SWAMP_WITCH")
                swampWitchCount++;
            else if (uid == "CRYSTAL_TOWER")
                crystalTowerCount++;
            else if (uid == "WOLF_DEN")
                wolfDenCount++;
        }

        // Swamp Witch: drain Ziel from enemy, give to you
        if (swampWitchCount > 0)
        {
            Side enemySide = GetOpponent(side);
            var enemyZiel = GetZielCurrent(enemySide);
            var myZiel = GetZielCurrent(side);
            var myZielMax = GetZielMax(side);

            int totalDrain = swampWitchCount;
            int actualDrain = Math.Min(enemyZiel.Value, totalDrain);

            if (actualDrain > 0)
            {
                enemyZiel.Value -= actualDrain;

                int beforeMine = myZiel.Value;
                myZiel.Value = Math.Min(myZielMax.Value, myZiel.Value + actualDrain);
                int gained = myZiel.Value - beforeMine;

                LogAction($"{side}'s Swamp Witch drains {actualDrain} Ziel from the enemy (you gain {gained}).");
            }
        }

        // Crystal Tower: flat Ziel income up to max
        if (crystalTowerCount > 0)
        {
            var myZiel = GetZielCurrent(side);
            var myZielMax = GetZielMax(side);

            int before = myZiel.Value;
            myZiel.Value = Math.Min(myZielMax.Value, myZiel.Value + crystalTowerCount);
            int gained = myZiel.Value - before;

            if (gained > 0)
            {
                LogAction($"{side}'s Crystal Tower generates {gained} extra Ziel.");
            }
        }

        // Wolf Den: adds Forest Wolf cards to hand
        if (wolfDenCount > 0)
        {
            for (int i = 0; i < wolfDenCount; i++)
            {
                AddCardToHand(side, CreateForestWolfDto());
            }

            LogAction($"{side}'s Wolf Den adds {wolfDenCount} Forest Wolf card(s) to hand.");
        }
    }

    
    // One-shot Ziel bonus from Mana Surge.
    
    private void ApplyManaSurgeBonus(Side side)
    {
        if (!IsServer)
            return;

        int turns, amount;
        if (side == Side.Player1)
        {
            turns = manaSurgeTurnsP1;
            amount = manaSurgeAmountP1;
        }
        else
        {
            turns = manaSurgeTurnsP2;
            amount = manaSurgeAmountP2;
        }

        if (turns <= 0 || amount <= 0)
            return;

        var zielCurVar = GetZielCurrent(side);

        int before = zielCurVar.Value;
        zielCurVar.Value = Mathf.Min(zielCap, zielCurVar.Value + amount);
        int gained = zielCurVar.Value - before;

        if (gained > 0)
            LogAction($"{side}'s Mana Surge grants {gained} extra Ziel this turn.");

        // Decrement duration
        if (side == Side.Player1)
        {
            manaSurgeTurnsP1--;
            if (manaSurgeTurnsP1 <= 0) manaSurgeAmountP1 = 0;
        }
        else
        {
            manaSurgeTurnsP2--;
            if (manaSurgeTurnsP2 <= 0) manaSurgeAmountP2 = 0;
        }
    }

    // -------------------------------------------------------------------------
    // Card play (requests from client -> server resolution)
    // -------------------------------------------------------------------------

    
    // Called by board UI when player clicks a lane to play the selected hand card.
    
    public void RequestPlayCard(int laneIndex, bool isPlayer1Lane, int handIndex)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        Side mySide = GetLocalSide();

        if (mySide != currentTurn.Value)
        {
            ErrorUI.Instance?.ShowError("It's not your turn!");
            return;
        }

        if (handIndex < 0)
        {
            ErrorUI.Instance?.ShowError("Select a card first.");
            return;
        }

        // Always send click to server; server decides if target is valid
        PlayCardServerRpc(laneIndex, mySide, handIndex, isPlayer1Lane);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayCardServerRpc(
        int laneIndex,
        Side side,
        int handIndex,
        bool isPlayer1Lane,
        ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        List<DeckCardDto> hand = GetHand(side);
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            Debug.Log("[GM] Invalid hand index.");
            return;
        }

        DeckCardDto cardData = hand[handIndex];
        bool isSpell = IsSpellCard(cardData);

        bool isMyRow = (side == Side.Player1 && isPlayer1Lane) ||
                       (side == Side.Player2 && !isPlayer1Lane);

        // Creatures/buildings must be on your own row
        if (!isSpell && !isMyRow)
        {
            SendErrorToSide(side, "You can only play creatures/buildings on your own row.");
            return;
        }

        // SPELLS ----------------------------------------------------------------
        if (isSpell)
        {
            NetworkVariable<int> zielCurrentVarSpell = GetZielCurrent(side);
            if (zielCurrentVarSpell.Value < cardData.cost)
            {
                Debug.Log("[GM] Not enough ziel for spell.");
                SendErrorToSide(side, "Not enough Ziel.");
                return;
            }

            zielCurrentVarSpell.Value -= cardData.cost;

            // Remove spell from hand and sync
            hand.RemoveAt(handIndex);
            SendHandToClient(side);

            LogAction($"{side} casts spell {cardData.name}.");

            ResolveSpellCard(side, cardData, laneIndex, isPlayer1Lane);

            // Spells might kill a player
            CheckForWin();
            return;
        }

        // CREATURES / BUILDINGS -------------------------------------------------

        NetworkCard[] laneArray = GetBoardArray(side);
        if (laneArray == null || laneIndex < 0 || laneIndex >= laneArray.Length)
        {
            Debug.LogWarning("[GM] PlayCardServerRpc: invalid lane index.");
            return;
        }

        if (laneArray[laneIndex] != null)
        {
            Debug.Log("[GM] Lane already occupied (server check).");
            SendErrorToSide(side, "That lane is already occupied.");
            return;
        }

        NetworkVariable<int> zielCurrentVar = GetZielCurrent(side);
        if (zielCurrentVar.Value < cardData.cost)
        {
            Debug.Log("[GM] Not enough ziel.");
            SendErrorToSide(side, "Not enough Ziel.");
            return;
        }

        zielCurrentVar.Value -= cardData.cost;

        hand.RemoveAt(handIndex);
        SendHandToClient(side);

        NetworkCard card = SpawnLogicCardInLane(side, laneIndex, cardData);
        if (card == null)
        {
            Debug.LogError("[GM] Failed to spawn NetworkCard.");
            return;
        }

        laneArray[laneIndex] = card;

        LogAction($"{side} played {cardData.name} into lane {laneIndex}.");

        // Sneaky Fox: surprise attack when played
        if (cardData.card_uid == "SNEAKY_FOX")
        {
            LogAction($"{side}'s Sneaky Fox attacks immediately when summoned in lane {laneIndex}.");
            ResolveLaneCombat(laneIndex);
            CheckForWin();
        }
    }

    private NetworkCard SpawnLogicCardInLane(Side side, int laneIndex, DeckCardDto data)
    {
        if (!IsServer)
            return null;

        if (networkCardPrefab == null)
        {
            Debug.LogError("[GM] networkCardPrefab is NULL.");
            return null;
        }

        NetworkCard card = Instantiate(networkCardPrefab, transform);
        NetworkObject no = card.GetComponent<NetworkObject>();

        if (no == null)
        {
            Debug.LogError("[GM] NetworkCard prefab missing NetworkObject.");
            Destroy(card.gameObject);
            return null;
        }

        // Initialize server-side stats before spawn
        card.ServerInitFromDto(data, side, laneIndex);

        no.Spawn(true);

        Debug.Log($"[GM] SpawnLogicCardInLane: Spawned {data.card_uid} for {side} lane {laneIndex}");
        return card;
    }

    private void SendErrorToSide(Side side, string msg)
    {
        ulong targetClientId = (side == Side.Player1) ? 0UL : 1UL;

        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientId }
            }
        };

        ShowErrorClientRpc(msg, side, rpcParams);
    }

    [ClientRpc]
    private void ShowErrorClientRpc(string message, Side side, ClientRpcParams clientRpcParams = default)
    {
        if (ErrorUI.Instance != null && GetLocalSide() == side)
        {
            ErrorUI.Instance.ShowError(message);
        }
    }

    // -------------------------------------------------------------------------
    // Combat & win logic
    // -------------------------------------------------------------------------

    
    // Full combat step: lane vs lane trades, then direct damage.
    // Called when P2 ends their turn. Might change later, we will see
    
    private void ResolveEndOfRoundCombat()
    {
        int laneCount = Mathf.Min(boardP1.Length, boardP2.Length);
        TickFrozenCounters();

        // Lane trades
        for (int i = 0; i < laneCount; i++)
        {
            ResolveLaneCombat(i);
        }

        // Direct damage from unblocked lanes
        ApplyEndOfRoundDirectDamage();
    }

    
    // Resolves a single lane's creature vs creature trade.
   
    private void ResolveLaneCombat(int laneIndex)
    {
        NetworkCard p1Card = (laneIndex >= 0 && laneIndex < boardP1.Length) ? boardP1[laneIndex] : null;
        NetworkCard p2Card = (laneIndex >= 0 && laneIndex < boardP2.Length) ? boardP2[laneIndex] : null;

        if (p1Card == null || p2Card == null)
            return;

        int p1Atk = (p1Card.FrozenForRounds.Value > 0) ? 0 : p1Card.Attack.Value;
        int p2Atk = (p2Card.FrozenForRounds.Value > 0) ? 0 : p2Card.Attack.Value;

        p1Card.TakeDamageServerRpc(p2Atk);
        p2Card.TakeDamageServerRpc(p1Atk);

        Debug.Log($"[GM] Combat in lane {laneIndex}: P1({p1Atk}) vs P2({p2Atk})");
        LogAction($"Lane {laneIndex}: P1 ({p1Atk} ATK) and P2 ({p2Atk} ATK) trade damage.");
    }

   
    // Handles direct hits on players from lanes where only one side has a card.
    
    private void ApplyEndOfRoundDirectDamage()
    {
        if (!IsServer)
            return;

        int laneCount = Mathf.Min(boardP1.Length, boardP2.Length);

        for (int i = 0; i < laneCount; i++)
        {
            NetworkCard p1Card = (i >= 0 && i < boardP1.Length) ? boardP1[i] : null;
            NetworkCard p2Card = (i >= 0 && i < boardP2.Length) ? boardP2[i] : null;

            if (p1Card != null && p2Card == null)
            {
                int dmg = (p1Card.FrozenForRounds.Value > 0) ? 0 : p1Card.Attack.Value;
                p2Hp.Value -= dmg;
                Debug.Log($"[GM] Direct damage: Player1 deals {dmg} to Player2 in lane {i}");
                LogAction($"Player1's card in lane {i} deals {dmg} damage to Player2.");
            }

            if (p2Card != null && p1Card == null)
            {
                int dmg = (p2Card.FrozenForRounds.Value > 0) ? 0 : p2Card.Attack.Value;
                p1Hp.Value -= dmg;
                Debug.Log($"[GM] Direct damage: Player2 deals {dmg} to Player1 in lane {i}");
                LogAction($"Player2's card in lane {i} deals {dmg} damage to Player1.");
            }
        }
    }
    private void ApplyEndOfTurnPassives(Side sideEndingTurn)
    {
        if (!IsServer)
            return;

        NetworkCard[] myBoard = GetBoardArray(sideEndingTurn);
        NetworkCard[] enemyBoard = GetBoardArray(GetOpponent(sideEndingTurn));

        if (myBoard == null || enemyBoard == null)
            return;

        bool hasStormGriffin = false;

        foreach (var card in myBoard)
        {
            if (card == null) continue;
            if (card.CardUid.Value == "STORM_GRIFFIN")
            {
                hasStormGriffin = true;
                break;
            }
        }

        if (hasStormGriffin)
        {
            for (int i = 0; i < enemyBoard.Length; i++)
            {
                var enemyCard = enemyBoard[i];
                if (enemyCard != null)
                {
                    enemyCard.TakeDamageServerRpc(1);
                }
            }

            LogAction($"{sideEndingTurn}'s Storm Griffin deals 1 damage to all enemy cards at the end of its turn.");
        }
    }

    private bool CheckForWin()
    {
        if (!IsServer)
            return false;

        if (p1Hp.Value <= 0 && p2Hp.Value <= 0)
        {
            LogAction("Both players reached 0 HP. Tie-breaker: Player1 wins.");
            GameOverClientRpc(Side.Player1);
            return true;
        }

        if (p1Hp.Value <= 0)
        {
            LogAction("Player1 reached 0 HP. Player2 wins.");
            GameOverClientRpc(Side.Player2);
            return true;
        }

        if (p2Hp.Value <= 0)
        {
            LogAction("Player2 reached 0 HP. Player1 wins.");
            GameOverClientRpc(Side.Player1);
            return true;
        }

        return false;
    }

    [ClientRpc]
    private void GameOverClientRpc(Side winner, ClientRpcParams rpcParams = default)
    {
        OnGameOver?.Invoke(winner);
    }

    private void TickFrozenCounters()
    {
        if (!IsServer)
            return;

        void TickBoard(NetworkCard[] board)
        {
            if (board == null) return;
            foreach (var c in board)
            {
                if (c == null) continue;
                int f = c.FrozenForRounds.Value;
                if (f > 0)
                    c.FrozenForRounds.Value = f - 1;
            }
        }

        TickBoard(boardP1);
        TickBoard(boardP2);
    }

    // -------------------------------------------------------------------------
    // Spell helpers
    // -------------------------------------------------------------------------

    private int GetSpellDamageBonus(Side side)
    {
        NetworkCard[] board = GetBoardArray(side);
        if (board == null) return 0;

        int bonus = 0;
        foreach (var card in board)
        {
            if (card == null) continue;
            if (card.CardUid.Value == "RUNE_GUARDIAN")
                bonus += 1;
        }
        return bonus;
    }

    private int GetSpellEffectMultiplier(Side side)
    {
        NetworkCard[] board = GetBoardArray(side);
        if (board == null) return 1;

        int mult = 1;
        foreach (var card in board)
        {
            if (card == null) continue;
            if (card.CardUid.Value == "RUNE_STONE")
                mult *= 2;
        }
        return mult;
    }

    
    // Applies spell damage bonus and effect multiplier.
    
    private int ComputeSpellAmount(Side casterSide, int baseAmount, bool isDamage)
    {
        int amount = baseAmount;
        if (isDamage)
            amount += GetSpellDamageBonus(casterSide);

        int mult = GetSpellEffectMultiplier(casterSide);
        amount *= mult;

        return Mathf.Max(0, amount);
    }

    private bool IsImmuneToSpells(NetworkCard card)
    {
        return card != null && card.CardUid.Value == "SILVER_DRAGON";
    }

    private bool IsSpellCard(DeckCardDto card)
    {
        return card.type != null &&
               card.type.Equals("Spell", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsBuildingCard(DeckCardDto card)
    {
        return card.type != null &&
               card.type.Equals("Building", StringComparison.OrdinalIgnoreCase);
    }

    
    // Resolves spell effects. Target is defined by laneIndex + which row was clicked.
   
    private void ResolveSpellCard(Side casterSide, DeckCardDto card, int laneIndex, bool isPlayer1LaneTarget)
    {
        if (!IsServer)
            return;

        Side enemySide = GetOpponent(casterSide);
        NetworkCard[] myBoard = GetBoardArray(casterSide);
        NetworkCard[] enemyBoard = GetBoardArray(enemySide);

        Side clickedSide = isPlayer1LaneTarget ? Side.Player1 : Side.Player2;
        NetworkCard[] clickedBoard = GetBoardArray(clickedSide);

        NetworkCard targetCard = null;
        if (clickedBoard != null &&
            laneIndex >= 0 &&
            laneIndex < clickedBoard.Length)
        {
            targetCard = clickedBoard[laneIndex];
        }

        string uid = card.card_uid;

        switch (uid)
        {
            // HEALING MIST – Heal all friendly monsters
            case "HEALING_MIST":
                {
                    int heal = ComputeSpellAmount(casterSide, 3, isDamage: false);

                    if (myBoard != null)
                    {
                        foreach (var c in myBoard)
                        {
                            if (c == null) continue;
                            if (IsImmuneToSpells(c)) continue;
                            c.HealServerRpc(heal);
                        }
                    }

                    LogAction($"{casterSide} casts Healing Mist, restoring {heal} health to all friendly monsters.");
                    break;
                }

            // ICE GRIP – Freeze an enemy monster
            case "ICE_GRIP":
                {
                    if (targetCard == null)
                    {
                        LogAction($"{casterSide} casts Ice Grip, but no target was selected.");
                        break;
                    }

                    if (clickedSide == casterSide)
                    {
                        LogAction($"{casterSide} casts Ice Grip on their own row; spell fizzles.");
                        break;
                    }

                    if (IsImmuneToSpells(targetCard))
                    {
                        LogAction($"{casterSide} casts Ice Grip, but the target is immune to spells.");
                        break;
                    }

                    int rounds = ComputeSpellAmount(casterSide, 1, isDamage: false);
                    targetCard.SetFrozenServerRpc(rounds);

                    LogAction($"{casterSide} casts Ice Grip, freezing the enemy monster in lane {laneIndex} for {rounds} round(s).");
                    break;
                }

            // SWEET TREAT – Buff one allied creature
            case "SWEET_TREAT":
                {
                    if (targetCard == null)
                    {
                        LogAction($"{casterSide} casts Sweet Treat, but no target was selected.");
                        break;
                    }

                    if (clickedSide != casterSide)
                    {
                        LogAction($"{casterSide} casts Sweet Treat on an enemy; spell fizzles.");
                        break;
                    }

                    if (IsImmuneToSpells(targetCard))
                    {
                        LogAction($"{casterSide} casts Sweet Treat, but the target is immune to spells.");
                        break;
                    }

                    int buff = ComputeSpellAmount(casterSide, 2, isDamage: false);

                    targetCard.MaxHealth.Value += buff;
                    targetCard.Health.Value += buff;
                    targetCard.Attack.Value += buff;

                    LogAction($"{casterSide} casts Sweet Treat, giving +{buff}/+{buff} to the allied creature in lane {laneIndex}.");
                    break;
                }

            // LIGHTNING BOLT – Single-target damage
            case "LIGHTNING_BOLT":
                {
                    if (targetCard == null)
                    {
                        LogAction($"{casterSide} casts Lightning Bolt, but no target was selected.");
                        break;
                    }

                    if (clickedSide == casterSide)
                    {
                        LogAction($"{casterSide} casts Lightning Bolt on their own row; spell fizzles.");
                        break;
                    }

                    if (IsImmuneToSpells(targetCard))
                    {
                        LogAction($"{casterSide} casts Lightning Bolt, but the target is immune to spells.");
                        break;
                    }

                    int dmg = ComputeSpellAmount(casterSide, 4, isDamage: true);
                    targetCard.TakeDamageServerRpc(dmg);

                    LogAction($"{casterSide} casts Lightning Bolt, dealing {dmg} damage to the enemy in lane {laneIndex}.");
                    break;
                }

            // MANA SURGE – temporary Ziel income buff
            case "MANA_SURGE":
                {
                    int perTurn = ComputeSpellAmount(casterSide, 3, isDamage: false);
                    int duration = 3; // effect length stays fixed

                    if (casterSide == Side.Player1)
                    {
                        manaSurgeTurnsP1 = duration;
                        manaSurgeAmountP1 = perTurn;
                    }
                    else
                    {
                        manaSurgeTurnsP2 = duration;
                        manaSurgeAmountP2 = perTurn;
                    }

                    LogAction($"{casterSide} casts Mana Surge, gaining +{perTurn} Ziel for the next {duration} turns.");
                    break;
                }

            // RAIN OF FIRE – AoE damage
            case "RAIN_OF_FIRE":
                {
                    int dmg = ComputeSpellAmount(casterSide, 3, isDamage: true);

                    if (enemyBoard != null)
                    {
                        for (int i = 0; i < enemyBoard.Length; i++)
                        {
                            var enemyCard = enemyBoard[i];
                            if (enemyCard == null) continue;
                            if (IsImmuneToSpells(enemyCard)) continue;

                            enemyCard.TakeDamageServerRpc(dmg);
                        }
                    }

                    LogAction($"{casterSide} casts Rain of Fire, dealing {dmg} damage to all enemy monsters.");
                    break;
                }

            // CLOAK TRICK – unconditional kill
            case "CLOAK_TRICK":
                {
                    if (targetCard == null)
                    {
                        LogAction($"{casterSide} casts Cloak Trick, but no target was selected.");
                        break;
                    }

                    if (clickedSide == casterSide)
                    {
                        LogAction($"{casterSide} casts Cloak Trick on their own row; spell fizzles.");
                        break;
                    }

                    if (IsImmuneToSpells(targetCard))
                    {
                        LogAction($"{casterSide} casts Cloak Trick, but the target is immune to spells.");
                        break;
                    }

                    int huge = ComputeSpellAmount(casterSide, 999, isDamage: true);
                    targetCard.TakeDamageServerRpc(huge);

                    LogAction($"{casterSide} casts Cloak Trick, instantly removing the enemy monster in lane {laneIndex} from play.");
                    break;
                }

            default:
                {
                    Debug.LogWarning($"[GM] ResolveSpellCard: No implementation for spell {uid}");
                    LogAction($"{casterSide} casts {card.name}, but its effect is not yet implemented.");
                    break;
                }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers / accessors
    // -------------------------------------------------------------------------

    public Side GetLocalSide()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            return Side.Player1;
        }

        // Host is always Player1, remote client is Player2
        return nm.IsHost ? Side.Player1 : Side.Player2;
    }

    private List<DeckCardDto> GetDeck(Side side)
    {
        return (side == Side.Player1) ? deckP1 : deckP2;
    }

    private List<DeckCardDto> GetHand(Side side)
    {
        return (side == Side.Player1) ? handP1 : handP2;
    }

    public NetworkVariable<int> GetHp(Side side)
    {
        return (side == Side.Player1) ? p1Hp : p2Hp;
    }

    public NetworkVariable<int> GetZielCurrent(Side side)
    {
        return (side == Side.Player1) ? p1ZielCurrent : p2ZielCurrent;
    }

    public NetworkVariable<int> GetZielMax(Side side)
    {
        return (side == Side.Player1) ? p1ZielMax : p2ZielMax;
    }

    private NetworkCard[] GetBoardArray(Side side)
    {
        return (side == Side.Player1) ? boardP1 : boardP2;
    }

    private Side GetOpponent(Side side)
    {
        return (side == Side.Player1) ? Side.Player2 : Side.Player1;
    }

    
    /// Called by NetworkCard on server when it despawns to keep board arrays in sync.
    
    public void Server_OnCardDespawn(NetworkCard card, Side ownerSide, int laneIndex)
    {
        if (!IsServer)
            return;

        NetworkCard[] arr = GetBoardArray(ownerSide);
        if (arr == null || laneIndex < 0 || laneIndex >= arr.Length)
            return;

        if (arr[laneIndex] == card)
        {
            arr[laneIndex] = null;
        }
    }

    /// <summary>
    /// Returns the transform for a given lane for spawning visuals.
    /// </summary>
    public Transform GetLaneTransformForVisual(Side cardOwner, int laneIndex)
    {
        if (player1Lanes == null || player2Lanes == null)
            return null;

        if (laneIndex < 0 ||
            laneIndex >= player1Lanes.Length ||
            laneIndex >= player2Lanes.Length)
            return null;

        // Scene set up as: P1 bottom, P2 top
        return (cardOwner == Side.Player1)
            ? player1Lanes[laneIndex]
            : player2Lanes[laneIndex];
    }

    // -------------------------------------------------------------------------
    // Action log
    // -------------------------------------------------------------------------

    private void LogAction(string message)
    {
        if (IsServer)
        {
            LogActionClientRpc(message);
        }
        else
        {
            OnActionLog?.Invoke(message);
        }
    }

    [ClientRpc]
    private void LogActionClientRpc(string message)
    {
        OnActionLog?.Invoke(message);
    }
}
