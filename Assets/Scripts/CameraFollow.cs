using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, -5f);
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float pitchMin = -20f;
    [SerializeField] private float pitchMax = 80f;

    private float yaw;
    private float pitch;

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

        // 3. Position and rotate camera around player
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = player.transform.position + Vector3.up * offset.y;

        transform.position = targetPosition + (rotation * new Vector3(0, 0, offset.z));
        transform.LookAt(targetPosition);
    }
}