//handles how cards look in your hand

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandCardUI : MonoBehaviour
{
   
    public TMP_Text nameText;  
    public TMP_Text costText;
    public TMP_Text attackText;
    public TMP_Text healthText;

    public Image artImage;

    private int handIndex = -1;
    private HandCardData currentData;

//called by hand ui
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
                //Assets/Resources/Cards/CARD_UID
                //hardnamed the sprites
                sprite = Resources.Load<Sprite>("Cards/" + data.card_uid);
            }
            artImage.sprite = sprite;
        }

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

 
        gameObject.SetActive(false);
    }


//selects card to be played
    public void OnClick()
    {
        if (HandUI.Instance != null && handIndex >= 0)
        {
            HandUI.Instance.OnCardClicked(handIndex);
        }
    }
}
