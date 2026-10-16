using UnityEngine;

/// <summary>
/// Simple IK system to attach hands to weapon grip points
/// Add this to your WeaponHolder or weapon model
/// </summary>
public class WeaponHandIK : MonoBehaviour
{
    [Header("Hand Targets")]
    [SerializeField] private Transform rightHandTarget; // Position on trigger/grip
    [SerializeField] private Transform leftHandTarget;  // Position on foregrip

    [Header("Arm Bones (Optional - for IK)")]
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftHand;

    [Header("Settings")]
    [SerializeField] private bool useIK = false;
    [SerializeField] [Range(0f, 1f)] private float ikWeight = 1f;

    private void OnDrawGizmos()
    {
        // Visualize hand targets in editor
        if (rightHandTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHandTarget.position, 0.02f);
            Gizmos.DrawLine(rightHandTarget.position, rightHandTarget.position + rightHandTarget.forward * 0.05f);
        }

        if (leftHandTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(leftHandTarget.position, 0.02f);
            Gizmos.DrawLine(leftHandTarget.position, leftHandTarget.position + leftHandTarget.forward * 0.05f);
        }
    }

    private void LateUpdate()
    {
        if (!useIK) return;

        // Simple position matching (not true IK)
        if (rightHand != null && rightHandTarget != null)
        {
            rightHand.position = Vector3.Lerp(rightHand.position, rightHandTarget.position, ikWeight);
            rightHand.rotation = Quaternion.Slerp(rightHand.rotation, rightHandTarget.rotation, ikWeight);
        }

        if (leftHand != null && leftHandTarget != null)
        {
            leftHand.position = Vector3.Lerp(leftHand.position, leftHandTarget.position, ikWeight);
            leftHand.rotation = Quaternion.Slerp(leftHand.rotation, leftHandTarget.rotation, ikWeight);
        }
    }
}
