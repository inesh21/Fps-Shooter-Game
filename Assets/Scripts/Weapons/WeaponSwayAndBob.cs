using UnityEngine;

public class WeaponSwayAndBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFSM player;

    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 0.5f;
    [SerializeField] private float maxSwayAmount = 2f;
    [SerializeField] private float swaySmoothness = 8f;

    [Header("Bobbing Settings")]
    [SerializeField] private float walkBobFrequency = 10f;
    [SerializeField] private float walkBobHorizontalAmplitude = 0.05f;
    [SerializeField] private float walkBobVerticalAmplitude = 0.05f;
    
    [SerializeField] private float sprintBobMultiplier = 1.5f;
    [SerializeField] private float bobSmoothness = 10f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float bobTimer;

    private void Start()
    {
        if (player == null) player = GetComponentInParent<PlayerFSM>();

        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        if (player == null || player.Actions == null) return;

        HandleSway();
        HandleBobbing();
    }

    private void HandleSway()
    {
        // 1. Read Mouse Look Input
        Vector2 lookInput = player.Actions.Player.Look.ReadValue<Vector2>();

        // 2. Calculate sway rotations based on mouse delta
        float pitch = Mathf.Clamp(-lookInput.y * swayAmount, -maxSwayAmount, maxSwayAmount);
        float yaw = Mathf.Clamp(lookInput.x * swayAmount, -maxSwayAmount, maxSwayAmount);

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f) * initialRotation;

        // 3. Smoothly interpolate rotation back to origin
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * swaySmoothness);
    }

    private void HandleBobbing()
    {
        // Calculate horizontal movement speed of the character
        Vector3 playerVelocity = player.Controller.velocity;
        playerVelocity.y = 0f;
        float speed = playerVelocity.magnitude;

        Vector3 targetBobPosition = initialPosition;

        if (speed > 0.1f && player.Controller.isGrounded)
        {
            // Determine if player is sprinting to scale frequency
            bool isSprinting = speed > player.walkSpeed + 0.5f;
            float currentFrequency = isSprinting ? walkBobFrequency * sprintBobMultiplier : walkBobFrequency;

            bobTimer += Time.deltaTime * currentFrequency;

            // Lissajous curve math for realistic figure-8 weapon motion
            float horizontalOffset = Mathf.Cos(bobTimer * 0.5f) * walkBobHorizontalAmplitude;
            float verticalOffset = Mathf.Sin(bobTimer) * walkBobVerticalAmplitude;

            if (isSprinting)
            {
                horizontalOffset *= 1.5f;
                verticalOffset *= 1.5f;
            }

            targetBobPosition = initialPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
        }
        else
        {
            // Reset timer smoothly when idle
            bobTimer = 0f;
        }

        // Apply smooth position transition
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetBobPosition, Time.deltaTime * bobSmoothness);
    }
}