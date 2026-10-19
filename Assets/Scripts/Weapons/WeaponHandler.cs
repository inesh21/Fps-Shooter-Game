using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class WeaponHandler : MonoBehaviour
{
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerFSM player;
    [SerializeField] private RecoilSpring recoilSpring;
    [SerializeField] private string tracerPoolTag = "Tracer";

    private int ammo;
    private float nextShotTime;
    private bool reloading;
    private Coroutine reloadRoutine;

    public int Ammo => ammo;

    private void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerFSM>();
        if (recoilSpring == null) recoilSpring = GetComponentInChildren<RecoilSpring>();
    }

    private void OnEnable()
    {
        if (player == null || player.Actions == null) return;
        player.Actions.Player.Reload.performed += OnReload;
    }

    private void OnDisable()
    {
        if (player != null && player.Actions != null)
            player.Actions.Player.Reload.performed -= OnReload;
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
        reloading = false;
    }

    private void Start()
    {
        ammo = currentWeapon != null ? currentWeapon.magazineSize : 0;
        if (currentWeapon == null)
            Debug.LogWarning("WeaponHandler has no WeaponData assigned; shooting is disabled.", this);
        if (firePoint == null)
            Debug.LogWarning("WeaponHandler has no Fire Point assigned; the camera is used as the muzzle.", this);
    }

    private void Update()
    {
        if (currentWeapon == null || player == null || player.Actions == null || reloading) return;
        InputAction attack = player.Actions.Player.Attack;
        bool shouldFire = currentWeapon.isAutomatic ? attack.IsPressed() : attack.WasPressedThisFrame();
        if (shouldFire && Time.time >= nextShotTime)
        {
            if (ammo > 0) Fire();
            else BeginReload();
        }
    }

    public void Equip(WeaponData data)
    {
        currentWeapon = data;
        ammo = data != null ? data.magazineSize : 0;
        reloading = false;
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
    }

    private void OnReload(InputAction.CallbackContext context) => BeginReload();

    private void BeginReload()
    {
        if (currentWeapon == null || reloading || ammo >= currentWeapon.magazineSize) return;
        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        reloading = true;
        yield return new WaitForSeconds(currentWeapon.reloadTime);
        if (currentWeapon != null) ammo = currentWeapon.magazineSize;
        reloading = false;
        reloadRoutine = null;
    }

    private void Fire()
    {
        Camera viewCamera = player.transform.GetComponentInChildren<Camera>();
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;

        ammo--;
        nextShotTime = Time.time + currentWeapon.SecondsPerShot;
        Vector3 origin = viewCamera.transform.position;
        Vector3 direction = viewCamera.transform.forward;
        if (currentWeapon.baseSpread > 0f)
        {
            direction += viewCamera.transform.right * Random.Range(-currentWeapon.baseSpread, currentWeapon.baseSpread) * 0.01f;
            direction += viewCamera.transform.up * Random.Range(-currentWeapon.baseSpread, currentWeapon.baseSpread) * 0.01f;
            direction.Normalize();
        }

        Ray ray = new Ray(origin, direction);
        Vector3 target = origin + direction * currentWeapon.range;
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range, currentWeapon.hitLayers, QueryTriggerInteraction.Ignore))
        {
            target = hit.point;
            hit.collider.SendMessage("TakeDamage", currentWeapon.damage, SendMessageOptions.DontRequireReceiver);
        }

        if (recoilSpring != null)
            recoilSpring.ApplyRecoil(currentWeapon.recoilRotation, currentWeapon.recoilKickback);

        Vector3 muzzle = firePoint != null ? firePoint.position : origin;
        if (ObjectPooler.Instance != null)
        {
            GameObject tracerObject = ObjectPooler.Instance.SpawnFromPool(tracerPoolTag, muzzle, Quaternion.LookRotation(target - muzzle));
            if (tracerObject != null && tracerObject.TryGetComponent(out BulletTracer tracer))
                tracer.Init(muzzle, target);
        }
    }
}
