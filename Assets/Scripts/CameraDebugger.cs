using UnityEngine;

public sealed class CameraDebugger : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [SerializeField] private bool showOverlay = true;

    private void Awake()
    {
        if (player == null) player = FindAnyObjectByType<PlayerFSM>();
    }

    private void OnGUI()
    {
        if (!showOverlay || player == null) return;
        GUI.Label(new Rect(12, 12, 420, 24), $"Grounded: {player.IsGrounded}  Vertical speed: {player.VerticalVelocity:F2}");
    }
}
