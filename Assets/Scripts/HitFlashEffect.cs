using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls hit flash effect on enemy materials
/// Works with custom shader that has _HitFlashColor and _HitFlashAmount properties
/// </summary>
public class HitFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Dissolve Settings (Optional)")]
    [SerializeField] private bool useDissolve = false;
    [SerializeField] private float dissolveDuration = 1.5f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Renderer[] renderers;
    private Dictionary<Material, Material> originalMaterials = new Dictionary<Material, Material>();
    private List<Material> instanceMaterials = new List<Material>();
    private Coroutine flashCoroutine;
    private Coroutine dissolveCoroutine;

    // Shader property IDs (more efficient than using strings)
    private static readonly int HitFlashColor = Shader.PropertyToID("_HitFlashColor");
    private static readonly int HitFlashAmount = Shader.PropertyToID("_HitFlashAmount");
    private static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");

    private void Awake()
    {
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            // Create material instances to avoid affecting other objects
            var materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (!originalMaterials.ContainsKey(materials[i]))
                {
                    Material instanceMat = new Material(materials[i]);
                    originalMaterials[materials[i]] = materials[i];
                    instanceMaterials.Add(instanceMat);
                    materials[i] = instanceMat;
                }
            }
            renderer.materials = materials;
        }
    }

    /// <summary>
    /// Trigger hit flash effect
    /// </summary>
    public void Flash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Trigger hit flash with custom color
    /// </summary>
    public void Flash(Color color)
    {
        Color originalColor = flashColor;
        flashColor = color;
        Flash();
        flashColor = originalColor;
    }

    /// <summary>
    /// Trigger dissolve effect (for death)
    /// </summary>
    public void Dissolve()
    {
        if (!useDissolve) return;

        if (dissolveCoroutine != null)
            StopCoroutine(dissolveCoroutine);

        dissolveCoroutine = StartCoroutine(DissolveRoutine());
    }

    /// <summary>
    /// Trigger flash then dissolve
    /// </summary>
    public void FlashAndDissolve()
    {
        Flash();
        if (useDissolve)
            Invoke(nameof(Dissolve), flashDuration);
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;

        // Set flash color
        foreach (var mat in instanceMaterials)
        {
            if (mat.HasProperty(HitFlashColor))
                mat.SetColor(HitFlashColor, flashColor);
        }

        // Animate flash amount
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            float flashValue = flashCurve.Evaluate(t);

            foreach (var mat in instanceMaterials)
            {
                if (mat.HasProperty(HitFlashAmount))
                    mat.SetFloat(HitFlashAmount, flashValue);
            }

            yield return null;
        }

        // Ensure flash is fully off
        foreach (var mat in instanceMaterials)
        {
            if (mat.HasProperty(HitFlashAmount))
                mat.SetFloat(HitFlashAmount, 0f);
        }
    }

    private IEnumerator DissolveRoutine()
    {
        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dissolveDuration;
            float dissolveValue = dissolveCurve.Evaluate(t);

            foreach (var mat in instanceMaterials)
            {
                if (mat.HasProperty(DissolveAmount))
                    mat.SetFloat(DissolveAmount, dissolveValue);
            }

            yield return null;
        }

        // Object fully dissolved
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up material instances
        foreach (var mat in instanceMaterials)
        {
            if (mat != null)
                Destroy(mat);
        }
    }

    // Public methods for external control
    public void SetFlashColor(Color color) => flashColor = color;
    public void SetFlashDuration(float duration) => flashDuration = duration;
    public void SetUseDissolve(bool use) => useDissolve = use;
    public void SetDissolveDuration(float duration) => dissolveDuration = duration;

    // Editor testing
    [ContextMenu("Test Flash")]
    private void TestFlash() => Flash();

    [ContextMenu("Test Dissolve")]
    private void TestDissolve() => Dissolve();

    [ContextMenu("Test Flash And Dissolve")]
    private void TestFlashAndDissolve() => FlashAndDissolve();
}
