
using UnityEngine;

public sealed class WeaponAntiClip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform viewCamera;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private float checkDistance = 0.8f;
    [SerializeField] private float radius = 0.12f;
    [SerializeField] private float pullbackDistance = 0.35f;
    [SerializeField] private float moveSpeed = 12f;

    private Vector3 originalLocalPosition;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;

        if (viewCamera == null && Camera.main != null)
            viewCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (viewCamera == null)
            return;

        Vector3 origin = viewCamera.position;
        Vector3 direction = viewCamera.forward;

        float targetOffset = 0f;

        if (Physics.SphereCast(
            origin,
            radius,
            direction,
            out RaycastHit hit,
            checkDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            targetOffset = pullbackDistance *
                (1f - Mathf.Clamp01(
                    hit.distance / checkDistance));
        }

        Vector3 targetPosition = originalLocalPosition -
            Vector3.forward * targetOffset;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }
}
