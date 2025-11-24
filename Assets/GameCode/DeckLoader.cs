using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Loads the selected deck from your web API and feeds it to CardGameManager.
/// Attach this to a GameObject in MatchScene (e.g. "GameSystems").
/// </summary>
public class DeckLoader : MonoBehaviour
{
    [Header("Config")]
    // NOTE: includes /api because deck_cards.php lives in public/Tovenaar/api/
    public string apiBaseUrl = "https://zachrhodesportfolio.org/Tovenaar/api";

    // Filled at runtime from TovenaarLoginManager.SelectedDeckId
    private int deckIdForPlayer1;

    private void Start()
    {
        // Get deck ID chosen in previous scene
        if (TovenaarLoginManager.Instance != null)
        {
            deckIdForPlayer1 = TovenaarLoginManager.Instance.SelectedDeckId;
        }

        if (deckIdForPlayer1 == 0)
        {
            Debug.LogWarning("[DeckLoader] SelectedDeckId was 0, using fallback deck 5.");
            deckIdForPlayer1 = 5; // safety fallback so you always get *something*
        }

        StartCoroutine(WaitForNetworkAndLoad());
    }

    /// <summary>
    /// Wait until NetworkManager is running as server and CardGameManager exists,
    /// then actually hit the API.
    /// </summary>
    private IEnumerator WaitForNetworkAndLoad()
    {
        // Wait until NetworkManager exists and we are the server (host)
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            yield return null;

        // Wait until CardGameManager has spawned and Instance is set
        while (CardGameManager.Instance == null)
            yield return null;

        Debug.Log($"[DeckLoader] Network ready. Loading deck {deckIdForPlayer1}...");
        yield return LoadDeckForPlayer1();
    }

    private IEnumerator LoadDeckForPlayer1()
    {
        // Unity token must match users.unity_token from your DB
        string token = "";

        if (TovenaarLoginManager.Instance != null)
        {
            token = TovenaarLoginManager.Instance.SessionToken; // <- make sure this property exists
        }

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[DeckLoader] Unity token is missing! Cannot authenticate deck request.");
            yield break;
        }

        string url = $"{apiBaseUrl}/deck_cards.php?deck_id={deckIdForPlayer1}&token={UnityWebRequest.EscapeURL(token)}";
        Debug.Log("[DeckLoader] Requesting deck from: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("[DeckLoader] Deck load failed: " + req.error + " (" + req.responseCode + ")");
                Debug.LogError("[DeckLoader] Response text: " + req.downloadHandler.text);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[DeckLoader] Deck JSON: " + json);

            DeckCardsResponse response = null;

            try
            {
                response = JsonUtility.FromJson<DeckCardsResponse>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[DeckLoader] JSON parse exception: " + e.Message);
                yield break;
            }

            if (response == null || response.cards == null || response.cards.Length == 0)
            {
                Debug.LogError("[DeckLoader] Deck JSON parse failed or empty deck.");
                yield break;
            }

            if (CardGameManager.Instance != null && CardGameManager.Instance.IsServer)
            {
                CardGameManager.Instance.SetDeckForSide(Side.Player1, response.cards);
                Debug.Log($"[DeckLoader] Loaded {response.cards.Length} cards into Player1 deck (deck_id={deckIdForPlayer1}).");
            }
            else
            {
                Debug.LogWarning("[DeckLoader] CardGameManager.Instance missing or not server.");
            }
        }
    }
}
