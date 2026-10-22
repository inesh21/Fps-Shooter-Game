using System.Collections;
using UnityEngine;

public class EnemyVisualEffects : MonoBehaviour
{
    [SerializeField] private Renderer enemyRenderer;
    [SerializeField] private float flashDuration = 0.15f;
    
    private Material mat;
    private static readonly int HitIntensityID = Shader.PropertyToID("_HitIntensity");
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private void Awake()
    {
        if (enemyRenderer == null) enemyRenderer = GetComponent<Renderer>();
        mat = enemyRenderer.material; // Creates a local material instance
    }

    public void TriggerHitFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        mat.SetFloat(HitIntensityID, 1f);
        yield return new WaitForSeconds(flashDuration);
        mat.SetFloat(HitIntensityID, 0f);
    }

    public void TriggerDissolve(float duration = 1.5f)
    {
        StartCoroutine(DissolveRoutine(duration));
    }

    private IEnumerator DissolveRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            mat.SetFloat(DissolveAmountID, progress);
            yield return null;
        }
        Destroy(gameObject);
    }
}