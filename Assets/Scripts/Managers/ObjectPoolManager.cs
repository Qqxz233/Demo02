using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

// 【新增】：用于在实例上记录源头预制体的小组件
public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject originPrefab;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // 移除 instanceToPrefabMap，节省内存并提升回收速度
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 获取或创建池子，并预先分配容量
    private Queue<GameObject> GetOrCreatePool(GameObject prefab, int capacity = 10)
    {
        if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            // 预设容量，避免后续 Enqueue 时发生数组扩容
            pool = new Queue<GameObject>(capacity);
            poolDictionary[prefab] = pool;
        }
        return pool;
    }

    // 同步预热
    public void Prewarm(GameObject prefab, int count)
    {
        Queue<GameObject> pool = GetOrCreatePool(prefab, count);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.AddComponent<PooledObject>().originPrefab = prefab; // 打上标记
            pool.Enqueue(obj);
        }
    }

    // 异步分帧预热
    public IEnumerator PrewarmAsync(GameObject prefab, int targetCount, Action<float> onProgress)
    {
        Queue<GameObject> pool = GetOrCreatePool(prefab, targetCount);

        int currentCount = pool.Count;
        int neededCount = targetCount - currentCount;

        if (neededCount <= 0)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        for (int i = 0; i < neededCount; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.AddComponent<PooledObject>().originPrefab = prefab;
            pool.Enqueue(obj);

            if (i % 5 == 0)
            {
                onProgress?.Invoke((float)i / neededCount);
                yield return null;
            }
        }
        onProgress?.Invoke(1f);
    }

    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> pool = GetOrCreatePool(prefab);

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        }
        else
        {
            // 池子空了，动态生成并打上标记
            obj = Instantiate(prefab, position, rotation);
            obj.AddComponent<PooledObject>().originPrefab = prefab;
        }
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);

        // 【核心优化】：直接从组件读取源头 Prefab，避免字典查找
        PooledObject pooledScript = obj.GetComponent<PooledObject>();

        if (pooledScript != null && pooledScript.originPrefab != null)
        {
            Queue<GameObject> pool = GetOrCreatePool(pooledScript.originPrefab);
            pool.Enqueue(obj);
        }
        else
        {
            // 如果没有标记（安全兜底），直接销毁
            Destroy(obj);
        }
    }
}