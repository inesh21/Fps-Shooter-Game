using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NetworkWeaponHandler : NetworkBehaviour
{
    [Header("Weapon Database")]
    [SerializeField] private WeaponDatabase database;
    [SerializeField] private int initialWeaponIndex = 0;

    [Header("Muzzle & Visuals")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private RecoilSpring recoilSpring;
    [SerializeField] private MuzzleFlash muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string tracerPoolTag = "Tracer";

    [Header("Network Sync")]
    public NetworkVariable<int> CurrentWeaponIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private WeaponData currentWeapon;
    private PlayerFSM player;
    private Camera playerCamera;
    private int currentAmmo;
    private float nextFireTime;
    private bool isReloading;
    private Coroutine reloadRoutine;

    public WeaponData CurrentWeapon => currentWeapon;
    public int Ammo => currentAmmo;
    public int MaxAmmo => currentWeapon != null ? currentWeapon.magazineSize : 0;
    public bool IsReloading => isReloading;

    public delegate void AmmoChangedDelegate(int current, int max);
    public event AmmoChangedDelegate OnAmmoChanged;

    private void Awake()
    {
        player = GetComponentInParent<PlayerFSM>();
        playerCamera = GetComponentInChildren<Camera>(true);
        if (recoilSpring == null) recoilSpring = GetComponentInChildren<RecoilSpring>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (muzzleFlash == null && firePoint != null) muzzleFlash = firePoint.GetComponent<MuzzleFlash>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentWeaponIndex.OnValueChanged += HandleWeaponChanged;
        EquipByIndex(CurrentWeaponIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentWeaponIndex.OnValueChanged -= HandleWeaponChanged;
    }

    private void Start()
    {
        if (!IsSpawned)
        {
            // Offline fallback
            EquipByIndex(initialWeaponIndex);
        }
    }

    private void HandleWeaponChanged(int oldVal, int newVal)
    {
        EquipByIndex(newVal);
    }

    public void EquipByIndex(int index)
    {
        if (database == null) database = WeaponDatabase.Instance;
        if (database != null)
        {
            var weapon = database.GetWeapon(index);
            if (weapon != null)
            {
                currentWeapon = weapon;
                currentAmmo = weapon.magazineSize;
                isReloading = false;
                if (reloadRoutine != null) StopCoroutine(reloadRoutine);
                reloadRoutine = null;
                OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
            }
        }
    }

    private void Update()
    {
        if (IsSpawned && !IsOwner) return;
        if (currentWeapon == null || isReloading) return;

        // Weapon switching input (1, 2)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchWeapon(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchWeapon(1);
        }

        // Fire input
        bool firePressed = false;
        if (player != null && player.Actions != null)
        {
            InputAction attack = player.Actions.Player.Attack;
            firePressed = currentWeapon.isAutomatic ? attack.IsPressed() : attack.WasPressedThisFrame();
        }
        else if (Mouse.current != null)
        {
            firePressed = currentWeapon.isAutomatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (firePressed && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                TryFire();
                nextFireTime = Time.time + currentWeapon.SecondsPerShot;
            }
            else
            {
                StartReload();
            }
        }

        // Reload input
        bool reloadPressed = false;
        if (player != null && player.Actions != null)
            reloadPressed = player.Actions.Player.Reload.WasPressedThisFrame();
        else if (Keyboard.current != null)
            reloadPressed = Keyboard.current.rKey.wasPressedThisFrame;

        if (reloadPressed) StartReload();
    }

    public void SwitchWeapon(int index)
    {
        if (index == CurrentWeaponIndex.Value) return;
        if (database != null && database.GetWeapon(index) != null)
        {
            if (IsSpawned && IsOwner)
            {
                CurrentWeaponIndex.Value = index;
            }
            else
            {
                EquipByIndex(index);
            }
        }
    }

    private void StartReload()
    {
        if (isReloading || currentWeapon == null || currentAmmo >= currentWeapon.magazineSize) return;
        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        if (audioSource != null && currentWeapon.reloadSound != null)
        {
            audioSource.PlayOneShot(currentWeapon.reloadSound);
        }

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        currentAmmo = currentWeapon.magazineSize;
        isReloading = false;
        OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);
    }

    private void TryFire()
    {
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, currentWeapon.magazineSize);

        Transform camTransform = playerCamera != null ? playerCamera.transform : (Camera.main != null ? Camera.main.transform : transform);
        Vector3 origin = camTransform.position;
        Vector3 direction = camTransform.forward;

        // Apply spread
        if (currentWeapon.baseSpread > 0f)
        {
            direction += camTransform.TransformDirection(new Vector3(
                Random.Range(-currentWeapon.baseSpread, currentWeapon.baseSpread) * 0.01f,
                Random.Range(-currentWeapon.baseSpread, currentWeapon.baseSpread) * 0.01f,
                0f
            ));
            direction.Normalize();
        }

        Vector3 hitPoint = origin + direction * currentWeapon.range;
        SurfaceType surfaceKind = SurfaceType.Default;
        bool didHit = false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, currentWeapon.range, currentWeapon.hitLayers))
        {
            hitPoint = hit.point;
            didHit = true;
            if (hit.collider.TryGetComponent(out SurfaceIdentifier surfaceId))
            {
                surfaceKind = surfaceId.Surface;
            }
        }

        // Local instant FX
        PlayLocalFireEffects(hitPoint);

        // Network RPC
        if (IsSpawned)
        {
            FireServerRpc(origin, direction, hitPoint, didHit, surfaceKind);
        }
        else if (didHit)
        {
            // Offline damage handling
            if (Physics.Raycast(origin, direction, out RaycastHit offlineHit, currentWeapon.range, currentWeapon.hitLayers))
            {
                if (offlineHit.collider.GetComponentInParent<Health>() is Health targetHealth)
                {
                    bool isHeadshot = offlineHit.collider.CompareTag("Head");
                    float damage = currentWeapon.damage * (isHeadshot ? currentWeapon.headshotMultiplier : 1f);
                    targetHealth.TakeDamage(damage, 0, isHeadshot);
                    if (GameFeel.Instance != null) GameFeel.Instance.TriggerHitMarker(isHeadshot);
                }
            }
        }
    }

    private void PlayLocalFireEffects(Vector3 targetPoint)
    {
        if (muzzleFlash != null) muzzleFlash.PlayFlash();

        if (audioSource != null && currentWeapon != null && currentWeapon.fireSound != null)
        {
            audioSource.pitch = Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(currentWeapon.fireSound);
        }

        if (recoilSpring != null && currentWeapon != null)
        {
            recoilSpring.ApplyRecoil(currentWeapon.recoilRotation, currentWeapon.recoilKickback);
        }

        SpawnTracer(targetPoint);
    }

    private void SpawnTracer(Vector3 destination)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        if (ObjectPooler.Instance != null)
        {
            GameObject tracerObj = ObjectPooler.Instance.SpawnFromPool(tracerPoolTag, spawnPos, Quaternion.LookRotation(destination - spawnPos));
            if (tracerObj != null)
            {
                if (tracerObj.TryGetComponent(out BulletTracer tracer))
                {
                    tracer.Init(spawnPos, destination);
                }
            }
        }
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 origin, Vector3 dir, Vector3 clientHitPoint, bool clientDidHit, SurfaceType surfaceKind, ServerRpcParams rpcParams = default)
    {
        if (currentWeapon == null) return;

        // Anti-exploit check: distance from shooter position
        if (Vector3.Distance(origin, transform.position) > 4f) return;

        bool isServerHit = Physics.Raycast(origin, dir, out RaycastHit hit, currentWeapon.range, currentWeapon.hitLayers);
        if (isServerHit)
        {
            var myScore = GetComponent<PlayerScore>();
            var match = ConversionMatchManager.Instance;

            if (hit.collider.GetComponentInParent<Health>() is Health targetHealth && targetHealth.OwnerClientId != OwnerClientId)
            {
                var targetScore = targetHealth.GetComponent<PlayerScore>();
                // Friendly Fire Gate: players on the same team do not damage each other!
                bool isSameTeam = myScore != null && targetScore != null && myScore.TeamId.Value == targetScore.TeamId.Value;
                bool isMatchActive = match == null || match.IsMatchActive.Value;

                if (!isSameTeam && isMatchActive)
                {
                    bool isHead = hit.collider.CompareTag("Head");
                    float distRatio = Mathf.Clamp01(hit.distance / currentWeapon.range);
                    float damage = currentWeapon.damage * currentWeapon.damageFalloff.Evaluate(distRatio) * (isHead ? currentWeapon.headshotMultiplier : 1f);

                    targetHealth.TakeDamage(damage, OwnerClientId, isHead);

                    // Confirm hit only to the owner
                    HitConfirmClientRpc(isHead, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                    });
                }
            }

            SurfaceType sKind = SurfaceType.Default;
            if (hit.collider.TryGetComponent(out SurfaceIdentifier surfaceId))
                sKind = surfaceId.Surface;

            ImpactClientRpc(hit.point, hit.normal, sKind);
        }

        // Notify other clients for third-person sound/flash
        ShotFxClientRpc(clientHitPoint);
    }

    [ClientRpc]
    private void HitConfirmClientRpc(bool headshot, ClientRpcParams rpcParams = default)
    {
        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.TriggerHitMarker(headshot);
        }
    }

    [ClientRpc]
    private void ShotFxClientRpc(Vector3 destination)
    {
        if (IsOwner) return; // Owner already played instant local visuals

        if (muzzleFlash != null) muzzleFlash.PlayFlash();
        if (audioSource != null && currentWeapon != null && currentWeapon.fireSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(currentWeapon.fireSound);
        }
        SpawnTracer(destination);
    }

    [ClientRpc]
    private void ImpactClientRpc(Vector3 hitPoint, Vector3 normal, SurfaceType surfaceKind)
    {
        if (ImpactEffectManager.Instance != null)
        {
            ImpactEffectManager.Instance.SpawnImpactEffect(hitPoint, normal, surfaceKind);
        }
    }
}
