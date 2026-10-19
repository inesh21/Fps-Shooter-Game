using UnityEngine;

public sealed class RecoilDebugger : MonoBehaviour
{
    [SerializeField] private WeaponHandler weapon;

    private void Awake()
    {
        if (weapon == null) weapon = FindAnyObjectByType<WeaponHandler>();
    }

    private void OnGUI()
    {
        if (weapon == null) return;
        GUI.Label(new Rect(12, 36, 240, 24), $"Ammo: {weapon.Ammo}");
    }
}
