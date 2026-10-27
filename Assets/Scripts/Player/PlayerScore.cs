
using Unity.Netcode;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    public NetworkVariable<int> Kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Deaths = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Conversion Mode additions: persistent individual score & team assignment (0 = Team A, 1 = Team B)
    public NetworkVariable<int> TeamId = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private int offlineKills;
    private int offlineDeaths;
    private int offlineScore;
    private int offlineTeamId;

    public delegate void StatsChangedDelegate(int kills, int deaths);
    public event StatsChangedDelegate OnStatsChanged;

    public delegate void ConversionScoreChangedDelegate(int score, int teamId);
    public event ConversionScoreChangedDelegate OnConversionStatsChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Kills.OnValueChanged += HandleStatsChanged;
        Deaths.OnValueChanged += HandleStatsChanged;
        TeamId.OnValueChanged += HandleConversionStatsChanged;
        Score.OnValueChanged += HandleConversionStatsChanged;

        HandleStatsChanged(0, 0);
        HandleConversionStatsChanged(0, 0);

        if (IsServer && ConversionMatchManager.Instance != null)
        {
            ConversionMatchManager.Instance.Register(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && ConversionMatchManager.Instance != null)
        {
            ConversionMatchManager.Instance.Unregister(this);
        }

        Kills.OnValueChanged -= HandleStatsChanged;
        Deaths.OnValueChanged -= HandleStatsChanged;
        TeamId.OnValueChanged -= HandleConversionStatsChanged;
        Score.OnValueChanged -= HandleConversionStatsChanged;

        base.OnNetworkDespawn();
    }

    private void HandleStatsChanged(int oldValue, int newValue)
    {
        OnStatsChanged?.Invoke(Kills.Value, Deaths.Value);
    }

    private void HandleConversionStatsChanged(int oldValue, int newValue)
    {
        OnConversionStatsChanged?.Invoke(Score.Value, TeamId.Value);
    }

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

    public void AddScore(int points)
    {
        if (IsSpawned && IsServer)
        {
            Score.Value += points;
        }
        else if (!IsSpawned)
        {
            offlineScore += points;
            OnConversionStatsChanged?.Invoke(offlineScore, offlineTeamId);
        }
    }

    public void SetTeam(int team)
    {
        if (IsSpawned && IsServer)
        {
            TeamId.Value = team;
        }
        else if (!IsSpawned)
        {
            offlineTeamId = team;
            OnConversionStatsChanged?.Invoke(offlineScore, offlineTeamId);
        }
    }

    public int GetKills() => IsSpawned ? Kills.Value : offlineKills;
    public int GetDeaths() => IsSpawned ? Deaths.Value : offlineDeaths;
    public int GetScore() => IsSpawned ? Score.Value : offlineScore;
    public int GetTeamId() => IsSpawned ? TeamId.Value : offlineTeamId;
}
