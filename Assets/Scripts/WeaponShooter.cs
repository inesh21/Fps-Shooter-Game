using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Basic weapon shooting controller
/// Integrates with MuzzleFlash and ImpactEffectManager
/// </summary>
public class WeaponShooter : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 0.1f;  // Time between shots
    [SerializeField] private float range = 100f;
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask hitLayers = -1;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private MuzzleFlash muzzleFlash;
    [SerializeField] private Camera playerCamera;

    [Header("Effects")]
    [SerializeField] private GameObject bulletTracerPrefab;
    [SerializeField] private float tracerSpeed = 300f;

    private float nextFireTime = 0f;
    private PlayerInput playerInput;

    private void Awake()
    {
        // Auto-find camera
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Auto-find muzzle flash
        if (muzzleFlash == null && firePoint != null)
            muzzleFlash = firePoint.GetComponent<MuzzleFlash>();

        // Get player input
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerInput = player.GetComponent<PlayerFSM>()?.Actions;
    }

    private void Update()
    {
        // Check for fire input
        bool firePressed = false;

        // New Input System
        if (playerInput != null)
            firePressed = playerInput.Player.Attack.IsPressed();

        // Fallback to keyboard
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            firePressed = true;

        // Mouse input (most common)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            firePressed = true;

        // Fire if ready
        if (firePressed && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Fire()
    {
        // Play muzzle flash
        if (muzzleFlash != null)
            muzzleFlash.PlayFlash();

        // Raycast from camera center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            // Create impact effect
            if (ImpactEffectManager.Instance != null)
                ImpactEffectManager.Instance.CreateImpact(hit);

            // Apply damage if hit something with health
            HitFlashEffect hitFlash = hit.collider.GetComponent<HitFlashEffect>();
            if (hitFlash != null)
            {
                hitFlash.Flash();

                // Example: kill on hit (you'd normally check health here)
                // hitFlash.FlashAndDissolve();
            }

            // Spawn bullet tracer
            if (bulletTracerPrefab != null && firePoint != null)
                SpawnTracer(firePoint.position, hit.point);

            // Debug
            Debug.DrawLine(ray.origin, hit.point, Color.red, 0.5f);
        }
        else
        {
            // Spawn tracer to max range
            if (bulletTracerPrefab != null && firePoint != null)
            {
                Vector3 endPoint = ray.origin + ray.direction * range;
                SpawnTracer(firePoint.position, endPoint);
            }

            Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 0.5f);
        }
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        GameObject tracer = Instantiate(bulletTracerPrefab, start, Quaternion.identity);

        // Simple tracer movement (you can make this fancier)

        // if (tracerScript != null)
        //     tracerScript.Initialize(start, end, tracerSpeed);
        // else
        //     Destroy(tracer, 2f);  // Cleanup if no script
    }

    // Public API
    public void SetFireRate(float rate) => fireRate = rate;
    public void SetDamage(int dmg) => damage = dmg;
    public void SetRange(float r) => range = r;
}

/// <summary>
/// Simple bullet tracer visual
/// </summary>

