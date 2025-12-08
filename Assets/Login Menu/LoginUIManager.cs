
using UnityEngine;

using TMPro;


public class LoginUIManager : MonoBehaviour
{
    //UI Panel
    [SerializeField] private GameObject mainMenu;
    //Error if Auth fails
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject LoginScreen;
    [SerializeField] private GameObject TutorialScreen;


    void Start()
    {
        mainMenu.SetActive(true);
        
    }
    
    public void OnLoginButtonClicked()
    {
        TovenaarLoginManager.Instance.OnOpenLoginPage();
        
    }

    public void OnTutorialButtonClicked()
    {
        LoginScreen.SetActive(false);
        TutorialScreen.SetActive(true);
    }

    public void OnBackButtonClicked()
    {
        LoginScreen.SetActive(true);
        TutorialScreen.SetActive(false);
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    
}

