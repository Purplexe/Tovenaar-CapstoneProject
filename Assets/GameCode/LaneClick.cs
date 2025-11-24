using UnityEngine;
using UnityEngine.UI;

public class LaneClick : MonoBehaviour
{
    public int laneIndex;
    public bool isPlayer1Lane;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (CardGameManager.Instance == null) return;
        if (HandUI.Instance == null) return;

        int selectedIndex = HandUI.Instance.SelectedIndex;

        // Correct: ONLY 3 arguments
        CardGameManager.Instance.RequestPlayCard(
            laneIndex,
            isPlayer1Lane,
            selectedIndex
        );
    }
}
