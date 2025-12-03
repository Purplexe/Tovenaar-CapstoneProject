using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("UI")]
    public GameObject rootPanel;
    public TMP_Text resultLabel;
    public float returnToMenuDelay = 5f;
    public string menuSceneName = "MenuScene"; // change if needed

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

    private void HandleGameOver(Side winner)
    {
        var gm = TovenaarGameManager.Instance;
        Side mySide = gm != null ? gm.GetLocalSide() : Side.Player1;
        bool iWon = (winner == mySide);

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (resultLabel != null)
            resultLabel.text = iWon ? "You Win!" : "You Lose";

        StartCoroutine(ReturnToMenuAfterDelay());
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(returnToMenuDelay);

        // Cleanly shut down Netcode
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
