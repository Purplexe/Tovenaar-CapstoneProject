using UnityEngine;

public class Lane : MonoBehaviour
{
    [SerializeField] private Card card;

    public bool HasUnit() => card != null;
    public Card GetCard() => card;
    public void SetCard(Card c) { card = c; c.transform.SetParent(transform, false); }
    public void ClearCard() { card = null; }
}
