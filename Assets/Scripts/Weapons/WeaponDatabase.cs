using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "FPS/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    private static WeaponDatabase runtimeInstance;
    public static WeaponDatabase Instance
    {
        get
        {
            if (runtimeInstance == null)
            {
                runtimeInstance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                if (runtimeInstance == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<WeaponDatabase>();
                    if (all != null && all.Length > 0) runtimeInstance = all[0];
                }
            }
            return runtimeInstance;
        }
    }

    [SerializeField] private List<WeaponData> weapons = new List<WeaponData>();

    public IReadOnlyList<WeaponData> Weapons => weapons;

    public WeaponData GetWeapon(int index)
    {
        if (index >= 0 && index < weapons.Count)
            return weapons[index];
        return null;
    }

    public int GetIndex(WeaponData data)
    {
        return weapons.IndexOf(data);
    }
}
