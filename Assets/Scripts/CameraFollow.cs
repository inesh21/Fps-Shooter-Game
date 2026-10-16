using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.73f, 0);
    [SerializeField] private float sensitivity = 2f;
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
        // Lock cursor to the game window
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation to match current camera rotation
        Vector3 currentRotation = transform.localEulerAngles;
        yaw = currentRotation.y;
        pitch = currentRotation.x;

        Debug.Log("CameraFollow started!");
    }

    private void LateUpdate()
    {
        if (player == null || player.Actions == null)
        {
            Debug.LogWarning("Player or Actions is null!");
            return;
        }

        // Read mouse input
        Vector2 lookInput = player.Actions.Player.Look.ReadValue<Vector2>();

        // Debug to see if we're getting input
        if (lookInput.magnitude > 0.01f)
        {
            Debug.Log($"Mouse input: {lookInput}");
        }

        // Accumulate rotation
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Smoothly recover from recoil
        recoilPitch = Mathf.Lerp(recoilPitch, 0f, recoilReturnSpeed * Time.deltaTime);
        recoilYaw = Mathf.Lerp(recoilYaw, 0f, recoilReturnSpeed * Time.deltaTime);

        // Apply base rotation + recoil
        float finalPitch = pitch + recoilPitch;
        float finalYaw = yaw + recoilYaw;

        // Apply rotation to camera
        transform.localRotation = Quaternion.Euler(finalPitch, finalYaw, 0f);

        // Apply rotation to player body (horizontal only)
        if (player != null)
        {
            player.transform.rotation = Quaternion.Euler(0, finalYaw, 0);
        }
    }

    public void AddRecoil(float kickPitch, float kickYaw)
    {
        recoilPitch += kickPitch;
        recoilYaw += kickYaw;

        recoilPitch = Mathf.Clamp(recoilPitch, -maxRecoilAngle, maxRecoilAngle);
        recoilYaw = Mathf.Clamp(recoilYaw, -maxRecoilAngle, maxRecoilAngle);
    }
}