using UnityEngine;

public sealed class RecoilSpring : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float returnTime = 0.12f;
    [SerializeField, Min(0f)] private float cameraMultiplier = 0.8f;
    [SerializeField] private CameraFollow cameraFollow;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 positionOffset;
    private Vector3 rotationOffset;
    private Vector3 positionVelocity;
    private Vector3 rotationVelocity;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void LateUpdate()
    {
        positionOffset = Vector3.SmoothDamp(positionOffset, Vector3.zero, ref positionVelocity, returnTime);
        rotationOffset = Vector3.SmoothDamp(rotationOffset, Vector3.zero, ref rotationVelocity, returnTime);
        transform.localPosition = startPosition + positionOffset;
        transform.localRotation = startRotation * Quaternion.Euler(rotationOffset);
    }

    public void ApplyRecoil(Vector3 rotation, Vector3 kickback)
    {
        rotationOffset += new Vector3(-Mathf.Abs(rotation.x), Random.Range(-Mathf.Abs(rotation.y), Mathf.Abs(rotation.y)),
            Random.Range(-Mathf.Abs(rotation.z), Mathf.Abs(rotation.z)));
        positionOffset += new Vector3(Random.Range(-Mathf.Abs(kickback.x), Mathf.Abs(kickback.x)),
            Random.Range(-Mathf.Abs(kickback.y), Mathf.Abs(kickback.y)), -Mathf.Abs(kickback.z));

        if (cameraFollow != null)
            cameraFollow.AddRecoil(-Mathf.Abs(rotation.x) * cameraMultiplier,
                Random.Range(-Mathf.Abs(rotation.y), Mathf.Abs(rotation.y)) * cameraMultiplier * 0.3f);
    }
}
