using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Single point of truth for enabling/disabling per-player components based on
/// network ownership.  Attach to the player prefab root (same object as
/// NetworkObject).  Drag references in the Inspector or let Awake auto-find them.
///
/// Guide ref: Section 6.3 – "Make the local player controllable"
/// </summary>
public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Camera / Audio")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("Owner-only Behaviours")]
    [SerializeField] private MonoBehaviour[] ownerOnlyComponents;

    [Header("HUD Root")]
    [SerializeField] private GameObject hudRoot;

    [Header("View-model vs World-model")]
    [Tooltip("GameObjects visible only to THIS player (view model arms, weapon).")]
    [SerializeField] private GameObject[] viewModelObjects;
    [Tooltip("GameObjects visible to REMOTE players (full body, world-model weapon).")]
    [SerializeField] private GameObject[] worldModelObjects;

    private void Awake()
    {
        // Auto-find camera + audio listener if not wired in the Inspector.
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
        if (audioListener == null && playerCamera != null)
            audioListener = playerCamera.GetComponent<AudioListener>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ApplyOwnership(IsOwner);
    }

    private void ApplyOwnership(bool isOwner)
    {
        // Camera & listener
        if (playerCamera != null)   playerCamera.enabled   = isOwner;
        if (audioListener != null)  audioListener.enabled  = isOwner;

        // Owner-only MonoBehaviours (input, sway, anti-clip, etc.)
        if (ownerOnlyComponents != null)
        {
            foreach (var comp in ownerOnlyComponents)
                if (comp != null) comp.enabled = isOwner;
        }

        // HUD canvas
        if (hudRoot != null) hudRoot.SetActive(isOwner);

        // View / world model toggling
        if (viewModelObjects != null)
            foreach (var go in viewModelObjects)
                if (go != null) go.SetActive(isOwner);

        if (worldModelObjects != null)
            foreach (var go in worldModelObjects)
                if (go != null) go.SetActive(!isOwner);

        // Cursor lock only for the owner
        if (isOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        // Disable CharacterController-driven movement on remote players
        // (their position comes from NetworkTransform).
        var fsm = GetComponent<PlayerFSM>();
        if (fsm != null && !isOwner) fsm.enabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Remind designer to wire this up.
        if (playerCamera == null)
            Debug.LogWarning($"[NetworkPlayerSetup] {name}: playerCamera is not assigned.", this);
    }
#endif
}
