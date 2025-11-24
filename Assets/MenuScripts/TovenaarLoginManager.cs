using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

public class TovenaarLoginManager : MonoBehaviour
{
    public static TovenaarLoginManager Instance;

    [SerializeField]private string unityLoginUrl ="https://zachrhodesportfolio.org/Tovenaar/unity_login.php";
    [SerializeField]public string deckEditBaseUrl = "https://zachrhodesportfolio.org/Tovenaar/deck_edit.php";
    //

    [SerializeField]public string apiURL ="https://zachrhodesportfolio.org/Tovenaar/api/decks.php";
    //Chatgpt recommended keeping an API section seperately for API requests. Since Unity has issues with logins in the editor, opted to use a code system instead of a deeplink.

    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] public string gameScene = "GameScene";

    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TMP_Text errorLabel;

    public string SessionToken { get; set; }
    public int SelectedDeckId { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by LoginUIManager
    public void OnOpenLoginPage()
    {
        //clear whatever is in error and launch the URL
        errorLabel.text = "";
        Application.OpenURL(unityLoginUrl);
    }

    // Called by "Confirm Code" button
    public void OnConfirmCode()
    {
        errorLabel.text = "";
        //check if input exists AT ALL LMAO
        if (codeInput == null)
        {
            ShowError("Code input is not assigned.");
            return;
        }

        string code = codeInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Please enter the code from the website.");
            return;
        }
        //code is valid and we can validate it
        StartCoroutine(VerifyCodeAndLogin(code));
    }

    private IEnumerator VerifyCodeAndLogin(string code)
    {
        string url = apiURL + "?token=" + Uri.EscapeDataString(code);
        UnityWebRequest req = UnityWebRequest.Get(url);

        yield return req.SendWebRequest();

        //check URL
        if (req.result != UnityWebRequest.Result.Success)
        {
            ShowError("Server error: " + req.error);
            yield break;
        }

        string json = req.downloadHandler.text;
        //passed ok from api/decks.php
        Debug.Log("Decks API response: " + json);

        if (!json.Contains("\"status\":\"ok\""))
        {
            ShowError("Invalid or expired code.");
            yield break;
        }

        // yay we did it
        SessionToken = code;
        //main screen, gonna populate with user info
        SceneManager.LoadScene(mainMenuScene);
    }

    private void ShowError(string errorMessage)
    {

        //print error if something goes wrong (it always does)
        Debug.LogError(errorMessage);
        if (errorLabel != null)
            errorLabel.text = errorMessage;
    }

}
