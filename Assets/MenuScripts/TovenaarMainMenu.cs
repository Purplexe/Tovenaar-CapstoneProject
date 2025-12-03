using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;


[Serializable]
public class DeckDBOject
    //Deck DTO
{
    public int id;
    public string name;
    public int is_active;
    public string created_at;
    public string updated_at;
}

[Serializable]
public class DBJson
    //JSON format 
{
    public string status;
    public string username;
    public DeckDBOject[] decks;
}

[Serializable]
public class CreateDeckRequest
{
    public string name;
}

public class TovenaarMainMenu : MonoBehaviour
{
    
    [SerializeField] private TMP_Text playerNameLabel; //Player Name for personability
    [SerializeField] private Transform deckListContainer; //gonna contain all player decks
    [SerializeField] private GameObject deckRowPrefab;   //Populates decklist
    [SerializeField] private TMP_Text statusLabel;  //Last action done, debuggin stuff
    [SerializeField] private Button playButton; //Moves to match
   

    
    [SerializeField] private TMP_InputField newDeckNameInput; //Create deck name
    [SerializeField] private Button createDeckButton;   //push

    
    [SerializeField] private string loginSceneName = "HomeScreen";  // logout scene

    private DeckDBOject[] _decks;
    private DeckDBOject _selectedDeck;

    private void Start()
    {
        // Ensure we have an auth manager & token
        if (TovenaarLoginManager.Instance == null ||
            string.IsNullOrEmpty(TovenaarLoginManager.Instance.SessionToken)) //told ya
        {
            //shoo
            Debug.LogWarning("No SessionToken");
            SceneManager.LoadScene(loginSceneName);
            return;
        }
        //Loading and disabling actions to prevent data corruption
        SetDeckActionButtonsEnabled(false);
        statusLabel.text = "Loading decks...";

        if (createDeckButton != null)
            createDeckButton.onClick.AddListener(OnCreateDeckClicked);

        StartCoroutine(LoadDecks());
    }

    private void SetDeckActionButtonsEnabled(bool enabled)
    {
        if (playButton != null) playButton.interactable = enabled;
        
    }

    private IEnumerator LoadDecks()
    {
        string token = TovenaarLoginManager.Instance.SessionToken;
        string url = TovenaarLoginManager.Instance.apiURL + "?token=" + Uri.EscapeDataString(token); //Getting Deck table from DB based on user token

        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError("Deck load error: " + req.error + "\n" + body);
            statusLabel.text = "Failed to load decks.";
            yield break;
        }

        string json = req.downloadHandler.text;
        Debug.Log("Decks JSON: " + json);

        DBJson resp = null;
        try
        {
            resp = JsonUtility.FromJson<DBJson>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("JSON parse error: " + ex);
        }

        if (resp == null || resp.status != "ok" || resp.decks == null)
        {
            statusLabel.text = "No decks found or invalid response.";
            yield break;
        }
        //success
        _decks = resp.decks;

        // Set player name
        if (playerNameLabel != null)
            playerNameLabel.text = $"Player: {resp.username}";

        PopulateDeckList();
    }

    private void PopulateDeckList()
    {
        foreach (Transform child in deckListContainer)
            Destroy(child.gameObject);

        if (_decks == null || _decks.Length == 0)
        {
            statusLabel.text = "You don't have any decks yet.";
            SetDeckActionButtonsEnabled(false);
            return;
        }

        statusLabel.text = "Select a deck";

        for (int i = 0; i < _decks.Length; i++)
        {
            DeckDBOject deck = _decks[i];

            GameObject rowObj = Instantiate(deckRowPrefab, deckListContainer);
            var rowUI = rowObj.GetComponent<DeckRowUI>(); //prefab I made to fit deck name, select button, and edit button. Select chooses deck for use while edit opens the web editor

            if (rowUI == null)
            {
                Debug.LogError("DeckRowPrefab is missing DeckRowUI component.");
                continue;
            }

            if (rowUI.nameLabel != null)
                rowUI.nameLabel.text = deck.name;

            // Capture a copy 
            DeckDBOject capturedDeck = deck;

            if (rowUI.selectButton != null)
                rowUI.selectButton.onClick.AddListener(() => OnDeckSelected(capturedDeck));

            if (rowUI.editButton != null)
                rowUI.editButton.onClick.AddListener(() => OnDeckEditClicked(capturedDeck));
        }
    }
    private void OnDeckSelected(DeckDBOject deck)
    {
        _selectedDeck = deck;
        statusLabel.text = $"Selected deck: {deck.name}";
        SetDeckActionButtonsEnabled(true);
        //Once a deck is selected, you can play the game. 
    }

    private void OnDeckEditClicked(DeckDBOject deck)
    {
        if (deck == null)
            return;

        // Build URL and open web editor. 
        string url = TovenaarLoginManager.Instance.deckEditBaseUrl
                     + "?id=" + deck.id;

        Debug.Log("Opening deck editor: " + url);
        Application.OpenURL(url);
    }





    // ===== Buttons =====

    public void OnPlayClicked()
    {
        if (_selectedDeck == null)
        {
            Debug.LogWarning("Play clicked without deck selection.");
            return;
        }

        Debug.Log("Play with deck ID " + _selectedDeck.id);

        // Remember which deck we chose for the match scene
        TovenaarLoginManager.Instance.SelectedDeckId = _selectedDeck.id;

        // If Netcode is already running as host/server, use Netcode scene loading
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
        {
            Debug.Log("[TovenaarMainMenu] Host/server already running, loading MatchScene via Netcode.");
            NetworkManager.Singleton.SceneManager.LoadScene(
                "MatchScene",
                LoadSceneMode.Single
            );
        }
        else
        {
            // No Netcode session yet (or this is just offline) → normal scene load
            Debug.Log("[TovenaarMainMenu] No Netcode session, loading MatchScene locally.");
            SceneManager.LoadScene("MatchScene");
        }
    }




    private void OnCreateDeckClicked()
    {
        if (newDeckNameInput == null) return;

        string name = newDeckNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            statusLabel.text = "Enter a deck name first.";
            return;
        }

        StartCoroutine(CreateDeckCoroutine(name));
    }

    private IEnumerator CreateDeckCoroutine(string deckName)
    {
        string token = TovenaarLoginManager.Instance.SessionToken;
        string url = TovenaarLoginManager.Instance.apiURL + "?token=" + Uri.EscapeDataString(token);

        var payload = new CreateDeckRequest { name = deckName };
        string json = JsonUtility.ToJson(payload);
        Debug.Log("CreateDeck JSON: " + json);  // should be {"name":"My Deck"}

        using UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError("Create deck error: " + req.error + "\n" + body);
            statusLabel.text = "Failed to create deck.";
            yield break;
        }

        newDeckNameInput.text = "";
        statusLabel.text = "Deck created!";
        StartCoroutine(LoadDecks());
    }


    public void OnLogoutClicked()
    {
        if (TovenaarLoginManager.Instance != null)
        {
            TovenaarLoginManager.Instance.SessionToken = null;//Discard token
            Destroy(TovenaarLoginManager.Instance.gameObject); //Needed so that the singleton can get references again
        }

        SceneManager.LoadScene(loginSceneName); //go back to login
    }
}
