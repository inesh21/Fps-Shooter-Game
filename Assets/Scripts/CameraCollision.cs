
using UnityEngine;

public sealed class CameraCollision : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private float padding = 0.05f;
    [SerializeField] private float returnSpeed = 10f;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private Vector3 originalLocalPosition;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        Vector3 anchor = transform.parent != null
            ? transform.parent.TransformPoint(originalLocalPosition)
            : originalLocalPosition;

        Vector3 direction = anchor - transform.position;
        float distance = direction.magnitude;

        if (distance > 0.1f &&
            Physics.SphereCast(
                anchor,
                radius,
                direction.normalized,
                out RaycastHit hit,
                distance,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
        {
            Vector3 safePosition =
                anchor + direction.normalized *
                Mathf.Max(0f, hit.distance - padding);

            transform.position = safePosition;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPosition,
                Time.deltaTime * returnSpeed
            );
        }
    }
}
