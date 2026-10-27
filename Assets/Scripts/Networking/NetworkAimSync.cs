using Unity.Netcode;
using UnityEngine;

public class NetworkAimSync : NetworkBehaviour
{
    [SerializeField] private Transform aimPitchTarget;
    [SerializeField] private Camera localCamera;

    public NetworkVariable<float> AimPitch = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        if (localCamera == null)
            localCamera = GetComponentInChildren<Camera>(true);

        if (aimPitchTarget == null && localCamera != null)
            aimPitchTarget = localCamera.transform;
    }

    private void Update()
    {
        if (aimPitchTarget == null) return;

        if (IsSpawned)
        {
            if (IsOwner)
            {
                AimPitch.Value = aimPitchTarget.localEulerAngles.x;
            }
            else
            {
                Vector3 euler = aimPitchTarget.localEulerAngles;
                aimPitchTarget.localRotation = Quaternion.Euler(AimPitch.Value, euler.y, euler.z);
            }
        }
    }
}
