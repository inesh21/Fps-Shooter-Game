using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHandler : MonoBehaviour
{
    [Header("Data & References")]
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerFSM player;
    [SerializeField] private RecoilSpring recoilSpring;

    private int currentAmmo;
    private bool isReloading;
    private float lastFireTime;
    private bool isShootingPressed;

    private void Awake()
    {
        if (player == null) 
            player = GetComponentInParent<PlayerFSM>();
    }

    private void Start()
    {
        // Subscribe in Start() to ensure PlayerFSM.Awake() has already run
        if (player != null && player.Actions != null)
        {
            player.Actions.Player.Attack.started += OnAttackStarted;
            player.Actions.Player.Attack.canceled += OnAttackCanceled;
            player.Actions.Player.Reload.performed += OnReloadPerformed;
        }

        EquipWeapon(currentWeapon);
    }

    private void OnDisable()
    {
        if (player != null && player.Actions != null)
        {
            player.Actions.Player.Attack.started -= OnAttackStarted;
            player.Actions.Player.Attack.canceled -= OnAttackCanceled;
            player.Actions.Player.Reload.performed -= OnReloadPerformed;
        }
    }

    public void EquipWeapon(WeaponData data)
    {
        currentWeapon = data;
        if (currentWeapon != null)
        {
            currentAmmo = currentWeapon.magazineSize;
        }
    }

    private void OnAttackStarted(InputAction.CallbackContext context) => isShootingPressed = true;
    private void OnAttackCanceled(InputAction.CallbackContext context) => isShootingPressed = false;
    private void OnReloadPerformed(InputAction.CallbackContext context) => StartCoroutine(Reload());

    private void Update()
    {
        HandleShooting();
    }

    private void HandleShooting()
    {
        if (!isShootingPressed || isReloading || currentWeapon == null) return;

        if (Time.time - lastFireTime >= currentWeapon.TimeBetweenShots)
        {
            if (currentAmmo > 0)
            {
                Shoot();
                lastFireTime = Time.time;

                if (!currentWeapon.isAutomatic)
                {
                    isShootingPressed = false;
                }
            }
            else
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        if (recoilSpring != null && currentWeapon != null)
        {
            recoilSpring.ApplyRecoil(currentWeapon.recoilRotation, currentWeapon.recoilKickback);
        }
        else
        {
            Debug.LogWarning("RecoilSpring or CurrentWeapon is missing!");
        }

        // 1. Raycast from FirePoint instead of camera center
        Vector3 shootOrigin = firePoint != null ? firePoint.position : Camera.main.transform.position;
        Vector3 shootDirection = Camera.main.transform.forward;

        // Add slight random spread
        if (currentWeapon != null)
        {
            shootDirection += new Vector3(
                Random.Range(-currentWeapon.baseSpread, currentWeapon.baseSpread),
                Random.Range(-currentWeapon.baseSpread, currentWeapon.baseSpread),
                0
            ) * 0.01f;
            shootDirection.Normalize();
        }

        Ray ray = new Ray(shootOrigin, shootDirection);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range, currentWeapon.hitLayers))
        {
            targetPoint = hit.point;
            Debug.Log($"Hit {hit.collider.name} for {currentWeapon.damage} damage!");
        }
        else
        {
            // If nothing was hit, trace to maximum range
            targetPoint = ray.GetPoint(currentWeapon.range);
        }

        // 2. Spawn Pooled Tracer from FirePoint
        if (ObjectPooler.Instance != null)
        {
            GameObject tracerObj = ObjectPooler.Instance.SpawnFromPool("Tracer", shootOrigin, Quaternion.identity);

            if (tracerObj != null && tracerObj.TryGetComponent(out BulletTracer tracer))
            {
                tracer.Init(shootOrigin, targetPoint);
            }
        }
        else
        {
            Debug.LogWarning("ObjectPooler.Instance is null! Make sure ObjectPooler exists in the scene.");
        }
    }

    private IEnumerator Reload()
    {
        if (isReloading || currentAmmo == currentWeapon.magazineSize) yield break;

        isReloading = true;
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        currentAmmo = currentWeapon.magazineSize;
        isReloading = false;
        Debug.Log("Reload Complete!");
    }
}