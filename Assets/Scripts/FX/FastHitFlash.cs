using UnityEngine;

public class FastHitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private float flashTimer;
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Flash()
    {
        flashTimer = flashDuration;
        SetFlash(1f);
    }

    private void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        float normalized = Mathf.Clamp01(flashTimer / flashDuration);
        SetFlash(normalized);
    }

    private void SetFlash(float amount)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FlashAmountId, amount);
            propertyBlock.SetColor(FlashColorId, flashColor);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
