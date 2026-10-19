using UnityEngine;

/// <summary>Compatibility relay for scenes that call CameraRecoil directly.</summary>
public sealed class CameraRecoil : MonoBehaviour
{
    [SerializeField] private CameraFollow cameraFollow;

    private void Awake()
    {
        if (cameraFollow == null) cameraFollow = GetComponent<CameraFollow>();
    }

    public void AddRecoil(float pitch, float yaw)
    {
        if (cameraFollow != null) cameraFollow.AddRecoil(pitch, yaw);
    }
}
