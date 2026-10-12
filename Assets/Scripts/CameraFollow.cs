using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 6, -8);
    [SerializeField] private float rotationSpeed = 0.1f;
    [SerializeField] private float minDistance = 2f;

    private Vector3 currentVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;

        // Use SmoothDamp for better, framerate-independent smoothing
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);

        // Smooth rotation towards target
        Vector3 lookTarget = target.position + Vector3.up * 1.5f;
        Vector3 direction = lookTarget - transform.position;

        if (direction.magnitude > minDistance)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
