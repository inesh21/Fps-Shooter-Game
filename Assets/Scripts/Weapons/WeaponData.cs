using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General Info")]
    public string weaponName = "New Weapon";
    public GameObject weaponPrefab;

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