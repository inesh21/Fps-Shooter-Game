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

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range, currentWeapon.hitLayers))
        {
            Debug.Log($"Hit {hit.collider.name} for {currentWeapon.damage} damage!");
            Debug.DrawLine(ray.origin, hit.point, Color.red, 0.5f);
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