using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class DeckDBOject
{
    public int id;
    public string name;
    public int is_active;
    public string created_at;
    public string updated_at;
}

[Serializable]
public class DBJson
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
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private Transform deckListContainer;   // parent with VerticalLayoutGroup
    [SerializeField] private GameObject deckRowPrefab;   // Button + TMP_Text
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button playButton;
   

    [Header("Create Deck UI")]
    [SerializeField] private TMP_InputField newDeckNameInput;
    [SerializeField] private Button createDeckButton;

    [Header("Scenes")]
    [SerializeField] private string loginSceneName = "Login";  // your login scene name

    private DeckDBOject[] _decks;
    private DeckDBOject _selectedDeck;

    private void Start()
    {
        // Ensure we have an auth manager & token
        if (TovenaarLoginManager.Instance == null ||
            string.IsNullOrEmpty(TovenaarLoginManager.Instance.SessionToken))
        {
            Debug.LogWarning("No SessionToken – sending back to login.");
            SceneManager.LoadScene(loginSceneName);
            return;
        }

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
        string url = TovenaarLoginManager.Instance.apiURL + "?token=" + Uri.EscapeDataString(token);

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
            var rowUI = rowObj.GetComponent<DeckRowUI>();

            if (rowUI == null)
            {
                Debug.LogError("DeckRowPrefab is missing DeckRowUI component.");
                continue;
            }

            if (rowUI.nameLabel != null)
                rowUI.nameLabel.text = deck.name;

            // Capture local copy for lambdas
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
    }

    private void OnDeckEditClicked(DeckDBOject deck)
    {
        if (deck == null)
            return;

        // Build full URL: deck_edit.php?id=123
        string url = TovenaarLoginManager.Instance.deckEditBaseUrl
                     + "?id=" + deck.id;

        Debug.Log("Opening deck editor: " + url);
        Application.OpenURL(url);
    }


    private void OnDeckButtonClicked(int index)
    {
        if (_decks == null || index < 0 || index >= _decks.Length)
            return;

        _selectedDeck = _decks[index];
        statusLabel.text = $"Selected deck: {_selectedDeck.name}";
        SetDeckActionButtonsEnabled(true);
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
        // TODO later: TovenaarLoginManager.Instance.SelectedDeckId = _selectedDeck.id;
        // SceneManager.LoadScene("GameScene");
    }


    public void OnEditDeckClicked()
    {
        if (_selectedDeck == null)
        {
            Debug.LogWarning("Edit clicked without deck selection.");
            return;
        }

        Debug.Log("Edit deck ID " + _selectedDeck.id);
        // TODO later: load deck builder scene, use _selectedDeck.id
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
            TovenaarLoginManager.Instance.SessionToken = null;
            Destroy(TovenaarLoginManager.Instance.gameObject); //Needed so that the singleton can get references again
        }

        SceneManager.LoadScene(loginSceneName);
    }
}
