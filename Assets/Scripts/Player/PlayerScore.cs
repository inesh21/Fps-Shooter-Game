using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tracks per-player kills and deaths as NetworkVariables.
/// Attach to the player prefab root (same object as Health / NetworkObject).
/// Read from the Scoreboard UI via PlayerScore.OnStatsChanged.
/// </summary>
public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> Kills = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Deaths = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Simple offline counters (used when IsSpawned == false)
    private int offlineKills;
    private int offlineDeaths;

    public delegate void StatsChangedDelegate(int kills, int deaths);
    public event StatsChangedDelegate OnStatsChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Kills.OnValueChanged += (_, __) => OnStatsChanged?.Invoke(Kills.Value, Deaths.Value);
        Deaths.OnValueChanged += (_, __) => OnStatsChanged?.Invoke(Kills.Value, Deaths.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Kills.OnValueChanged -= (_, __) => OnStatsChanged?.Invoke(Kills.Value, Deaths.Value);
        Deaths.OnValueChanged -= (_, __) => OnStatsChanged?.Invoke(Kills.Value, Deaths.Value);
    }

    // ---- Server-side methods called by Health / KillFeed ----

    /// <summary>Add a kill. Must be called on the server.</summary>
    public void AddKill()
    {
        if (IsSpawned && IsServer)
        {
            Kills.Value++;
        }
        else if (!IsSpawned)
        {
            offlineKills++;
            OnStatsChanged?.Invoke(offlineKills, offlineDeaths);
        }
    }

    /// <summary>Add a death. Must be called on the server.</summary>
    public void AddDeath()
    {
        if (IsSpawned && IsServer)
        {
            Deaths.Value++;
        }
        else if (!IsSpawned)
        {
            offlineDeaths++;
            OnStatsChanged?.Invoke(offlineKills, offlineDeaths);
        }
    }

    public int GetKills() => IsSpawned ? Kills.Value : offlineKills;
    public int GetDeaths() => IsSpawned ? Deaths.Value : offlineDeaths;
}
