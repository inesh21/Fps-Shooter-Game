using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WeaponIK : MonoBehaviour
{
    [Header("IK Settings")]
    [SerializeField, Range(0f, 1f)] private float leftHandWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float rightHandWeight = 1f;
    [SerializeField] private bool useIK = true;

    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Auto-find WeaponManager if not assigned
        if (weaponManager == null)
            weaponManager = GetComponent<WeaponManager>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!useIK || animator == null || weaponManager == null)
            return;

        // Get current weapon
        var currentWeapon = weaponManager.CurrentWeapon;
        if (currentWeapon == null || currentWeapon.weaponObject == null)
        {
            // No weapon equipped, disable IK
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            return;
        }

        // Find hand targets in current weapon
        Transform leftTarget = FindChildRecursive(currentWeapon.weaponObject.transform, "LeftHand_Target");
        Transform rightTarget = FindChildRecursive(currentWeapon.weaponObject.transform, "RightHand_Target");

        // Left Hand IK
        if (leftTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }

        // Right Hand IK
        if (rightTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        }
    }

    // Helper function to find child by name recursively
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    // Public methods to control IK
    public void EnableIK() => useIK = true;
    public void DisableIK() => useIK = false;
    public void SetLeftHandWeight(float weight) => leftHandWeight = Mathf.Clamp01(weight);
    public void SetRightHandWeight(float weight) => rightHandWeight = Mathf.Clamp01(weight);
}
