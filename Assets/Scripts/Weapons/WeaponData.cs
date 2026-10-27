using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "FPS/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    [Header("Weapon Identity")]
    public string weaponName = "Rifle";
    public GameObject weaponPrefab;

    [Header("Firing Parameters")]
    public bool isAutomatic = true;
    [Min(1f)] public float fireRate = 600f; // Rounds per minute
    [Min(0f)] public float damage = 25f;
    public float headshotMultiplier = 2f;
    public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
    [Min(0f)] public float range = 100f;
    [Min(0f)] public float baseSpread = 0.5f;
    public LayerMask hitLayers = Physics.DefaultRaycastLayers;

    [Header("Ammo & Reload")]
    [Min(1)] public int magazineSize = 30;
    [Min(0f)] public float reloadTime = 1.8f;

    [Header("Recoil (Procedural Spring & Camera Kick)")]
    public Vector3 recoilRotation = new Vector3(4f, 1.5f, 1f);
    public Vector3 recoilKickback = new Vector3(0.02f, 0.02f, 0.1f);
    public Vector2 cameraRecoilKick = new Vector2(0.5f, 1.5f);
    public float recoilSnappiness = 6f;
    public float recoilReturnSpeed = 8f;

    [Header("ADS")]
    public float adsFov = 50f;
    public float adsSpeed = 10f;

    [Header("Audio & Visual FX")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;

    public float SecondsPerShot => 60f / Mathf.Max(1f, fireRate);
}
