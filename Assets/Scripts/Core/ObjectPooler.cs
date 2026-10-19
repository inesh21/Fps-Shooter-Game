using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [System.Serializable]
    public sealed class Pool
    {
        public string tag;
        public GameObject prefab;
        [Min(0)] public int size = 20;
    }

    [SerializeField] private List<Pool> pools = new List<Pool>();
    private readonly Dictionary<string, Queue<GameObject>> available = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<GameObject, string> poolByObject = new Dictionary<GameObject, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one ObjectPooler may be active in a scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
        BuildPools();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildPools()
    {
        foreach (Pool config in pools)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.tag) || config.prefab == null || config.size < 0)
            {
                Debug.LogWarning("Skipping an invalid pool configuration.", this);
                continue;
            }
            if (available.ContainsKey(config.tag))
            {
                Debug.LogWarning($"Pool tag '{config.tag}' is duplicated.", this);
                continue;
            }

            Queue<GameObject> queue = new Queue<GameObject>();
            available.Add(config.tag, queue);
            for (int i = 0; i < config.size; i++)
            {
                GameObject instance = Instantiate(config.prefab, transform);
                instance.SetActive(false);
                poolByObject.Add(instance, config.tag);
                queue.Enqueue(instance);
            }
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!available.TryGetValue(tag, out Queue<GameObject> queue))
        {
            Debug.LogWarning($"No object pool is configured for '{tag}'.", this);
            return null;
        }
        while (queue.Count > 0)
        {
            GameObject item = queue.Dequeue();
            if (item == null) continue;
            item.transform.SetParent(null, true);
            item.transform.SetPositionAndRotation(position, rotation);
            item.SetActive(true);
            return item;
        }
        return null;
    }

    public void ReturnToPool(GameObject item)
    {
        if (item == null || !poolByObject.TryGetValue(item, out string tag)) return;
        item.SetActive(false);
        item.transform.SetParent(transform, false);
        available[tag].Enqueue(item);
    }
}
