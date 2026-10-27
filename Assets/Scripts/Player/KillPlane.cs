using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Respawns the player (or triggers Health.TakeDamage for instant kill) when
/// they fall below a Y-threshold.  Attach to the player prefab root.
/// Works both offline and online (owner-side check, server applies damage).
/// </summary>
public class KillPlane : NetworkBehaviour
{
    [SerializeField] private float killY = -20f;

    private Health health;
    private CharacterController controller;

    private void Awake()
    {
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Only the owner (or the local player in offline mode) does this check.
        if (IsSpawned && !IsOwner) return;

        if (transform.position.y < killY)
        {
            if (IsSpawned)
            {
                // Ask the server to instantly kill us.
                KillServerRpc();
            }
            else
            {
                // Offline fallback — instantly "die" and teleport to spawn.
                TeleportToSpawn();
            }
        }
    }

    [ServerRpc]
    private void KillServerRpc()
    {
        if (health != null && !health.IsDead)
        {
            // Deal enough damage to kill regardless of remaining HP.
            health.TakeDamage(9999f, OwnerClientId, false);
        }
        else
        {
            // Health already dead / missing — just teleport the client back.
            var sp = SpawnPoints.Random();
            if (sp != null)
                TeleportClientRpc(sp.position, sp.rotation);
        }
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 pos, Quaternion rot)
    {
        TeleportTo(pos, rot);
    }

    private void TeleportToSpawn()
    {
        var sp = SpawnPoints.Random();
        if (sp != null)
            TeleportTo(sp.position, sp.rotation);
    }

    private void TeleportTo(Vector3 pos, Quaternion rot)
    {
        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        if (controller != null) controller.enabled = true;
    }
}
