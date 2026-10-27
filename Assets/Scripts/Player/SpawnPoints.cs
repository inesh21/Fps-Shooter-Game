using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a list of spawn transforms and picks a random (or furthest) one.
/// Place this on a SceneManager-style object. Register spawns via the Inspector
/// or by calling Register() from individual SpawnPoint marker components.
/// </summary>
public class SpawnPoints : MonoBehaviour
{
    public static SpawnPoints Instance { get; private set; }

    [SerializeField] private List<Transform> points = new List<Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Allow SpawnPoint marker objects to self-register at runtime.</summary>
    public void Register(Transform t)
    {
        if (!points.Contains(t))
            points.Add(t);
    }

    /// <summary>Returns a random spawn point transform.</summary>
    public static Transform Random()
    {
        if (Instance == null || Instance.points.Count == 0)
        {
            Debug.LogWarning("[SpawnPoints] No SpawnPoints configured – spawning at world origin.");
            return null;
        }
        return Instance.points[UnityEngine.Random.Range(0, Instance.points.Count)];
    }

    /// <summary>
    /// Returns the spawn point furthest from all active players.
    /// Falls back to Random() if no players are found.
    /// </summary>
    public static Transform Furthest(IEnumerable<Vector3> occupiedPositions)
    {
        if (Instance == null || Instance.points.Count == 0) return null;

        Transform best = null;
        float bestDist = float.NegativeInfinity;

        foreach (Transform sp in Instance.points)
        {
            float minDist = float.PositiveInfinity;
            foreach (Vector3 pos in occupiedPositions)
                minDist = Mathf.Min(minDist, Vector3.Distance(sp.position, pos));

            float score = minDist == float.PositiveInfinity ? 0f : minDist;
            if (score > bestDist)
            {
                bestDist = score;
                best = sp;
            }
        }
        return best ?? Random();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Transform t in points)
        {
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, 0.5f);
            Gizmos.DrawLine(t.position, t.position + t.forward * 1f);
        }
    }
#endif
}
