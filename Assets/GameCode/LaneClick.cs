using UnityEngine;

public class LaneClick : MonoBehaviour
{
    public int laneIndex;
    public bool isPlayer1Lane;

    // Hook this to the lane button's OnClick
    public void OnLaneClicked()
    {
        int selectedIndex = -1;

        if (HandUI.Instance != null)
        {
            selectedIndex = HandUI.Instance.GetSelectedIndex();
        }

        if (TovenaarGameManager.Instance != null)
        {
            TovenaarGameManager.Instance.RequestPlayCard(
                laneIndex,
                isPlayer1Lane,
                selectedIndex
            );
        }
        else
        {
            Debug.LogWarning("[LaneClick] TovenaarGameManager.Instance is null.");
        }
    }
}
