using UnityEngine;
using UnityEngine.SceneManagement;
public class HomePageUi : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log("Starting Game");
        SceneManager.LoadScene("MainGame");
    }
    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }


}
