using UnityEngine;
using UnityEngine.EventSystems;

public class Lane : MonoBehaviour, IDropHandler
{
    public PlayerSide Owner = PlayerSide.Player1; // which side this lane belongs to
    public int PairIndex = 0;                     // matches opposing lane index for combat pairing
    public Card Occupant { get; private set; }

    public bool IsOccupied => Occupant != null;

    public void OnDrop(PointerEventData eventData)
    {
        if (IsOccupied) return;
        if (!TurnManager.instance.IsPlayersTurn(Owner)) return;

        var go = eventData.pointerDrag;
        if (!go) return;

        var card = go.GetComponent<Card>();
        if (!card) return;
        if (card.Owner != Owner) return;

        // 🪙 Check resource cost
        if (!PlayerResourceManager.I.CanAfford(Owner, card.Cost))
        {
            Debug.Log("Not enough Ziel!");
            return;
        }

        PlayerResourceManager.I.Spend(Owner, card.Cost);

        // Snap & lock into lane
        go.transform.SetParent(transform);
        var rt = go.transform as RectTransform;
        rt.anchoredPosition = Vector2.zero;
        Occupant = card;
    }
    public void ClearIf(Card c)
    {
        if (Occupant == c) Occupant = null;
    }
}
