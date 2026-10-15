using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, -5f);
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float pitchMin = -20f;
    [SerializeField] private float pitchMax = 80f;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilReturnSpeed = 8f;
    [SerializeField] private float maxRecoilAngle = 15f;

    private float yaw;
    private float pitch;
    private float recoilPitch;
    private float recoilYaw;

    private void Start()
    {
        // Lock cursor to the game window so the mouse doesn't leave the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (player == null || player.Actions == null) return;

        // 1. Read the Look action vector (Delta [Mouse])
        Vector2 lookInput = player.Actions.Player.Look.ReadValue<Vector2>();

        // 2. Accumulate rotation
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // 3. Smoothly recover from recoil
        recoilPitch = Mathf.Lerp(recoilPitch, 0f, recoilReturnSpeed * Time.deltaTime);
        recoilYaw = Mathf.Lerp(recoilYaw, 0f, recoilReturnSpeed * Time.deltaTime);

        // 4. Apply base rotation + recoil
        float finalPitch = pitch + recoilPitch;
        float finalYaw = yaw + recoilYaw;

        // 5. Position and rotate camera around player
        Quaternion rotation = Quaternion.Euler(finalPitch, finalYaw, 0f);
        Vector3 targetPosition = player.transform.position + Vector3.up * offset.y;

        transform.position = targetPosition + (rotation * new Vector3(0, 0, offset.z));
        transform.LookAt(targetPosition);
    }

    /// <summary>
    /// Add camera recoil kick
    /// </summary>
    public void AddRecoil(float kickPitch, float kickYaw)
    {
        recoilPitch += kickPitch;
        recoilYaw += kickYaw;

        // Clamp to prevent extreme angles
        recoilPitch = Mathf.Clamp(recoilPitch, -maxRecoilAngle, maxRecoilAngle);
        recoilYaw = Mathf.Clamp(recoilYaw, -maxRecoilAngle, maxRecoilAngle);
    }
}