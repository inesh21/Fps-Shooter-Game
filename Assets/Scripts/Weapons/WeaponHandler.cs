using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHandler : MonoBehaviour
{
    [Header("Data & References")]
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerFSM player;

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

        // 1. Raycast for instant damage detection (Hitscan)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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

        // 2. Spawn Pooled Tracer for visual trajectory
        Vector3 spawnPosition = firePoint != null ? firePoint.position : ray.origin;
        GameObject tracerObj = ObjectPooler.Instance.SpawnFromPool("Tracer", spawnPosition, Quaternion.identity);

        if (tracerObj != null && tracerObj.TryGetComponent(out BulletTracer tracer))
        {
            tracer.Init(spawnPosition, targetPoint);
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