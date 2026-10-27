using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Disables this camera when the local player spawns.
/// Use on menu/lobby cameras that should only show before gameplay.
/// </summary>
public class DisableOnPlayerSpawn : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        // Check if a player has spawned for the local client
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClient != null &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            // Player spawned! Disable this menu camera
            if (cam != null) cam.enabled = false;
        }
    }
}
