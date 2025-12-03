using UnityEngine;

public class HandUI : MonoBehaviour
{
    public static HandUI Instance { get; private set; }

    [Header("Hand spawning")]
    public Transform handContainer;      // The object with HorizontalLayoutGroup
    public GameObject handCardPrefab;    // Prefab with HandCardUI on it

    private int selectedIndex = -1;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Called by TovenaarGameManager on the owning client
    /// to update the displayed hand.
    /// </summary>
    public void SetHand(HandCardData[] cards)
    {
        if (handContainer == null || handCardPrefab == null)
        {
            Debug.LogError("[HandUI] handContainer or handCardPrefab not assigned.");
            return;
        }

        // Clear existing cards
        for (int i = handContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(handContainer.GetChild(i).gameObject);
        }

        // Spawn one UI card per HandCardData
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

        RefreshSelectionVisuals();
    }

    /// <summary>
    /// Called by individual HandCardUI when clicked.
    /// </summary>
    public void OnCardClicked(int index)
    {
        selectedIndex = index;
        Debug.Log("[HandUI] Selected card index = " + selectedIndex);
        RefreshSelectionVisuals();
    }

    /// <summary>
    /// Used by lane buttons to know which hand card is selected.
    /// </summary>
    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < handContainer.childCount; i++)
        {
            HandCardUI cardUI = handContainer.GetChild(i).GetComponent<HandCardUI>();
            if (cardUI != null)
            {
                bool isSelected = (i == selectedIndex);
                cardUI.SetSelected(isSelected);
            }
        }
    }
}
