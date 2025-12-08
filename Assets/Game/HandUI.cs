//overall hand manager, handcardui is per cardd
using UnityEngine;

public class HandUI : MonoBehaviour
{
    public static HandUI Instance { get; private set; }

    public Transform handContainer;      //HorizontalLayoutGroup for even spacings 
    public GameObject handCardPrefab;    //base prefab to build off of

    private int selectedIndex = -1;

    private void Awake()
    {
        Instance = this;
    }

    public void SetHand(HandCardData[] cards)
    {
        if (handContainer == null || handCardPrefab == null)
        {
            return;
        }

        // Clear existing cards
        for (int i = handContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(handContainer.GetChild(i).gameObject);
        }

        // Spawn one UI card with da handcardui
        for (int i = 0; i < cards.Length; i++)
        {
            GameObject cardObj = Instantiate(handCardPrefab, handContainer);
            HandCardUI cardUI = cardObj.GetComponent<HandCardUI>();

            if (cardUI != null)
            {
                cardUI.SetCard(cards[i], i);
            }
        }

        // If our old selection is no longer valid, clear it
        if (selectedIndex >= cards.Length)
        {
            selectedIndex = -1;
        }
    }

    public void OnCardClicked(int index)
    {
        selectedIndex = index;
        Debug.Log(selectedIndex);
    }

    /// <summary>
    /// Used by lane buttons to know which hand card is selected.
    /// </summary>
    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    
}
