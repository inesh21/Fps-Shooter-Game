using UnityEngine;

/// <summary>
/// Add this script to Main Camera to debug recoil issues
/// </summary>
public class RecoilDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== RECOIL DEBUG ===");

        // Check if CameraRecoil exists
        CameraRecoil cameraRecoil = GetComponent<CameraRecoil>();
        if (cameraRecoil != null)
        {
            Debug.Log("✓ CameraRecoil component found on Main Camera");
        }
        else
        {
            Debug.LogError("✗ CameraRecoil component NOT found on Main Camera!");
        }

        // Check if RecoilSpring exists
        RecoilSpring recoilSpring = FindObjectOfType<RecoilSpring>();
        if (recoilSpring != null)
        {
            Debug.Log("✓ RecoilSpring found in scene");
        }
        else
        {
            Debug.LogError("✗ RecoilSpring NOT found in scene!");
        }

        // Check if WeaponHandler exists and has references
        WeaponHandler weaponHandler = FindObjectOfType<WeaponHandler>();
        if (weaponHandler != null)
        {
            Debug.Log("✓ WeaponHandler found");
        }
        else
        {
            Debug.LogError("✗ WeaponHandler NOT found!");
        }
    }
}
