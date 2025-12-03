using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class DeckLoader : MonoBehaviour
{
    public string apiBaseUrl = "https://zachrhodesportfolio.org/Tovenaar/api";

    private int selectedDeckId;

    private void Start()
    {
        if (TovenaarLoginManager.Instance != null)
        {
            selectedDeckId = TovenaarLoginManager.Instance.SelectedDeckId;
        }

        if (selectedDeckId == 0)
        {
            Debug.LogWarning("[DeckLoader] SelectedDeckId was 0, using fallback deck 5.");
            selectedDeckId = 5;
        }

        StartCoroutine(WaitForNetworkAndLoad());
    }

    private IEnumerator WaitForNetworkAndLoad()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            yield return null;
        }

        while (TovenaarGameManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log("[DeckLoader] Network ready for client " +
                  NetworkManager.Singleton.LocalClientId +
                  ". Loading deck " + selectedDeckId + "...");

        yield return LoadDeckForThisClient();
    }

    private IEnumerator LoadDeckForThisClient()
    {
        string token = string.Empty;

        if (TovenaarLoginManager.Instance != null)
        {
            // IMPORTANT: this must be the same value stored in users.unity_token
            // If your login manager property is called SessionToken instead, use that.
            token = TovenaarLoginManager.Instance.SessionToken;
        }

        Debug.Log("[DeckLoader] raw token from login manager: '" + token + "'");
        Debug.Log("[DeckLoader] selectedDeckId: " + selectedDeckId);

        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError("[DeckLoader] Unity token missing or empty. Not calling API.");
            yield break;
        }

        string escapedToken = UnityWebRequest.EscapeURL(token);

        // PHP expects ?deck_id=...&token=...
        string url = apiBaseUrl +
                     "/deck_cards.php?deck_id=" + selectedDeckId +
                     "&token=" + escapedToken;

        Debug.Log("[DeckLoader] Requesting deck from: " + url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[DeckLoader] Deck load failed: " + req.error +
                               " (" + req.responseCode + ")");
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

            if (TovenaarGameManager.Instance != null)
            {
                TovenaarGameManager.Instance.SubmitDeckServerRpc(response.cards);
                Debug.Log("[DeckLoader] Sent " + response.cards.Length +
                          " cards to server for my deck (deck_id=" + selectedDeckId + ").");
            }
            else
            {
                Debug.LogWarning("[DeckLoader] GameManager instance missing; cannot submit deck.");
            }
        }
    }
}
