using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "FPS/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    [Header("Weapon")]
    public string weaponName = "Rifle";
    public GameObject weaponPrefab;

    [Header("Fire")]
    public bool isAutomatic = true;
    [Min(1f)] public float fireRate = 600f;
    [Min(0f)] public float damage = 25f;
    [Min(0f)] public float range = 100f;
    [Min(0f)] public float baseSpread = 0.5f;
    public LayerMask hitLayers = Physics.DefaultRaycastLayers;

    [Header("Ammo")]
    [Min(1)] public int magazineSize = 30;
    [Min(0f)] public float reloadTime = 1.8f;

    [Header("Recoil")]
    public Vector3 recoilRotation = new Vector3(4f, 1.5f, 1f);
    public Vector3 recoilKickback = new Vector3(0.02f, 0.02f, 0.1f);

    public float SecondsPerShot => 60f / Mathf.Max(1f, fireRate);
}
