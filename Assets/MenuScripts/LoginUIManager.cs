using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LoginUIManager : MonoBehaviour
{
    //UI Panel
    [SerializeField] private GameObject mainMenu;
    //Error if Auth fails
    [SerializeField] private TMP_Text errorText;



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

    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    
}

