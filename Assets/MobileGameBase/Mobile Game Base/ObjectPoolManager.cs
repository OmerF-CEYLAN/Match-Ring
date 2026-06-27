// ═══════════════════════════════════════════════════════
//  ObjectPoolManager.cs  —  REUSABLE. NEVER MODIFY.
//  Configure pools in the Inspector. Spawn and recycle
//  objects with zero runtime allocation.
// ═══════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private EventBinding<GameOverEvent> gameOverBinding;

    [Header("Pools")]
    [SerializeField] private PoolConfig[] pools;

    [Header("Integration")]
    [Tooltip("Automatically returns all active objects to their pools on Game Over.")]
    [SerializeField] private bool returnAllOnGameOver = true;


    private Dictionary<string, Stack<GameObject>>  available  = new();
    private Dictionary<string, HashSet<GameObject>> active    = new();
    private Dictionary<string, PoolConfig>          configs   = new();
    private Dictionary<string, Transform>           containers = new();

    // ───────────────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitialisePools();
    }

    private void OnEnable()
    {
        if (returnAllOnGameOver)
        {
            gameOverBinding = new EventBinding<GameOverEvent>(HandleGameOver);
            EventBus<GameOverEvent>.Subscribe(gameOverBinding);
        }
    }

    private void OnDisable()
    {
        if (returnAllOnGameOver)
            EventBus<GameOverEvent>.Unsubscribe(gameOverBinding);
    }

    private void HandleGameOver(GameOverEvent e) => ReturnAll();

    // ───────────────────────────────────────────────────
    //  Public API — Get
    // ───────────────────────────────────────────────────

    /// <summary>Retrieve an object from the pool, inactive and ready to configure.</summary>
    public GameObject Get(string key)
    {
        if (!IsValidKey(key)) return null;

        if (available[key].Count == 0)
            Expand(key);

        GameObject obj = available[key].Pop();
        active[key].Add(obj);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>Retrieve, position, and activate in one call.</summary>
    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        GameObject obj = Get(key);
        if (obj == null) return null;
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    /// <summary>Retrieve and cast to a component directly.</summary>
    public T Get<T>(string key) where T : Component
    {
        GameObject obj = Get(key);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    public T Get<T>(string key, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject obj = Get(key, position, rotation);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    // ───────────────────────────────────────────────────
    //  Public API — Return
    // ───────────────────────────────────────────────────

    /// <summary>Return an object back to its pool.</summary>
    public void Return(string key, GameObject obj)
    {
        if (!IsValidKey(key) || obj == null) return;

        active[key].Remove(obj);
        obj.SetActive(false);
        obj.transform.SetParent(containers[key]);
        available[key].Push(obj);
    }

    /// <summary>Return via the PooledObject component — no key needed.</summary>
    public void Return(PooledObject pooled)
    {
        if (pooled == null) return;
        Return(pooled.PoolKey, pooled.gameObject);
    }

    /// <summary>Return all active objects from a specific pool.</summary>
    public void ReturnAll(string key)
    {
        if (!IsValidKey(key)) return;

        // Copy to avoid modifying the set while iterating
        var toReturn = new List<GameObject>(active[key]);
        foreach (var obj in toReturn)
            Return(key, obj);
    }

    /// <summary>Return ALL active objects from every pool. Called automatically on Game Over.</summary>
    public void ReturnAll()
    {
        foreach (var key in active.Keys)
            ReturnAll(key);
    }

    /// <summary>Return an object after a delay. Useful for death effects, explosions.</summary>
    public void ReturnDelayed(string key, GameObject obj, float delay)
    {
        StartCoroutine(ReturnAfterDelay(key, obj, delay));
    }

    // ───────────────────────────────────────────────────
    //  Public API — Runtime Management
    // ───────────────────────────────────────────────────

    /// <summary>Add more objects to an existing pool at runtime.</summary>
    public void PreWarm(string key, int count)
    {
        if (!IsValidKey(key)) return;
        for (int i = 0; i < count; i++)
            CreateObject(configs[key], key);
    }

    /// <summary>How many objects are currently available in a pool.</summary>
    public int AvailableCount(string key) =>
        IsValidKey(key) ? available[key].Count : 0;

    /// <summary>How many objects are currently active from a pool.</summary>
    public int ActiveCount(string key) =>
        IsValidKey(key) ? active[key].Count : 0;

    // ───────────────────────────────────────────────────
    //  Internal
    // ───────────────────────────────────────────────────
    private void InitialisePools()
    {
        foreach (var config in pools)
        {
            if (config.prefab == null || string.IsNullOrEmpty(config.key))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool config is missing a prefab or key.");
                continue;
            }

            if (available.ContainsKey(config.key))
            {
                Debug.LogWarning($"[ObjectPoolManager] Duplicate pool key: '{config.key}'. Skipping.");
                continue;
            }

            available[config.key]   = new Stack<GameObject>();
            active[config.key]      = new HashSet<GameObject>();
            configs[config.key]     = config;

            // Create a container GameObject to keep the hierarchy tidy
            var container = new GameObject($"Pool_{config.key}");
            container.transform.SetParent(transform);
            containers[config.key] = container.transform;

            // Pre-warm
            int count = Mathf.Max(1, config.initialSize);
            for (int i = 0; i < count; i++)
                CreateObject(config, config.key);
        }
    }

    private void CreateObject(PoolConfig config, string key)
    {
        GameObject obj = Instantiate(config.prefab, containers[key]);
        obj.SetActive(false);

        // Attach the PooledObject component so any script can self-return
        var pooled = obj.GetComponent<PooledObject>() ?? obj.AddComponent<PooledObject>();
        pooled.PoolKey = key;

        available[key].Push(obj);
    }

    private void Expand(string key)
    {
        PoolConfig config = configs[key];
        if (!config.autoExpand)
        {
            Debug.LogWarning($"[ObjectPoolManager] Pool '{key}' is empty and autoExpand is off.");
            return;
        }

        int expandBy = Mathf.Max(1, configs[key].initialSize / 2);
        Debug.Log($"[ObjectPoolManager] Expanding pool '{key}' by {expandBy}.");
        for (int i = 0; i < expandBy; i++)
            CreateObject(config, key);
    }

    private bool IsValidKey(string key)
    {
        if (available.ContainsKey(key)) return true;
        Debug.LogWarning($"[ObjectPoolManager] No pool found with key '{key}'.");
        return false;
    }

    private IEnumerator ReturnAfterDelay(string key, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null && obj.activeInHierarchy)
            Return(key, obj);
    }
}

// ═══════════════════════════════════════════════════════
//  PoolConfig — Configured per pool in the Inspector
// ═══════════════════════════════════════════════════════

[System.Serializable]
public class PoolConfig
{
    [Tooltip("Unique name for this pool. Used to Get() and Return() objects.")]
    public string key;

    [Tooltip("The prefab to pool.")]
    public GameObject prefab;

    [Tooltip("How many objects to create on startup.")]
    public int initialSize = 10;

    [Tooltip("If empty, create more objects automatically. Disable to catch sizing issues.")]
    public bool autoExpand = true;
}

// ═══════════════════════════════════════════════════════
//  PooledObject — Auto-attached to every pooled object.
//  Call ReturnToPool() from any script on the object.
// ═══════════════════════════════════════════════════════

public class PooledObject : MonoBehaviour
{
    public string PoolKey { get; set; }

    /// <summary>Return this object to its pool from any script.</summary>
    public void ReturnToPool()
    {
        ObjectPoolManager.Instance?.Return(PoolKey, gameObject);
    }

    /// <summary>Return after a delay — great for particles and death effects.</summary>
    public void ReturnToPool(float delay)
    {
        ObjectPoolManager.Instance?.ReturnDelayed(PoolKey, gameObject, delay);
    }
}

// ═══════════════════════════════════════════════════════
//  HOW TO USE
//
//  SETUP: Put ObjectPoolManager on a scene GameObject.
//  In Inspector, add pools: set key, prefab, size.
//
//  SPAWN:
//      var obj = ObjectPoolManager.Instance.Get("Obstacle", spawnPos, Quaternion.identity);
//      var rb  = ObjectPoolManager.Instance.Get<Rigidbody>("Coin", spawnPos, Quaternion.identity);
//
//  RETURN (from a gameplay script on the object):
//      GetComponent<PooledObject>().ReturnToPool();
//      GetComponent<PooledObject>().ReturnToPool(2f);  // after 2 seconds
//
//  RETURN (from an external script):
//      ObjectPoolManager.Instance.Return("Obstacle", obstacleObj);
//      ObjectPoolManager.Instance.ReturnAll("Obstacle");
//      ObjectPoolManager.Instance.ReturnAll();   // called automatically on Game Over
// ═══════════════════════════════════════════════════════
