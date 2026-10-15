using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General Info")]
    public string weaponName = "New Weapon";
    public GameObject weaponPrefab;
    [Header("Spring Recoil Profile")]
    public Vector3 recoilRotation = new Vector3(4f, 1.5f, 1f); // Degrees
    public Vector3 recoilKickback = new Vector3(0.02f, 0.02f, 0.1f); // Meters

    [Header("Shooting Properties")]
    public bool isAutomatic = true;
    public float fireRate = 600f; // Rounds per minute
    public float damage = 25f;
    public float range = 100f;
    public LayerMask hitLayers;

    [Header("Ammo & Reloading")]
    public int magazineSize = 30;
    public float reloadTime = 1.8f;

    [Header("Recoil & Accuracy")]
    public float baseSpread = 0.5f;
    public float maxSpread = 3.0f;
    public AnimationCurve recoilKickCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Calculated time between shots in seconds
    public float TimeBetweenShots => 60f / fireRate;
}