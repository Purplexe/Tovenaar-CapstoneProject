using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager I { get; private set; }

    public List<Lane> Player1Lanes; // order indexes must align across both lists
    public List<Lane> Player2Lanes;

    void Awake() { I = this; }

    public void ResolveCombat()
    {
        int pairs = Mathf.Min(Player1Lanes.Count, Player2Lanes.Count);
        for (int i = 0; i < pairs; i++)
        {
            var p1 = Player1Lanes[i].Occupant;
            var p2 = Player2Lanes[i].Occupant;
            if (p1 == null && p2 == null) continue;

            // Simultaneous damage if both exist
            if (p1 && p2)
            {
                p1.TakeDamage(p2.Attack);
                p2.TakeDamage(p1.Attack);
                TryRemoveDead(Player1Lanes[i], p1);
                TryRemoveDead(Player2Lanes[i], p2);
                UpdateUIIfPresent(p1);
                UpdateUIIfPresent(p2);
            }
            else
            {
                // OPTIONAL: lane damage to player base, chip points, etc.
                // e.g., if (p1) Player2Health -= p1.Attack; if (p2) Player1Health -= p2.Attack;
            }
        }
    }

    void TryRemoveDead(Lane lane, Card c)
    {
        if (c != null && c.IsDead)
        {
            lane.ClearIf(c);
            Destroy(c.gameObject);
        }
    }

    void UpdateUIIfPresent(Card c)
    {
        if (!c) return;
        var ui = c.GetComponent<CardUI>();
        if (ui) ui.UpdateUI();
    }
}
