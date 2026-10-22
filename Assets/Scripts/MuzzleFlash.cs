using UnityEngine;

/// <summary>
/// Manages muzzle flash visual effects with lighting
/// Attach to weapon's FirePoint
/// </summary>
[RequireComponent(typeof(Light))]
public class MuzzleFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private ParticleSystem flashParticles;
    [SerializeField] private Light flashLight;
    [SerializeField] private GameObject[] flashModels;  // Random flash sprites/meshes

    [Header("Light Settings")]
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightRange = 5f;
    [SerializeField] private Color lightColor = new Color(1f, 0.8f, 0.3f);  // Orange-yellow
    [SerializeField] private float lightDuration = 0.05f;

    [Header("Flash Model Settings")]
    [SerializeField] private Vector3 flashScale = Vector3.one;
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private float flashModelDuration = 0.05f;

    private float lightTimer = 0f;
    private GameObject currentFlashModel;

    private void Awake()
    {
        // Auto-get light component
        if (flashLight == null)
            flashLight = GetComponent<Light>();

        // Ensure light is off initially
        if (flashLight != null)
            flashLight.enabled = false;

        // Auto-find particle system
        if (flashParticles == null)
            flashParticles = GetComponentInChildren<ParticleSystem>();

        // Disable all flash models
        if (flashModels != null)
        {
            foreach (var model in flashModels)
            {
                if (model != null)
                    model.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Handle light fade
        if (lightTimer > 0f)
        {
            lightTimer -= Time.deltaTime;

            if (flashLight != null)
            {
                // Fade light intensity
                float t = lightTimer / lightDuration;
                flashLight.intensity = lightIntensity * t;

                if (lightTimer <= 0f)
                {
                    flashLight.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Play muzzle flash effect
    /// </summary>
    public void PlayFlash()
    {
        // Play particles
        if (flashParticles != null)
            flashParticles.Play();

        // Enable light
        if (flashLight != null)
        {
            flashLight.color = lightColor;
            flashLight.intensity = lightIntensity;
            flashLight.range = lightRange;
            flashLight.enabled = true;
            lightTimer = lightDuration;
        }

        // Show random flash model
        if (flashModels != null && flashModels.Length > 0)
        {
            ShowRandomFlashModel();
        }
    }

    private void ShowRandomFlashModel()
    {
        // Hide previous flash model
        if (currentFlashModel != null)
            currentFlashModel.SetActive(false);

        // Pick random flash model
        GameObject flashModel = flashModels[Random.Range(0, flashModels.Length)];

        if (flashModel != null)
        {
            flashModel.SetActive(true);
            flashModel.transform.localScale = flashScale;

            // Random rotation for variety
            if (randomRotation)
            {
                flashModel.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }

            currentFlashModel = flashModel;

            // Hide after duration
            Invoke(nameof(HideFlashModel), flashModelDuration);
        }
    }

    private void HideFlashModel()
    {
        if (currentFlashModel != null)
            currentFlashModel.SetActive(false);
    }

    // Public API
    public void SetLightColor(Color color) => lightColor = color;
    public void SetLightIntensity(float intensity) => lightIntensity = intensity;
    public void SetLightRange(float range) => lightRange = range;

    [ContextMenu("Test Flash")]
    private void TestFlash() => PlayFlash();
}
