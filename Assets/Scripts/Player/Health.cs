using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float respawnTime = 3f;

    public NetworkVariable<float> CurrentHp = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float MaxHp => maxHp;
    public bool IsDead => CurrentHp.Value <= 0f;

    public delegate void HealthChangedDelegate(float current, float max);
    public event HealthChangedDelegate OnHealthChanged;

    public delegate void DeathDelegate(ulong killerId, bool headshot);
    public event DeathDelegate OnDied;

    private CharacterController controller;
    private PlayerFSM playerFSM;
    private FastHitFlash hitFlash;
    private PlayerScore playerScore;

    private void Awake()
    {
        controller  = GetComponent<CharacterController>();
        playerFSM   = GetComponent<PlayerFSM>();
        hitFlash    = GetComponent<FastHitFlash>();
        playerScore = GetComponent<PlayerScore>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentHp.OnValueChanged += HandleHpChanged;

        if (IsServer)
        {
            CurrentHp.Value = maxHp;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentHp.OnValueChanged -= HandleHpChanged;
    }

    private void HandleHpChanged(float oldVal, float newVal)
    {
        OnHealthChanged?.Invoke(newVal, maxHp);

        if (newVal < oldVal)
        {
            if (hitFlash != null) hitFlash.Flash();
        }
    }

    public void TakeDamage(float damage, ulong attackerId, bool headshot)
    {
        if (IsDead) return;

        if (!IsServer && !IsSpawned)
        {
            // Offline/singleplayer fallback
            ApplyDamageOffline(damage, attackerId, headshot);
            return;
        }

        if (!IsServer) return;

        float afterDamage = Mathf.Max(0f, CurrentHp.Value - damage);
        CurrentHp.Value = afterDamage;

        if (afterDamage <= 0f)
        {
            if (KillFeed.Instance != null)
            {
                KillFeed.Instance.ReportKillServer(attackerId, OwnerClientId, headshot);
            }

            // Update scores on the server
            UpdateScoresServer(attackerId);

            OnDied?.Invoke(attackerId, headshot);
            DieClientRpc();
            StartCoroutine(RespawnRoutine());
        }
    }

    private void ApplyDamageOffline(float damage, ulong attackerId, bool headshot)
    {
        float afterDamage = Mathf.Max(0f, CurrentHp.Value - damage);
        CurrentHp.Value = afterDamage;
        HandleHpChanged(CurrentHp.Value + damage, afterDamage);

        if (afterDamage <= 0f)
        {
            if (KillFeed.Instance != null)
                KillFeed.Instance.ReportKillOffline(attackerId, 0, headshot);

            StartCoroutine(RespawnRoutineOffline());
        }
    }

    private void UpdateScoresServer(ulong attackerId)
    {
        // Give a kill to the attacker if they are different from the victim.
        if (attackerId != OwnerClientId)
        {
            // Find the attacker's NetworkObject and their PlayerScore.
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out var attackerObj))
            {
                var score = attackerObj.GetComponent<PlayerScore>();
                if (score != null) score.AddKill();
            }
            else
            {
                // Attacker may share a different NetworkObjectId; iterate clients.
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.ClientId == attackerId && client.PlayerObject != null)
                    {
                        var score = client.PlayerObject.GetComponent<PlayerScore>();
                        if (score != null) score.AddKill();
                        break;
                    }
                }
            }
        }

        // Add a death to the victim.
        if (playerScore != null) playerScore.AddDeath();
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        if (!IsServer) yield break;

        CurrentHp.Value = maxHp;

        // Use SpawnPoints singleton; fall back to a safe position above origin.
        var sp = SpawnPoints.Random();
        Vector3 spawnPos = sp != null ? sp.position : Vector3.up * 2f;
        Quaternion spawnRot = sp != null ? sp.rotation : Quaternion.identity;

        RespawnClientRpc(spawnPos, spawnRot);
    }

    private IEnumerator RespawnRoutineOffline()
    {
        yield return new WaitForSeconds(respawnTime);
        CurrentHp.Value = maxHp;
        HandleHpChanged(0f, maxHp);
        transform.position = Vector3.up * 2f;
        if (controller != null) controller.enabled = true;
        if (playerFSM != null) playerFSM.enabled = true;
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        if (controller != null) controller.enabled = false;
        if (playerFSM != null) playerFSM.enabled = false;

        // Hide visuals
        SetVisualsActive(false);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 pos, Quaternion rot)
    {
        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        if (controller != null && IsOwner) controller.enabled = true;
        if (playerFSM != null && IsOwner) playerFSM.enabled = true;

        SetVisualsActive(true);
    }

    private void SetVisualsActive(bool active)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = active;
        }
    }
}
