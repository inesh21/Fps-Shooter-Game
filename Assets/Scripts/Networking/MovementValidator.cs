using Unity.Netcode;
using UnityEngine;

public class MovementValidator : NetworkBehaviour
{
    [SerializeField] private float maxSpeed = 14f;
    [SerializeField] private float tolerance = 1.5f;
    [SerializeField] private float checkInterval = 0.2f;

    private Vector3 lastPosition;
    private float lastCheckTime;
    private CharacterController controller;

    public override void OnNetworkSpawn()
    {
        lastPosition = transform.position;
        lastCheckTime = Time.time;
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsServer || IsOwner) return;

        float dt = Time.time - lastCheckTime;
        if (dt < checkInterval) return;

        Vector3 flatCurrent = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatLast = new Vector3(lastPosition.x, 0f, lastPosition.z);
        float distance = Vector3.Distance(flatCurrent, flatLast);
        float maxAllowed = maxSpeed * dt * tolerance;

        if (distance > maxAllowed)
        {
            SnapBackClientRpc(lastPosition, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
            });
        }
        else
        {
            lastPosition = transform.position;
        }

        lastCheckTime = Time.time;
    }

    [ClientRpc]
    private void SnapBackClientRpc(Vector3 correctedPos, ClientRpcParams rpcParams = default)
    {
        if (controller != null) controller.enabled = false;
        transform.position = correctedPos;
        if (controller != null) controller.enabled = true;
    }
}
