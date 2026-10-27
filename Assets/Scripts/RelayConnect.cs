using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayConnect : MonoBehaviour
{
    private string inputJoinCode = "";
    private string statusMessage = "";
    private bool isInitializing = false;

    /// <summary>Exposes the current join code so other UI (e.g. PauseMenu) can read it.</summary>
    public string CurrentJoinCode { get; private set; } = "";

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    private async Task InitializeServicesAsync()
    {
        if (isInitializing || (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn))
            return;

        isInitializing = true;
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            statusMessage = "Unity Services connected.";
            Debug.Log($"[Relay] Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception ex)
        {
            statusMessage = $"Auth Error: {ex.Message}";
            Debug.LogError($"[Relay] Failed to authenticate: {ex}");
        }
        finally
        {
            isInitializing = false;
        }
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 120), GUI.skin.box);
            GUILayout.Label($"Mode: {(NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client")}");
            if (!string.IsNullOrEmpty(statusMessage))
                GUILayout.Label(statusMessage);

            if (GUILayout.Button("Disconnect"))
            {
                NetworkManager.Singleton.Shutdown();
                statusMessage = "Disconnected.";
            }
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 320, 260), GUI.skin.box);
        GUILayout.Label("<b>Multiplayer Lobby / Relay</b>");

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Label(statusMessage);
        }

        GUILayout.Space(5);

        // 1. Host Button
        if (GUILayout.Button("Start Host"))
        {
            StartHostWithRelay();
        }

        GUILayout.Space(10);

        // 2. Client Join Code Input & Button
        GUILayout.Label("Enter Join Code:");
        inputJoinCode = GUILayout.TextField(inputJoinCode?.Trim().ToUpper() ?? "");

        if (GUILayout.Button("Start Client"))
        {
            StartClientWithRelay(inputJoinCode);
        }

        GUILayout.EndArea();
    }

    // Call this to Host via Relay
    public async void StartHostWithRelay(int maxConnections = 4)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Relay] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
            statusMessage = "Error: NetworkManager missing!";
            return;
        }

        statusMessage = "Creating Relay host...";
        try
        {
            await InitializeServicesAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[Relay] UnityTransport component not found on NetworkManager!");
                statusMessage = "Error: UnityTransport missing!";
                return;
            }

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            bool started = NetworkManager.Singleton.StartHost();
            if (started)
            {
                inputJoinCode = joinCode;
                CurrentJoinCode = joinCode;         // expose for PauseMenu
                statusMessage = $"Hosting! Code: {joinCode}";
                GUIUtility.systemCopyBuffer = joinCode;
                Debug.Log($"[Relay] Host started. Join Code (copied to clipboard): {joinCode}");
            }
            else
            {
                statusMessage = "Failed to start host.";
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Host failed: {ex.Message}";
            Debug.LogError($"[Relay] StartHostWithRelay Exception: {ex}");
        }
    }

    // Call this to Join using a code
    public async void StartClientWithRelay(string joinCode)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Relay] NetworkManager.Singleton is null. Ensure a NetworkManager exists in the scene.");
            statusMessage = "Error: NetworkManager missing!";
            return;
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            statusMessage = "Please enter a valid Join Code.";
            return;
        }

        statusMessage = $"Joining with code {joinCode.Trim().ToUpper()}...";
        try
        {
            await InitializeServicesAsync();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpper());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[Relay] UnityTransport component not found on NetworkManager!");
                statusMessage = "Error: UnityTransport missing!";
                return;
            }

            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            bool started = NetworkManager.Singleton.StartClient();
            if (started)
            {
                statusMessage = $"Client joined via Relay: {joinCode}";
                Debug.Log($"[Relay] Client connected with code {joinCode}");
            }
            else
            {
                statusMessage = "Failed to start client.";
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Join failed: {ex.Message}";
            Debug.LogError($"[Relay] StartClientWithRelay Exception: {ex}");
        }
    }
}
