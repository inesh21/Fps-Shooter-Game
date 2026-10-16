using UnityEngine;

public class CameraDebugger : MonoBehaviour
{
    private PlayerFSM player;
    private CameraFollow cameraFollow;

    private void Start()
    {
        player = FindObjectOfType<PlayerFSM>();
        cameraFollow = FindObjectOfType<CameraFollow>();

        Debug.Log("=== CAMERA SETUP CHECK ===");

        if (player != null)
            Debug.Log("✓ PlayerFSM found");
        else
            Debug.LogError("✗ PlayerFSM NOT found!");

        if (cameraFollow != null)
            Debug.Log("✓ CameraFollow found");
        else
            Debug.LogError("✗ CameraFollow NOT found!");

        if (player != null && player.Actions != null)
            Debug.Log("✓ PlayerInput Actions initialized");
        else
            Debug.LogError("✗ PlayerInput Actions NOT initialized!");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("=== REAL-TIME DEBUG ===");
            Debug.Log($"Camera Position: {transform.position}");
            Debug.Log($"Camera Rotation: {transform.eulerAngles}");

            if (player != null)
            {
                Debug.Log($"Player Position: {player.transform.position}");
                Debug.Log($"Player Rotation: {player.transform.eulerAngles}");
            }

            if (cameraFollow != null)
            {
                Debug.Log("CameraFollow is active");
            }
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 500, 30), "Press F1 for Debug Info", style);

        if (Camera.main != null)
        {
            GUI.Label(new Rect(10, 40, 500, 30), $"Camera Rot: {Camera.main.transform.eulerAngles}", style);
        }

        if (player != null)
        {
            GUI.Label(new Rect(10, 70, 500, 30), $"Player Rot: {player.transform.eulerAngles}", style);
        }
    }
}
