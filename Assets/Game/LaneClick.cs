//lane clicking to place car
using UnityEngine;

public class LaneClick : MonoBehaviour
{
    public int laneIndex;
    public bool isPlayer1Lane;

    // when click, get handUI selected and paly it here
    public void OnLaneClicked()
    {
        int selectedIndex = -1;

        if (HandUI.Instance != null)
        {
            selectedIndex = HandUI.Instance.GetSelectedIndex();
        }

        if (TovenaarGameManager.Instance != null)
        {
            TovenaarGameManager.Instance.RequestPlayCard(laneIndex,isPlayer1Lane,selectedIndex);
        }
        else
        {
            Debug.Log("no game manager silly");
        }
    }
}
