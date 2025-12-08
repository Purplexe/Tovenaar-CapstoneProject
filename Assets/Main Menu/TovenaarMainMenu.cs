using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[Serializable]
public class DeckDBOject
    //Deck Data transfer object
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
    
    //player name from DB
    [SerializeField] private TMP_Text playerNameLabel;

    //player decks
    [SerializeField] private Transform deckListContainer;
    [SerializeField] private GameObject deckRowPrefab;

    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button playButton;


    //Since Unity has issues with logins in the editor, opted to use a code system instead of a deeplink, as it made testing simpler and reduced development time HEAVILY
    public string deckEditBaseUrl = "https://zachrhodesportfolio.org/Tovenaar/deck_edit.php";
    

    //creating new decks
    [SerializeField] private TMP_InputField newDeckNameInput;
    [SerializeField] private Button createDeckButton;

    //logout
    [SerializeField] private string loginSceneName = "HomeScreen";

    //Deck Objects
    private DeckDBOject[] _decks;
    private DeckDBOject _selectedDeck;

    private void Start()
    {
        // check first to see if we have manager and token
        if (TovenaarLoginManager.Instance == null ||
            string.IsNullOrEmpty(TovenaarLoginManager.Instance.SessionToken)) //told ya
        {
            //shoo
            Debug.LogWarning("No SessionToken");
            SceneManager.LoadScene(loginSceneName);
            return;
        }

        //Loading and disabling actions to prevent data corruption. I accidentally corrupted a deck without this
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


    //Deck loading coroutine
    private IEnumerator LoadDecks()
    {
        string token = TovenaarLoginManager.Instance.SessionToken; //yoink
        string url = TovenaarLoginManager.Instance.apiURL + "?token=" + Uri.EscapeDataString(token); //Getting Deck table from DB based on user token we have stored in Login manager


        //send request to api w/ users token to get links to player decks
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        //error handling in that regard
        if (req.result != UnityWebRequest.Result.Success)
        {
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError("Deck load error: " + req.error + "\n" + body);
            statusLabel.text = "Failed to load decks.";
            yield break;
        }

        //json from api, deck links
        string json = req.downloadHandler.text;
        Debug.Log(json);

        DBJson resp = null; //attempting json response from DB
        try
        {
            resp = JsonUtility.FromJson<DBJson>(json); //here we aree attempting to deserialize the json object we got into type DBJson, See top
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }

        if (resp == null || resp.status != "ok" || resp.decks == null)
        {
            statusLabel.text = "No decks found or invalid response.";
            yield break;
        }
        //success, now get list of player decks
        _decks = resp.decks;

        // Set player name in top right corner UI
        if (playerNameLabel != null)
            playerNameLabel.text = $"Player: {resp.username}";

        PopulateDeckList();
        //populate deck container w decks
    }

    private void PopulateDeckList()
    {
        //clearout deckListContainer first to prevent weird errors
        foreach (Transform child in deckListContainer)
            Destroy(child.gameObject);

        //self explanatory
        if (_decks == null || _decks.Length == 0)
        {
            statusLabel.text = "You don't have any decks yet.";
            SetDeckActionButtonsEnabled(false);
            return;
        }

        statusLabel.text = "Select a deck"; //inform user they have to select a deck before playing

        //here we just run through the amount of decks the player has and populate a DeckRowUI for each of them
        for (int i = 0; i < _decks.Length; i++)
        {
            DeckDBOject deck = _decks[i];

            GameObject rowObj = Instantiate(deckRowPrefab, deckListContainer);

            var rowUI = rowObj.GetComponent<DeckRowUI>(); //prefab I made to fit deck name, select button, and edit button. Select chooses deck for use while edit opens the web editor
            rowUI.nameLabel.text = deck.name;

            // Capture a copy. This is a weird issue w C# I had to use chatGPT since when using listeners, it could instead use the last object from the loop. IDK
            DeckDBOject capturedDeck = deck;

            if (rowUI.selectButton != null)
                rowUI.selectButton.onClick.AddListener(() => OnDeckSelected(capturedDeck));

            if (rowUI.editButton != null)
                rowUI.editButton.onClick.AddListener(() => OnDeckEditClicked(capturedDeck));
        }
    }
    //selecting deck for play
    private void OnDeckSelected(DeckDBOject deck)
    {
        _selectedDeck = deck;
        statusLabel.text = $"Selected deck: {deck.name}";
        SetDeckActionButtonsEnabled(true);
        //Once a deck is selected, you can play the game. 
    }

    //editing deck, just gonna use the web editor since building an in-game one has proven to be troublesome. 
    private void OnDeckEditClicked(DeckDBOject deck)
    {
        if (deck == null)
            return;

        // opening web editor based on deck id. website already has precautions in place to make sure only deck owners can edit their own decks.  
        string url = deckEditBaseUrl
                     + "?id=" + deck.id;

        Debug.Log("Opening deck editor: " + url);
        Application.OpenURL(url);
    }

    //play button
    public void OnPlayClicked()
    {
        if (_selectedDeck == null)
        {
            Debug.LogWarning("Play clicked without deck selection.");
            return;
        }

        Debug.Log("Play with deck ID " + _selectedDeck.id);

        //set deck to one we selected in main menu
        TovenaarLoginManager.Instance.SelectedDeckId = _selectedDeck.id;

        // If Netcode is already running as host/server, use Netcode scene loading
        //Netcode has been so annoying
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
        {
            //we need to load the scene with network manager so that the network objects can work correctly. IDK ITS SO WEIRD
            NetworkManager.Singleton.SceneManager.LoadScene("MatchScene",LoadSceneMode.Single);
        }
        else 
        {
            //for offline testing
            SceneManager.LoadScene("MatchScene");
        }
    }



    //piggy backing off web deck creator. Editing DB's in unity is a pain. 
    private void OnCreateDeckClicked()
    {
        if (newDeckNameInput == null) return;

        //formatting stuff
        string name = newDeckNameInput.text.Trim();
        //check name
        if (string.IsNullOrEmpty(name))
        {
            statusLabel.text = "Enter a deck name first.";
            return;
        }
        //yipppe
        StartCoroutine(CreateDeckCoroutine(name));
    }

    private IEnumerator CreateDeckCoroutine(string deckName)
    {
        //get player token
        string token = TovenaarLoginManager.Instance.SessionToken;
        //custom api link
        string url = TovenaarLoginManager.Instance.apiURL + "?token=" + Uri.EscapeDataString(token);

        //class object from earlier lol. had to do it this way or else unity yelled at me
        var payload = new CreateDeckRequest { name = deckName };
        string json = JsonUtility.ToJson(payload); //set inputted name to json
        Debug.Log(json);

        //posting json to server
        using UnityWebRequest req = new UnityWebRequest(url, "POST");
        //dont ask lol. so many errors
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json"); //make sure website type is application/json typing
        yield return req.SendWebRequest();

        //failure
        if (req.result != UnityWebRequest.Result.Success)
        {
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError("Create deck error: " + req.error + "\n" + body);
            statusLabel.text = "Failed to create deck.";
            yield break;
        }

        //wow wow wow
        newDeckNameInput.text = "";
        statusLabel.text = "Deck created!";
        StartCoroutine(LoadDecks());
    }

    //logout button
    public void OnLogoutClicked()
    {
        TovenaarLoginManager.Instance.SessionToken = null;//Discard token
        Destroy(TovenaarLoginManager.Instance.gameObject); //Needed so that the singleton can get references again in the uhhhhh login menu
        SceneManager.LoadScene(loginSceneName); //go back to login
    }
}
