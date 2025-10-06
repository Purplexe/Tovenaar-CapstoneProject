using UnityEngine;

public class CardInventory : MonoBehaviour
{
    public GameObject Card;
    public Transform Inventory;

    public void AddCard()
    {
        Instantiate(Card, Inventory);
    }


}
