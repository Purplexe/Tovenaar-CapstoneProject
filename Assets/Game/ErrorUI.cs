//error displayer in game class. 
using System.Collections;
using TMPro;
using UnityEngine;

public class ErrorUI : MonoBehaviour
{
    public static ErrorUI Instance { get; private set; }

    public TMP_Text messageText;
    public float displayTime = 2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    //call this if u want to show an error
    public void ShowError(string message)
    {
        if (messageText == null)
        {
            Debug.LogWarning("[ErrorUI] messageText not assigned.");
            return;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine(message));
    }
    //show for alloted amount of time
    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(displayTime);

        messageText.gameObject.SetActive(false);
        currentRoutine = null;
    }
}
