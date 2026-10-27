using UnityEngine;

/// <summary>
/// Place this component on any GameObject in the scene that should act as a
/// player spawn point.  It self-registers with the SpawnPoints singleton.
/// Tag the GameObject "Respawn" for compatibility with older tag-based lookups.
/// </summary>
[AddComponentMenu("FPS/Spawn Point Marker")]
public class SpawnPointMarker : MonoBehaviour
{
    private void Awake()
    {
        if (SpawnPoints.Instance != null)
            SpawnPoints.Instance.Register(transform);
        else
            // SpawnPoints singleton might not exist yet in Awake order; defer.
            Invoke(nameof(Register), 0f);
    }

    private void Register()
    {
        if (SpawnPoints.Instance != null)
            SpawnPoints.Instance.Register(transform);
        else
            Debug.LogWarning("[SpawnPointMarker] No SpawnPoints singleton found in the scene.", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.4f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 1.2f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, gameObject.name);
    }
#endif
}
