using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HandUI : MonoBehaviour
{
    public static HandUI Instance { get; private set; }

    public Transform handRoot;          // layout parent
    public GameObject handCardPrefab;   // button prefab

    private List<HandCardData> currentHand = new List<HandCardData>();
    public int SelectedIndex { get; private set; } = -1;

    private void Awake()
    {
        Instance = this;
    }
    

    public void SetHand(HandCardData[] cards)
    {
        currentHand.Clear();
        currentHand.AddRange(cards);
        SelectedIndex = -1;

        foreach (Transform child in handRoot)
            Destroy(child.gameObject);

        for (int i = 0; i < currentHand.Count; i++)
        {
            int idx = i;
            var data = currentHand[i];
            GameObject go = Instantiate(handCardPrefab, handRoot);

            // quick wiring
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0) texts[0].text = data.name;
            if (texts.Length > 1) texts[1].text = data.cost.ToString();

            // art by card_uid
            var img = go.GetComponentInChildren<Image>();
            if (img != null)
            {
                var sprite = Resources.Load<Sprite>($"Cards/{data.card_uid}");
                if (sprite != null) img.sprite = sprite;
            }

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SelectedIndex = idx;
            });
        }
    }

    public HandCardData? GetSelectedCard()
    {
        if (SelectedIndex < 0 || SelectedIndex >= currentHand.Count) return null;
        return currentHand[SelectedIndex];
    }

    public void ClearSelection()
    {
        SelectedIndex = -1;
    }
}
