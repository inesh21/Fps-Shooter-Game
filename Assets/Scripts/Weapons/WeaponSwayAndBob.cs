using UnityEngine;

public sealed class WeaponSwayAndBob : MonoBehaviour
{
    [SerializeField] private PlayerFSM player;
    [Header("Sway")]
    [SerializeField, Min(0f)] private float swayScale = 0.015f;
    [SerializeField, Min(0f)] private float maximumSway = 3f;
    [SerializeField, Min(0f)] private float swaySharpness = 12f;
    [Header("Bobbing")]
    [SerializeField, Min(0f)] private float bobFrequency = 9f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.025f;
    [SerializeField, Min(1f)] private float sprintMultiplier = 1.5f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float phase;

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerFSM>();
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (player == null || player.Actions == null || player.Controller == null) return;
        Vector2 look = player.Actions.Player.Look.ReadValue<Vector2>();
        Vector3 velocity = player.CurrentVelocity;
        velocity.y = 0f;
        bool walking = player.IsGrounded && velocity.sqrMagnitude > 0.04f;
        bool sprinting = walking && player.SprintHeld;

        Vector3 bob = Vector3.zero;
        if (walking)
        {
            phase += Time.deltaTime * bobFrequency * (sprinting ? sprintMultiplier : 1f);
            float amount = bobAmplitude * (sprinting ? sprintMultiplier : 1f);
            bob = new Vector3(Mathf.Cos(phase * 0.5f), Mathf.Sin(phase), 0f) * amount;
        }
        else phase = Mathf.MoveTowards(phase, 0f, Time.deltaTime * bobFrequency);

        Vector3 swayAngles = new Vector3(-look.y, look.x, -look.x) * swayScale;
        swayAngles = Vector3.ClampMagnitude(swayAngles, maximumSway);
        float blend = 1f - Mathf.Exp(-swaySharpness * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition + bob, blend);
        transform.localRotation = Quaternion.Slerp(transform.localRotation,
            baseRotation * Quaternion.Euler(swayAngles), blend);
    }
}
