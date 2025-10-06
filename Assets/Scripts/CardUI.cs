using UnityEngine;
using TMPro;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI costText;   

    private Card card;

    void Awake()
    {
        card = GetComponent<Card>();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (!card) card = GetComponent<Card>();

        if (nameText) nameText.text = card.CardName;
        if (atkText) atkText.text = card.Attack.ToString();
        if (hpText) hpText.text = card.Health.ToString();
        if (costText) costText.text = card.Cost.ToString();
    }
}
