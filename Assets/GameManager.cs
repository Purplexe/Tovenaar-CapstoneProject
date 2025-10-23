using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Side { Player1, Player2 }
public enum TurnPhase { Start, Main, Combat, End, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [Header("Assign lane GameObjects")]
    public List<GameObject> playerLaneObjects = new List<GameObject>();
    public List<GameObject> enemyLaneObjects = new List<GameObject>();

    [Header("Match Settings")]
    public int lanesCount = 4;
    public int playerStartingHealth = 20;
    public int enemyStartingHealth = 20;

    [Header("Resource Settings")]
    public int startingZiel = 1;
    public int zielPerTurn = 1;
    public int maxZiel = 10;

    [Header("Turn Order")]
    public Side startingSide = Side.Player1;

    [HideInInspector] public TurnPhase phase { get; private set; }
    [HideInInspector] public Side activeSide { get; private set; }

    private List<Lane> playerLanes = new List<Lane>();
    private List<Lane> enemyLanes = new List<Lane>();

    private int player1Health;
    private int player2Health;

    private int player1Ziel;
    private int player2Ziel;

    private int player1MaxZiel;
    private int player2MaxZiel;

    [Header("UI Settings")]
    public Image Bottom;
    public Image Top;
    public TMP_Text turn;
    public Color player1color;
    public Color player2color;
    public TMP_Text friendlyHealthUI;
    public TMP_Text friendlyZielUI;
    public TMP_Text enemyHealthUI;
    public RectTransform Player1LaneContainer;
    public RectTransform Player2LaneContainer;


    [Header("Inventory Settings")]
    public GameObject player1InventoryUI;
    public GameObject player2InventoryUI;
    public List<Card> player1Deck;
    public List<Card> player2Deck;

    void Awake()
    {
        Instance = this;
        BuildLaneLists();
        InitMatch();
        UpdateUI();
    }

    void BuildLaneLists()
    {
        playerLanes.Clear();
        enemyLanes.Clear();

        foreach (var go in playerLaneObjects)
        {
            if (!go) continue;
            var lane = go.GetComponent<Lane>();
            if (lane) playerLanes.Add(lane);
        }

        foreach (var go in enemyLaneObjects)
        {
            if (!go) continue;
            var lane = go.GetComponent<Lane>();
            if (lane) enemyLanes.Add(lane);
        }

        lanesCount = Mathf.Min(playerLanes.Count, enemyLanes.Count);
    }

    public bool IsPlayersTurn(Side side) => activeSide == side;

    public void UpdateUI()
    {
        if (activeSide == Side.Player1)
        {
            Bottom.color = player1color;
            Top.color = player2color;
            turn.text = "Player 1 Turn.";
            friendlyHealthUI.text = player1Health.ToString();
            friendlyZielUI.text = player1Ziel.ToString();
            enemyHealthUI.text = player2Health.ToString();
            player1InventoryUI.SetActive(true);
            player2InventoryUI.SetActive(false);
            Player1LaneContainer.anchoredPosition = new Vector2(0,21);
            Player2LaneContainer.anchoredPosition = new Vector2(0, 175);


        }
        else
        {
            Bottom.color = player2color;
            Top.color = player1color;
            turn.text = "Player 2 Turn.";
            friendlyHealthUI.text = player2Health.ToString();
            friendlyZielUI.text = player2Ziel.ToString();
            enemyHealthUI.text = player1Health.ToString();
            player1InventoryUI.SetActive(false);
            player2InventoryUI.SetActive(true);
            Player1LaneContainer.anchoredPosition = new Vector3(0, 175, 0);
            Player2LaneContainer.anchoredPosition = new Vector3(0, 21, 0);
        }
    }

    void InitMatch()
    {
        player1Health = playerStartingHealth;
        player2Health = enemyStartingHealth;

        player1MaxZiel = startingZiel;
        player2MaxZiel = startingZiel;

        player1Ziel = player1MaxZiel;
        player2Ziel = player2MaxZiel;

        activeSide = startingSide;
        phase = TurnPhase.Start;
        StartTurn();
    }

    void StartTurn()
    {
        phase = TurnPhase.Start;

        if (activeSide == Side.Player1)
        {
            player1MaxZiel = Mathf.Min(maxZiel, player1MaxZiel + zielPerTurn);
            player1Ziel = player1MaxZiel;
        }
        else
        {
            player2MaxZiel = Mathf.Min(maxZiel, player2MaxZiel + zielPerTurn);
            player2Ziel = player2MaxZiel;
        }

        phase = TurnPhase.Main;
        UpdateUI();
    }

    public bool TryPlayUnitToLane(Side side, GameObject unitPrefab, int laneIndex, int zielCost)
    {
        if (phase != TurnPhase.Main) return false;
        if (laneIndex < 0 || laneIndex >= lanesCount) return false;

        if (!HasZiel(side, zielCost)) return false;

        var lane = GetLane(side, laneIndex);
        if (!lane || lane.HasUnit()) return false;

        var instance = Instantiate(unitPrefab, lane.transform);
        var unit = instance.GetComponent<Card>();
        if (!unit) { Destroy(instance); return false; }

        unit.Owner = side;
        lane.SetCard(unit);

        SpendZiel(side, zielCost);
        return true;
    }

    public void EndMainPhaseAndCombat()
    {
        if (phase != TurnPhase.Main) return;
        phase = TurnPhase.Combat;
        ResolveCombat();
        phase = TurnPhase.End;
        EndTurn();
    }

    void ResolveCombat()
    {
        for (int i = 0; i < lanesCount; i++)
        {
            var pLane = playerLanes[i];
            var eLane = enemyLanes[i];

            var pUnit = pLane ? pLane.GetCard() : null;
            var eUnit = eLane ? eLane.GetCard() : null;

            if (pUnit && eUnit)
            {
                int pAtk = Mathf.Max(0, pUnit.Attack);
                int eAtk = Mathf.Max(0, eUnit.Attack);

                pUnit.TakeDamage(eAtk);
                eUnit.TakeDamage(pAtk);

                if (pUnit.IsDead()) { pLane.ClearCard(); Destroy(pUnit.gameObject); }
                if (eUnit.IsDead()) { eLane.ClearCard(); Destroy(eUnit.gameObject); }
            }
            else if (pUnit && !eUnit)
            {
                int dmg = Mathf.Max(0, pUnit.Attack);
                player2Health -= dmg;
            }
            else if (!pUnit && eUnit)
            {
                int dmg = Mathf.Max(0, eUnit.Attack);
                player1Health -= dmg;
            }
        }

        if (player1Health <= 0 || player2Health <= 0)
        {
            phase = TurnPhase.GameOver;
        }
    }

   public void EndTurn()
    {
        if (phase == TurnPhase.GameOver) return;
        if (activeSide == Side.Player2)
        {
            player2Health -= 3;
        }
        activeSide = (activeSide == Side.Player1) ? Side.Player2 : Side.Player1;
        UpdateUI();
        StartTurn();

        
    }

    Lane GetLane(Side side, int index)
    {
        return side == Side.Player1 ? playerLanes[index] : enemyLanes[index];
    }

    bool HasZiel(Side side, int cost)
    {
        return side == Side.Player1 ? player1Ziel >= cost : player2Ziel >= cost;
    }

    void SpendZiel(Side side, int cost)
    {
        if (side == Side.Player1) player1Ziel -= cost; else player2Ziel -= cost;
    }

    public int GetZiel(Side side) => side == Side.Player1 ? player1Ziel : player2Ziel;
    public int GetMaxZiel(Side side) => side == Side.Player1 ? player1MaxZiel : player2MaxZiel;
    public int GetHealth(Side side) => side == Side.Player1 ? player1Health : player2Health;

    public void RebuildLanesAndSync()
    {
        BuildLaneLists();
    }

    


}
