using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [SerializeField, Min(0f)] private float sensitivity = 0.08f;
    [SerializeField] private float minimumPitch = -85f;
    [SerializeField] private float maximumPitch = 85f;
    [SerializeField, Min(0f)] private float recoilRecovery = 10f;
    [SerializeField, Min(0f)] private float maximumRecoil = 15f;

    private float yaw;
    private float pitch;
    private Vector2 recoil;

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerFSM>();
        yaw = player != null ? player.transform.eulerAngles.y : transform.eulerAngles.y;
        pitch = NormalizeAngle(transform.localEulerAngles.x);
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (player == null || player.Actions == null) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector2 look = player.Actions.Player.Look.ReadValue<Vector2>();
        yaw += look.x * sensitivity;
        pitch = Mathf.Clamp(pitch - look.y * sensitivity, minimumPitch, maximumPitch);
        recoil = Vector2.Lerp(recoil, Vector2.zero, 1f - Mathf.Exp(-recoilRecovery * Time.deltaTime));

        player.transform.rotation = Quaternion.Euler(0f, yaw + recoil.y, 0f);
        transform.localRotation = Quaternion.Euler(pitch + recoil.x, 0f, 0f);
    }

    public void AddRecoil(float pitchKick, float yawKick)
    {
        recoil.x = Mathf.Clamp(recoil.x + pitchKick, -maximumRecoil, maximumRecoil);
        recoil.y = Mathf.Clamp(recoil.y + yawKick, -maximumRecoil, maximumRecoil);
    }

    private static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;
}
