using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Physical Material types for different surface impacts
/// </summary>
public enum SurfaceType
{
    Default,
    Concrete,
    Metal,
    Wood,
    Glass,
    Dirt,
    Flesh,
    Water
}

/// <summary>
/// Manages bullet impact effects based on surface material
/// Spawns appropriate particles, decals, and sounds
/// </summary>
public class ImpactEffectManager : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceEffect
    {
        public SurfaceType surfaceType;
        [Header("Particles")]
        public ParticleSystem impactParticles;
        public int particleCount = 10;

        [Header("Decals")]
        public GameObject decalPrefab;
        public float decalSize = 0.1f;
        public float decalLifetime = 10f;

        [Header("Audio")]
        public AudioClip impactSound;
        [Range(0f, 1f)] public float soundVolume = 0.8f;
    }

    [Header("Surface Effects")]
    [SerializeField] private SurfaceEffect[] surfaceEffects;

    [Header("Settings")]
    [SerializeField] private LayerMask impactLayers = -1;
    [SerializeField] private float maxDecals = 50;
    [SerializeField] private Transform decalParent;

    [Header("Physics Material Mapping")]
    [SerializeField] private PhysicsMaterialMapping[] materialMappings;

    private Dictionary<PhysicsMaterial, SurfaceType> materialDict = new Dictionary<PhysicsMaterial, SurfaceType>();
    private Dictionary<SurfaceType, SurfaceEffect> effectDict = new Dictionary<SurfaceType, SurfaceEffect>();
    private Queue<GameObject> activeDecals = new Queue<GameObject>();
    private AudioSource audioSource;

    [System.Serializable]
    public class PhysicsMaterialMapping
    {
        public PhysicsMaterial physicsMaterial;
        public SurfaceType surfaceType;
    }

    public static ImpactEffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create audio source for impact sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound

        // Create decal parent if not assigned
        if (decalParent == null)
        {
            GameObject decalObj = new GameObject("ImpactDecals");
            decalParent = decalObj.transform;
        }

        // Build dictionaries for fast lookup
        BuildDictionaries();
    }

    public void SpawnImpactEffect(Vector3 position, Vector3 normal, SurfaceType surfaceType)
    {
        CreateImpact(position, normal, surfaceType, null);
    }

    private void BuildDictionaries()
    {
        // Map physics materials to surface types
        if (materialMappings != null)
        {
            foreach (var mapping in materialMappings)
            {
                if (mapping.physicsMaterial != null && !materialDict.ContainsKey(mapping.physicsMaterial))
                {
                    materialDict.Add(mapping.physicsMaterial, mapping.surfaceType);
                }
            }
        }

        // Map surface types to effects
        if (surfaceEffects != null)
        {
            foreach (var effect in surfaceEffects)
            {
                if (!effectDict.ContainsKey(effect.surfaceType))
                {
                    effectDict.Add(effect.surfaceType, effect);
                }
            }
        }
    }

    /// <summary>
    /// Create impact effect at raycast hit point
    /// </summary>
    public void CreateImpact(RaycastHit hit)
    {
        SurfaceType surfaceType = GetSurfaceType(hit);
        CreateImpact(hit.point, hit.normal, surfaceType, hit.transform);
    }

    /// <summary>
    /// Create impact effect at specific point with surface type
    /// </summary>
    public void CreateImpact(Vector3 position, Vector3 normal, SurfaceType surfaceType, Transform hitObject = null)
    {
        if (!effectDict.TryGetValue(surfaceType, out SurfaceEffect effect))
        {
            // Fall back to default if surface type not found
            if (!effectDict.TryGetValue(SurfaceType.Default, out effect))
                return;
        }

        // Spawn particles
        if (effect.impactParticles != null)
        {
            SpawnParticles(effect.impactParticles, position, normal, effect.particleCount);
        }

        // Spawn decal
        if (effect.decalPrefab != null)
        {
            SpawnDecal(effect.decalPrefab, position, normal, effect.decalSize, effect.decalLifetime, hitObject);
        }

        // Play sound
        if (effect.impactSound != null)
        {
            PlayImpactSound(effect.impactSound, position, effect.soundVolume);
        }
    }

    private SurfaceType GetSurfaceType(RaycastHit hit)
    {
        // Try to get surface type from collider's physics material
        Collider collider = hit.collider;
        if (collider != null)
        {
            PhysicsMaterial physicsMat = collider.sharedMaterial;
            if (physicsMat != null && materialDict.TryGetValue(physicsMat, out SurfaceType surfaceType))
            {
                return surfaceType;
            }

            // Try to get from tag
            if (collider.CompareTag("Metal")) return SurfaceType.Metal;
            if (collider.CompareTag("Wood")) return SurfaceType.Wood;
            if (collider.CompareTag("Glass")) return SurfaceType.Glass;
            if (collider.CompareTag("Concrete")) return SurfaceType.Concrete;
            if (collider.CompareTag("Dirt")) return SurfaceType.Dirt;
        }

        return SurfaceType.Default;
    }

    private void SpawnParticles(ParticleSystem particlePrefab, Vector3 position, Vector3 normal, int count)
    {
        ParticleSystem particles = Instantiate(particlePrefab, position, Quaternion.LookRotation(normal));

        var main = particles.main;
        main.maxParticles = count;

        particles.Play();

        // Destroy after playing
        Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
    }

    private void SpawnDecal(GameObject decalPrefab, Vector3 position, Vector3 normal, float size, float lifetime, Transform parent)
    {
        // Offset slightly from surface to prevent z-fighting
        Vector3 decalPosition = position + normal * 0.001f;
        Quaternion decalRotation = Quaternion.LookRotation(-normal);

        // Add random rotation around normal
        decalRotation *= Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        GameObject decal = Instantiate(decalPrefab, decalPosition, decalRotation, decalParent);
        decal.transform.localScale = Vector3.one * size;

        // Parent to hit object if it can move
        Rigidbody rb = parent != null ? parent.GetComponent<Rigidbody>() : null;
        if (rb != null)
        {
            decal.transform.SetParent(parent, true);
        }

        // Manage decal pool
        activeDecals.Enqueue(decal);
        if (activeDecals.Count > maxDecals)
        {
            GameObject oldestDecal = activeDecals.Dequeue();
            if (oldestDecal != null)
                Destroy(oldestDecal);
        }

        // Destroy after lifetime
        if (lifetime > 0)
            Destroy(decal, lifetime);
    }

    private void PlayImpactSound(AudioClip clip, Vector3 position, float volume)
    {
        audioSource.transform.position = position;
        audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Clear all active decals
    /// </summary>
    public void ClearAllDecals()
    {
        while (activeDecals.Count > 0)
        {
            GameObject decal = activeDecals.Dequeue();
            if (decal != null)
                Destroy(decal);
        }
    }

    // Public API
    public void SetMaxDecals(int max) => maxDecals = max;
}
