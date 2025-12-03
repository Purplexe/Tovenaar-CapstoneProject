using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;          // NameText
    public TMP_Text costText;          // CostText
    public TMP_Text attackText;        // AttackText (NEW)
    public TMP_Text healthText;        // HealthText (NEW)

    public Image artImage;             // ArtImage
    public Image selectionHighlight;   // SelectionHighlight (optional)

    private int handIndex = -1;
    private HandCardData currentData;

    /// <summary>
    /// Called by HandUI after instantiating this card.
    /// </summary>
    public void SetCard(HandCardData data, int index)
    {
        handIndex = index;
        currentData = data;

        if (nameText != null)
            nameText.text = data.name;

        if (costText != null)
            costText.text = data.cost.ToString();

        if (attackText != null)
            attackText.text = data.attack.ToString();

        if (healthText != null)
            healthText.text = data.health.ToString();

        if (artImage != null)
        {
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(data.card_uid))
            {
                // Assumes Assets/Resources/Cards/CARD_UID.png
                sprite = Resources.Load<Sprite>("Cards/" + data.card_uid);
            }
            artImage.sprite = sprite;
        }

        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        handIndex = -1;
        currentData = default;

        if (nameText != null) nameText.text = string.Empty;
        if (costText != null) costText.text = string.Empty;
        if (attackText != null) attackText.text = string.Empty;
        if (healthText != null) healthText.text = string.Empty;
        if (artImage != null) artImage.sprite = null;

        SetSelected(false);
        gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.enabled = selected;
    }

    /// <summary>
    /// Hook this up to the Button's OnClick.
    /// This does NOT play the card directly; it just selects it.
    /// </summary>
    public void OnClick()
    {
        if (HandUI.Instance != null && handIndex >= 0)
        {
            HandUI.Instance.OnCardClicked(handIndex);
        }
    }
}
