using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UGSInitializer : MonoBehaviour
{
    private async void Awake()
    {
        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in to UGS as: " + AuthenticationService.Instance.PlayerId);
        }
    }
}
