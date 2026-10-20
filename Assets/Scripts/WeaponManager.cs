using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string weaponName;
        public GameObject weaponObject; // This will be the weapon GameObject (e.g., assault1, pistol1)
        [Range(0f, 2f)] public float weaponSwitchSpeed = 1f;
    }

    [Header("Weapons")]
    [SerializeField] private Weapon[] weapons;
    [SerializeField] private int startingWeaponIndex = 0;

    [Header("Switch Animation Settings")]
    [SerializeField] private float switchDuration = 0.5f;
    [SerializeField] private bool useAnimation = true;

    [Header("References")]
    [SerializeField] private Animator characterAnimator; // Character body animator
    [SerializeField] private Transform weaponHolder; // The WeaponHolder transform

    private int currentWeaponIndex = -1;
    private bool isSwitching = false;

    public int CurrentWeaponIndex => currentWeaponIndex;
    public bool IsSwitching => isSwitching;
    public Weapon CurrentWeapon => currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length ? weapons[currentWeaponIndex] : null;

    private void Start()
    {
        // Auto-find WeaponHolder if not assigned
        if (weaponHolder == null)
        {
            Transform camera = transform.Find("Main Camera");
            if (camera != null)
                weaponHolder = camera.Find("WeaponHolder");
        }

        if (weaponHolder == null)
        {
            Debug.LogError("WeaponHolder not found! Please assign it in the inspector or ensure hierarchy is correct.", this);
            return;
        }

        Debug.Log($"=== WeaponManager Start ===");
        Debug.Log($"Total weapons in array: {weapons.Length}");

        // Hide all weapons initially
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].weaponObject != null)
            {
                weapons[i].weaponObject.SetActive(false);
                Debug.Log($"Hiding weapon {i}: {weapons[i].weaponName} ({weapons[i].weaponObject.name})");
            }
            else
            {
                Debug.LogWarning($"Weapon {i} ({weapons[i].weaponName}) has no GameObject assigned!");
            }
        }

        // Equip starting weapon
        if (startingWeaponIndex >= 0 && startingWeaponIndex < weapons.Length)
        {
            Debug.Log($"Equipping starting weapon: Index {startingWeaponIndex}");
            EquipWeapon(startingWeaponIndex, true);
        }
        else
        {
            Debug.LogError($"Invalid starting weapon index: {startingWeaponIndex}. Array length: {weapons.Length}");
        }
    }

    private void Update()
    {
        // Check for weapon switch inputs using new Input System
        if (!isSwitching && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SwitchToWeapon(0); // First weapon (Rifle/Assault)
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SwitchToWeapon(1); // Second weapon (Pistol)
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SwitchToWeapon(2); // Third weapon (optional)
            }
        }
    }

    public void SwitchToWeapon(int weaponIndex)
    {
        // Don't switch if already using this weapon or index is invalid
        if (weaponIndex == currentWeaponIndex || weaponIndex < 0 || weaponIndex >= weapons.Length)
            return;

        // Don't switch if already switching
        if (isSwitching)
            return;

        StartWeaponSwitch(weaponIndex);
    }

    private void StartWeaponSwitch(int newWeaponIndex)
    {
        isSwitching = true;

        // Trigger animation for character body if using animations
        if (useAnimation && characterAnimator != null)
        {
            characterAnimator.SetTrigger("SwitchWeapon");
            characterAnimator.SetInteger("WeaponType", newWeaponIndex);
        }

        // Schedule weapon switch after animation delay
        Invoke(nameof(CompleteWeaponSwitch), switchDuration);
        currentWeaponIndex = newWeaponIndex;
    }

    private void CompleteWeaponSwitch()
    {
        EquipWeapon(currentWeaponIndex, false);
        isSwitching = false;
    }

    private void EquipWeapon(int weaponIndex, bool instant)
    {
        // Hide all weapons
        foreach (var weapon in weapons)
        {
            if (weapon.weaponObject != null)
                weapon.weaponObject.SetActive(false);
        }

        // Show the selected weapon
        if (weaponIndex >= 0 && weaponIndex < weapons.Length)
        {
            currentWeaponIndex = weaponIndex;
            var weapon = weapons[weaponIndex];

            if (weapon.weaponObject != null)
            {
                weapon.weaponObject.SetActive(true);
                Debug.Log($"✓ Equipped: {weapon.weaponName}");
            }
            else
            {
                Debug.LogWarning($"Weapon object for {weapon.weaponName} is not assigned!");
            }

            // Update character animator
            if (characterAnimator != null)
            {
                characterAnimator.SetInteger("WeaponType", weaponIndex);
            }
        }
    }

    // Public method to force equip a weapon
    public void ForceEquipWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weapons.Length)
        {
            EquipWeapon(weaponIndex, true);
        }
    }

    // Get weapon type name for debugging
    public string GetCurrentWeaponName()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length)
            return weapons[currentWeaponIndex].weaponName;
        return "None";
    }
}
