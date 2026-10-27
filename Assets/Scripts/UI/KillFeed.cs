using Unity.Netcode;
using UnityEngine;
using TMPro;

public class KillFeed : NetworkBehaviour
{
    public static KillFeed Instance { get; private set; }

    [SerializeField] private Transform listRoot;
    [SerializeField] private TMP_Text entryPrefab;
    [SerializeField] private int maxEntries = 5;
    [SerializeField] private float entryLife = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ReportKillServer(ulong killerId, ulong victimId, bool headshot)
    {
        if (!IsServer && IsSpawned) return;

        KillFeedClientRpc(killerId, victimId, headshot);
    }

    public void ReportKillOffline(ulong killerId, ulong victimId, bool headshot)
    {
        CreateFeedEntry(killerId, victimId, headshot);
    }

    [ClientRpc]
    private void KillFeedClientRpc(ulong killerId, ulong victimId, bool headshot)
    {
        CreateFeedEntry(killerId, victimId, headshot);
    }

    private void CreateFeedEntry(ulong killerId, ulong victimId, bool headshot)
    {
        if (entryPrefab == null || listRoot == null)
        {
            Debug.Log($"[KillFeed] Player {killerId} eliminated Player {victimId}{(headshot ? " [HEADSHOT]" : "")}");
            return;
        }

        TMP_Text entry = Instantiate(entryPrefab, listRoot);
        entry.text = $"Player {killerId}  <color=red>killed</color>  Player {victimId}{(headshot ? " <color=yellow>[HEADSHOT]</color>" : "")}";

        if (listRoot.childCount > maxEntries)
        {
            Destroy(listRoot.GetChild(0).gameObject);
        }

        Destroy(entry.gameObject, entryLife);
    }
}
