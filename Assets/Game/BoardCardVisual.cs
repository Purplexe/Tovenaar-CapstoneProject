//This script controls how cards on the board physically load. Since Network objects can't be children of non-network objects, I had to use this workaround.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardCardVisual : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public TMP_Text costText;
    public TMP_Text rarityText;
    public TMP_Text rulesText;
    public Image artImage;

    public void SetData(
        string name,
        int attack,
        int health,
        int cost,
        string rarity,
        string rules,
        string cardUid)
    {
        if (nameText != null) nameText.text = name;
        if (attackText != null) attackText.text = attack.ToString();
        if (healthText != null) healthText.text = health.ToString();
        if (costText != null) costText.text = cost.ToString();
        if (rarityText != null) rarityText.text = rarity;
        if (rulesText != null) rulesText.text = rules;

        //pull image from resources based on carduit. had to hardname the cards in this file as their UID's 
        if (artImage != null && !string.IsNullOrEmpty(cardUid))
        {
            Sprite s = Resources.Load<Sprite>($"Cards/{cardUid}");
            if (s != null)
                artImage.sprite = s;
        }
    }
}
