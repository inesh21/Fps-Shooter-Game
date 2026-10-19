using UnityEngine;

/// <summary>Optional direct hand placement for simple, unanimated rigs.</summary>
public sealed class WeaponHandIK : MonoBehaviour
{
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightTarget;
    [SerializeField] private Transform leftTarget;
    [SerializeField, Range(0f, 1f)] private float weight = 1f;
    [SerializeField] private bool enabledForSimpleRig;

    private void LateUpdate()
    {
        if (!enabledForSimpleRig) return;
        Apply(rightHand, rightTarget);
        Apply(leftHand, leftTarget);
    }

    private void Apply(Transform hand, Transform target)
    {
        if (hand == null || target == null) return;
        hand.SetPositionAndRotation(Vector3.Lerp(hand.position, target.position, weight),
            Quaternion.Slerp(hand.rotation, target.rotation, weight));
    }

    private void OnDrawGizmosSelected()
    {
        DrawTarget(rightTarget, Color.red);
        DrawTarget(leftTarget, Color.cyan);
    }

    private static void DrawTarget(Transform target, Color color)
    {
        if (target == null) return;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(target.position, 0.025f);
        Gizmos.DrawLine(target.position, target.position + target.forward * 0.08f);
    }
}
