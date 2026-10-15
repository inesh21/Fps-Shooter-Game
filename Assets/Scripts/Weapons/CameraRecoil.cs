using UnityEngine;

/// <summary>
/// Handles camera recoil independently for smoother recovery
/// </summary>
public class CameraRecoil : MonoBehaviour
{
    [Header("Recoil Recovery")]
    [SerializeField] private float returnSpeed = 8f;
    [SerializeField] private float maxRecoilAngle = 15f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private void Update()
    {
        // Smoothly return to zero
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, returnSpeed * Time.deltaTime);

        // Apply rotation
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void AddRecoil(float pitch, float yaw)
    {
        Debug.Log($"CameraRecoil.AddRecoil called! Pitch: {pitch}, Yaw: {yaw}");

        // Add recoil but clamp to prevent extreme angles
        targetRotation.x += pitch;
        targetRotation.y += yaw;

        targetRotation.x = Mathf.Clamp(targetRotation.x, -maxRecoilAngle, maxRecoilAngle);
        targetRotation.y = Mathf.Clamp(targetRotation.y, -maxRecoilAngle, maxRecoilAngle);

        Debug.Log($"Target rotation now: {targetRotation}");
    }
}
