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
        if (CardGameManager.Instance == null) return; //Need Main game Logic
        if (HandUI.Instance == null) return; // Need Hand Logic to store/place cards

        int selectedIndex = HandUI.Instance.SelectedIndex;

        //lane index, if lane belongs to the player, and the index of the selected card from HandUI
        CardGameManager.Instance.RequestPlayCard(
            laneIndex,
            isPlayer1Lane,
            selectedIndex
        );
    }
}
