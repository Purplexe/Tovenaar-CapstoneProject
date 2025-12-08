//win or lose script. boot to menu after game ends.

using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    public GameObject rootPanel;
    public TMP_Text resultLabel;
    public float returnToMenuDelay = 5f;
    public string menuSceneName = "MenuScene";

    private void Awake()
    {
        Instance = this;
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void OnEnable()
    {
        TovenaarGameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        TovenaarGameManager.OnGameOver -= HandleGameOver;
    }


    //who wins, called from Game manager
    private void HandleGameOver(Side winner)
    {
        var gameManager = TovenaarGameManager.Instance;
        Side mySide = gameManager != null ? gameManager.GetLocalSide() : Side.Player1;
        //set winner 
        bool iWon = (winner == mySide);

        if (rootPanel != null)
            rootPanel.SetActive(true);

        //check side, then set result to win or lose based on side
        if (resultLabel != null)
            resultLabel.text = iWon ? "You Win!" : "You Lose";

        StartCoroutine(ReturnToMenuAfterDelay());
    }


    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(returnToMenuDelay);

        // Cleanly shut down Netcode, if you dont bad things happen
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
        //return to main menu
        SceneManager.LoadScene(menuSceneName);
    }
}
