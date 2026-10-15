using UnityEngine;

public class RecoilSpring : MonoBehaviour
{
    [Header("Spring Settings")]
    [SerializeField] private float stiffness = 200f; // Speed of snap-back
    [SerializeField] private float damping = 15f;    // Friction/absorption rate

    [Header("Camera Recoil")]
    [SerializeField] private bool applyCameraRecoil = true;
    [SerializeField] private float cameraRecoilMultiplier = 0.8f;

    private CameraRecoil cameraRecoil;

    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Vector3 rotationVelocity;

    private Vector3 currentPosition;
    private Vector3 targetPosition;
    private Vector3 positionVelocity;

    private void Start()
    {
        // Try to find CameraRecoil component on camera
        if (Camera.main != null)
        {
            cameraRecoil = Camera.main.GetComponent<CameraRecoil>();
            if (cameraRecoil == null && applyCameraRecoil)
            {
                cameraRecoil = Camera.main.gameObject.AddComponent<CameraRecoil>();
            }
        }
    }

    private void Update()
    {
        targetRotation = Vector3.SmoothDamp(targetRotation, Vector3.zero, ref rotationVelocity, 1f / stiffness);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, Time.deltaTime * damping);
        transform.localRotation = Quaternion.Euler(currentRotation);

        targetPosition = Vector3.SmoothDamp(targetPosition, Vector3.zero, ref positionVelocity, 1f / stiffness);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * damping);
        transform.localPosition = currentPosition;
    }

    /// <summary>
    /// Applies an instant physical recoil kick.
    /// </summary>
    /// <param name="recoilRotation">Degrees of rotational kick (Pitch, Yaw, Roll)</param>
    /// <param name="recoilKickback">Positional backward kick (X, Y, Z)</param>
    public void ApplyRecoil(Vector3 recoilRotation, Vector3 recoilKickback)
    {
        // Weapon visual recoil
        targetRotation += new Vector3(
            -recoilRotation.x, // Vertical pitch kick
            Random.Range(-recoilRotation.y, recoilRotation.y), // Horizontal yaw variation
            Random.Range(-recoilRotation.z, recoilRotation.z)  // Roll variation
        );

        targetPosition += new Vector3(
            Random.Range(-recoilKickback.x, recoilKickback.x),
            Random.Range(-recoilKickback.y, recoilKickback.y),
            -recoilKickback.z // Push back along Z-axis
        );

        // Apply camera recoil using dedicated component for smooth recovery
        if (applyCameraRecoil && cameraRecoil != null)
        {
            float cameraPitch = -recoilRotation.x * cameraRecoilMultiplier;
            float cameraYaw = Random.Range(-recoilRotation.y, recoilRotation.y) * cameraRecoilMultiplier * 0.3f;

            cameraRecoil.AddRecoil(cameraPitch, cameraYaw);
        }
    }
}