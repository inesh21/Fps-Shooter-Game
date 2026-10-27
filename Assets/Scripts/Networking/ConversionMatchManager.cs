using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Server-authoritative manager for Conversion Mode.
/// Controls match duration, two teams (Team A = 0, Team B = 1),
/// dynamic bounties, team conversion on fatal hit, and round loop.
/// Reference: CONVERSION_MODE_GUIDE.md
/// </summary>
public class ConversionMatchManager : NetworkBehaviour
{
    public static ConversionMatchManager Instance { get; private set; }
    public static event Action<int, ulong[], int[]> OnMatchEnded; // winningTeam, top3 client ids, top3 scores

    [Header("Match Settings")]
    [SerializeField] private float matchDuration = 180f; // 3 minutes
    [SerializeField] private float restartDelay = 10f;
    [SerializeField] private int victoryBonus = 500;

    public NetworkVariable<bool> IsMatchActive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<double> RoundEndTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TeamACount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TeamBCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> WinningTeam = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly List<PlayerScore> activePlayers = new List<PlayerScore>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            StartRound();
        }
    }

    // ---------- Registration (Server) ----------
    public void Register(PlayerScore player)
    {
        if (!IsServer || player == null || activePlayers.Contains(player)) return;

        int teamACount = activePlayers.Count(x => x.TeamId.Value == 0);
        int teamBCount = activePlayers.Count - teamACount;
        player.TeamId.Value = teamACount <= teamBCount ? 0 : 1; // Join smaller team for initial balance

        activePlayers.Add(player);
        RefreshCounts();
    }

    public void Unregister(PlayerScore player)
    {
        if (!IsServer || player == null) return;
        activePlayers.Remove(player);
        RefreshCounts();
    }

    // ---------- Timer & Round Loop (Server) ----------
    private void Update()
    {
        if (!IsServer || !IsMatchActive.Value) return;

        if (NetworkManager.Singleton != null && NetworkManager.ServerTime.Time >= RoundEndTime.Value)
        {
            EndByTimer();
        }
    }

    public void StartRound()
    {
        if (!IsServer) return;

        double serverTime = NetworkManager.Singleton != null ? NetworkManager.ServerTime.Time : 0;
        RoundEndTime.Value = serverTime + matchDuration;
        WinningTeam.Value = -1;
        IsMatchActive.Value = true;
        RefreshCounts();
    }

    // ---------- Conversion Execution (Server) ----------
    public void ProcessConversion(PlayerScore killer, PlayerScore victim)
    {
        if (!IsServer || !IsMatchActive.Value || killer == null || victim == null) return;

        int killerTeam = killer.TeamId.Value;
        int victimTeam = victim.TeamId.Value;

        // Friendly fire safety check
        if (killerTeam == victimTeam) return;

        int killerTeamSize = CountTeam(killerTeam);
        int victimTeamSize = CountTeam(victimTeam);

        // Persistent bounty calculation before conversion
        int bountyPoints = CalculateBounty(killerTeamSize, victimTeamSize);
        killer.AddScore(bountyPoints);

        // Convert victim to killer's team (score is strictly preserved!)
        victim.SetTeam(killerTeam);
        RefreshCounts();

        // Check domination: if victim's old team has 0 players remaining, killer's team dominates
        if (CountTeam(victimTeam) == 0)
        {
            FinishRound(killerTeam);
        }
    }

    public static int CalculateBounty(int killerTeamSize, int victimTeamSize)
    {
        const int basePoints = 100;

        // Last stand bonus: killer is the sole survivor vs multiple enemies
        if (killerTeamSize == 1 && victimTeamSize > 1)
            return basePoints + 500;

        // Underdog bonus: killer team is smaller
        if (killerTeamSize < victimTeamSize)
            return basePoints + (victimTeamSize - killerTeamSize) * 50;

        // Snowball reduction: killer team has numbers advantage
        if (killerTeamSize > victimTeamSize)
            return Mathf.Max(20, basePoints - (killerTeamSize - victimTeamSize) * 20);

        return basePoints;
    }

    private void EndByTimer()
    {
        int a = CountTeam(0);
        int b = CountTeam(1);
        int winner = a != b ? (a > b ? 0 : 1) : (GetTeamAggregatedScore(0) >= GetTeamAggregatedScore(1) ? 0 : 1);
        FinishRound(winner);
    }

    private void FinishRound(int winningTeam)
    {
        if (!IsMatchActive.Value) return;

        IsMatchActive.Value = false;
        WinningTeam.Value = winningTeam;

        // Award +500 victory bonus to winning team members
        foreach (var p in activePlayers)
        {
            if (p != null && p.TeamId.Value == winningTeam)
                p.AddScore(victoryBonus);
        }

        // Top 3 MVPs determined by individual persistent score
        var topMVPs = activePlayers.Where(p => p != null).OrderByDescending(p => p.Score.Value).Take(3).ToList();
        ulong[] ids = topMVPs.Select(p => p.OwnerClientId).ToArray();
        int[] scores = topMVPs.Select(p => p.Score.Value).ToArray();

        MatchEndedClientRpc(winningTeam, ids, scores);

        StartCoroutine(RestartRoutine());
    }

    [ClientRpc]
    private void MatchEndedClientRpc(int winningTeam, ulong[] ids, int[] scores)
    {
        OnMatchEnded?.Invoke(winningTeam, ids, scores);
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        if (!IsServer) yield break;

        var shuffled = activePlayers.Where(p => p != null).OrderBy(_ => Random.value).ToList();
        for (int i = 0; i < shuffled.Count; i++)
        {
            var p = shuffled[i];
            p.TeamId.Value = i % 2;
            p.Score.Value = 0;
            p.Kills.Value = 0;
            p.Deaths.Value = 0;

            var hp = p.GetComponent<Health>();
            if (hp != null)
            {
                hp.ResetForNewRound();
            }
        }

        StartRound();
    }

    private int CountTeam(int team) => activePlayers.Count(p => p != null && p.TeamId.Value == team);
    private int GetTeamAggregatedScore(int team) => activePlayers.Where(p => p != null && p.TeamId.Value == team).Sum(p => p.Score.Value);
    private void RefreshCounts()
    {
        TeamACount.Value = CountTeam(0);
        TeamBCount.Value = CountTeam(1);
    }
}
