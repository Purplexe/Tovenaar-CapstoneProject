//loads selected deck when match starts. This is attached to deckloader object in the scene so when match scene is loaded into, this is called. 

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

        StartCoroutine(WaitForNetworkAndLoad());
    }

    //had to wait sometimes since my computer was kinda slow. We were loading other assets first which caused issues. 
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

        yield return LoadDeckForThisClient();
    }

    private IEnumerator LoadDeckForThisClient()
    {
        //empty out string
        string token = string.Empty;

        if (TovenaarLoginManager.Instance != null)
        {
            // IMPORTANT: this must be the same value stored in users.unity_token
            //now that I think about it, if the game is played long enough you'd have to logout and logback in. hmmm
            token = TovenaarLoginManager.Instance.SessionToken; //so useful
        }

        if (string.IsNullOrWhiteSpace(token)) 
        {
            Debug.LogError("Token missing");
            yield break;
        }

        //escape url 
        string escapedToken = UnityWebRequest.EscapeURL(token);

        // url build
        string url = apiBaseUrl + "/deck_cards.php?deck_id=" + selectedDeckId + "&token=" + escapedToken;

        Debug.Log(url);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.downloadHandler.text);
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log(json);

            DeckCardsResponse response = null;

            try
            {
                response = JsonUtility.FromJson<DeckCardsResponse>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                yield break;
            }

            if (response == null || response.cards == null || response.cards.Length == 0)
            {
                Debug.LogError("empty deck silly");
                yield break;
            }

            if (TovenaarGameManager.Instance != null)
            {
                TovenaarGameManager.Instance.SubmitDeckServerRpc(response.cards);
                Debug.Log(response.cards.Length +  + selectedDeckId);
            }
            else
            {
                Debug.Log("Game manager missing");
            }
        }
    }
}
