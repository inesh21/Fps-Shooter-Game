using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Crosshair & Hit Marker")]
    [SerializeField] private Image crosshair;
    [SerializeField] private Image hitMarkerImage;
    [SerializeField] private float hitMarkerDuration = 0.12f;

    [Header("Crosshair Spread")]
    [Tooltip("The four directional crosshair lines (top, bottom, left, right).")]
    [SerializeField] private RectTransform[] crosshairParts;   // 0=top 1=bottom 2=left 3=right
    [SerializeField] private float spreadResting   = 4f;
    [SerializeField] private float spreadSprinting = 16f;
    [SerializeField] private float spreadFiring    = 24f;
    [SerializeField] private float spreadLerpSpeed = 12f;

    [Header("Target Bindings")]
    [SerializeField] private Health boundHealth;
    [SerializeField] private NetworkWeaponHandler boundWeapon;

    // Internal state
    private float hitMarkerTimer;
    private float currentSpread;
    private float targetSpread;
    private bool lateBindDone;

    // Spread triggers (called externally)
    public void OnFired()       => targetSpread = spreadFiring;
    public void OnSprinting()   => targetSpread = Mathf.Max(targetSpread, spreadSprinting);
    public void OnStopSprint()  => targetSpread = spreadResting;

    private void Start()
    {
        currentSpread = spreadResting;
        targetSpread  = spreadResting;

        if (hitMarkerImage != null)
            hitMarkerImage.gameObject.SetActive(false);

        BindEvents();
    }

    private void BindEvents()
    {
        if (boundHealth == null)
            boundHealth = GetComponentInParent<Health>();

        if (boundHealth != null)
        {
            boundHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(boundHealth.CurrentHp.Value, boundHealth.MaxHp);
        }

        if (boundWeapon == null)
            boundWeapon = GetComponentInParent<NetworkWeaponHandler>();

        if (boundWeapon != null)
        {
            boundWeapon.OnAmmoChanged += UpdateAmmoUI;
            UpdateAmmoUI(boundWeapon.Ammo, boundWeapon.MaxAmmo);
        }

        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.OnHitMarkerTriggered += ShowHitMarker;
        }

        lateBindDone = true;
    }

    private void OnDestroy()
    {
        if (boundHealth != null)
            boundHealth.OnHealthChanged -= UpdateHealthUI;

        if (boundWeapon != null)
            boundWeapon.OnAmmoChanged -= UpdateAmmoUI;

        if (GameFeel.Instance != null)
            GameFeel.Instance.OnHitMarkerTriggered -= ShowHitMarker;
    }

    private void Update()
    {
        // Late-bind retry in case Awake ran before NetworkSpawn populated the parent.
        if (!lateBindDone) BindEvents();

        // Hit marker timer
        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= Time.deltaTime;
            if (hitMarkerTimer <= 0f && hitMarkerImage != null)
                hitMarkerImage.gameObject.SetActive(false);
        }

        // Crosshair spread
        UpdateCrosshairSpread();
    }

    // ---- Health & Ammo ----

    public void UpdateHealthUI(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value    = current;
        }
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}";
    }

    public void UpdateAmmoUI(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = $"{current} / {max}";
    }

    // ---- Hit Marker ----

    public void ShowHitMarker(bool headshot)
    {
        if (hitMarkerImage == null) return;
        hitMarkerImage.color = headshot ? Color.red : Color.white;
        hitMarkerImage.gameObject.SetActive(true);
        hitMarkerTimer = hitMarkerDuration;
    }

    // ---- Crosshair Spread ----

    private void UpdateCrosshairSpread()
    {
        if (crosshairParts == null || crosshairParts.Length < 4) return;

        // Decay spread back toward resting
        if (targetSpread > spreadResting)
            targetSpread = Mathf.MoveTowards(targetSpread, spreadResting, Time.deltaTime * spreadFiring);

        currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * spreadLerpSpeed);

        // Directions: top(+Y), bottom(-Y), left(-X), right(+X)
        var offsets = new Vector2[]
        {
            new Vector2(0,  currentSpread),
            new Vector2(0, -currentSpread),
            new Vector2(-currentSpread, 0),
            new Vector2( currentSpread, 0),
        };

        for (int i = 0; i < crosshairParts.Length && i < offsets.Length; i++)
        {
            if (crosshairParts[i] != null)
                crosshairParts[i].anchoredPosition = offsets[i];
        }
    }
}
