using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Updates world-model renderers with team color using MaterialPropertyBlock
/// (preserving URP Lit hit flashes and avoiding material instance leaks).
/// Team 0 = Blue, Team 1 = Red.
/// Reference: CONVERSION_MODE_GUIDE.md Step 5
/// </summary>
public class TeamVisuals : NetworkBehaviour
{
    [SerializeField] private PlayerScore score;
    [SerializeField] private Renderer[] worldRenderers;
    [SerializeField] private Color teamAColor = new Color(0.2f, 0.5f, 1f); // Blue
    [SerializeField] private Color teamBColor = new Color(1f, 0.3f, 0.2f); // Red

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (score == null) score = GetComponent<PlayerScore>();
        if (worldRenderers == null || worldRenderers.Length == 0)
            worldRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        propertyBlock = new MaterialPropertyBlock();
        ApplyColor(0, score != null ? score.TeamId.Value : 0);

        if (score != null)
        {
            score.TeamId.OnValueChanged += ApplyColor;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (score != null)
        {
            score.TeamId.OnValueChanged -= ApplyColor;
        }

        base.OnNetworkDespawn();
    }

    private void ApplyColor(int oldTeam, int newTeam)
    {
        if (worldRenderers == null) return;

        Color targetColor = newTeam == 0 ? teamAColor : teamBColor;
        for (int i = 0; i < worldRenderers.Length; i++)
        {
            if (worldRenderers[i] == null) continue;

            // Retain any existing properties in block (e.g. hit flash amount)
            worldRenderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, targetColor);
            worldRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
