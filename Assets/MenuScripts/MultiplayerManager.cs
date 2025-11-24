using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    public string LastJoinCode { get; private set; }
    public bool IsReady { get; private set; }

    private UnityTransport _transport;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _transport = GetComponent<UnityTransport>();

        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized &&
            UnityServices.State != ServicesInitializationState.Initializing)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        IsReady = true;
        Debug.Log("UGS Initialized. PlayerID: " + AuthenticationService.Instance.PlayerId);
    }

    // ----------------------------
    // HOST
    // ----------------------------
    public async Task<string> StartHostAsync()
    {
        if (!IsReady)
            await InitializeServices();

        var allocation = await RelayService.Instance.CreateAllocationAsync(4);

        _transport.SetRelayServerData(
            AllocationUtils.ToRelayServerData(allocation, "dtls")
        );

        LastJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log("Relay join code: " + LastJoinCode);

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log("Host started: " + ok);

        return ok ? LastJoinCode : null;
    }

    // ----------------------------
    // CLIENT
    // ----------------------------
    public async Task<bool> StartClientAsync(string joinCode)
    {
        if (!IsReady)
            await InitializeServices();

        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        _transport.SetRelayServerData(
            AllocationUtils.ToRelayServerData(joinAlloc, "dtls")
        );

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log("Client started: " + ok);

        return ok;
    }
}
