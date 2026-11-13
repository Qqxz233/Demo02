using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // 需要引用 AI 命名空间

public class LevelGeneratorMC : MonoBehaviour
{
    public static LevelGeneratorMC Instance { get; private set; }

    [Header("生成数量配置")]
    public int minWalls = 5;
    public int maxWalls = 8;
    public int minTraps = 8;
    public int maxTraps = 15;

    [Header("预设坐标锚点 (在场景中拖入空物体作为生成点)")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("预制体列表")]
    public GameObject[] wallPrefabs;
    public GameObject[] trapPrefabs;

    [Header("可选：动态重新烘焙导航网格")]
    [Tooltip("如果你安装了 AI Navigation 插件并挂载了 NavMeshSurface，请拖到这里。如果没有，保持为空。")]
    public MonoBehaviour navMeshSurfaceComponent; // 使用 MonoBehaviour 弱引用，避免没安装插件时报错

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator GenerateLevelAsync(System.Action<float> onProgress)
    {
        // 检查配置是否为空
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("<color=red>【生成失败】没有配置预设坐标锚点！</color>");
            onProgress?.Invoke(1f);
            yield break;
        }

        if (wallPrefabs == null || wallPrefabs.Length == 0)
        {
            Debug.LogWarning("<color=orange>【警告】没有配置墙体预制体！</color>");
        }

        // 过滤掉列表中为空的锚点
        List<Transform> validPoints = new List<Transform>();
        foreach (var pt in spawnPoints)
        {
            if (pt != null) validPoints.Add(pt);
        }

        if (validPoints.Count == 0)
        {
            Debug.LogError("<color=red>【生成失败】锚点列表中全是空物体（None）！</color>");
            onProgress?.Invoke(1f);
            yield break;
        }

        // 打乱锚点列表
        List<Transform> availablePoints = new List<Transform>(validPoints);
        for (int i = 0; i < availablePoints.Count; i++)
        {
            Transform temp = availablePoints[i];
            int randomIndex = UnityEngine.Random.Range(i, availablePoints.Count);
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        int wallCount = UnityEngine.Random.Range(minWalls, maxWalls + 1);
        int trapCount = UnityEngine.Random.Range(minTraps, maxTraps + 1);

        int totalNeeded = wallCount + trapCount;
        if (totalNeeded > availablePoints.Count)
        {
            if (wallCount > availablePoints.Count)
            {
                wallCount = availablePoints.Count;
                trapCount = 0;
            }
            else
            {
                trapCount = availablePoints.Count - wallCount;
            }
        }

        int generated = 0;
        int pointIndex = 0;

        // 1. 生成墙体
        if (wallPrefabs != null && wallPrefabs.Length > 0)
        {
            for (int i = 0; i < wallCount; i++)
            {
                if (pointIndex >= availablePoints.Count) break;

                GameObject prefab = wallPrefabs[UnityEngine.Random.Range(0, wallPrefabs.Length)];
                if (prefab != null)
                {
                    SpawnWallAtPoint(prefab, availablePoints[pointIndex]);
                    generated++;
                }
                pointIndex++;
                onProgress?.Invoke(totalNeeded > 0 ? (float)generated / totalNeeded : 1f);
                yield return null;
            }
        }

        // 2. 生成陷阱/道具
        if (trapPrefabs != null && trapPrefabs.Length > 0)
        {
            for (int i = 0; i < trapCount; i++)
            {
                if (pointIndex >= availablePoints.Count) break;

                GameObject prefab = trapPrefabs[UnityEngine.Random.Range(0, trapPrefabs.Length)];
                if (prefab != null)
                {
                    SpawnTrapAtPoint(prefab, availablePoints[pointIndex]);
                    generated++;
                }
                pointIndex++;
                onProgress?.Invoke(totalNeeded > 0 ? (float)generated / totalNeeded : 1f);
                yield return null;
            }
        }

        // 强制物理引擎同步
        Physics.SyncTransforms();

        // 【可选修复】：如果使用了 NavMeshSurface 插件，生成完地图后强制重新烘焙一次
        if (navMeshSurfaceComponent != null)
        {
            var buildMethod = navMeshSurfaceComponent.GetType().GetMethod("BuildNavMesh");
            if (buildMethod != null) buildMethod.Invoke(navMeshSurfaceComponent, null);
        }

        onProgress?.Invoke(1f);
        Debug.Log($"<color=green>地图生成完毕！墙体:{wallCount} 个, 陷阱/道具:{trapCount} 个。</color>");
    }

    // ==========================================
    // 【核心修复】：墙体生成与物理设置
    // ==========================================
    private void SpawnWallAtPoint(GameObject prefab, Transform point)
    {
        Vector3 spawnPos = point.position;
        Quaternion spawnRot = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

        GameObject obj = Instantiate(prefab, spawnPos, spawnRot);

        // 1. 【修复子弹穿墙】：强制将所有子物体设为 "Wall" 层
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer != -1) // 确保工程里有 Wall 层
        {
            obj.layer = wallLayer;
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = wallLayer;
            }
        }
        else
        {
            Debug.LogError("<color=red>你的工程里没有名为 'Wall' 的 Layer！请去右上角 Layers 添加！</color>");
        }

        // 2. 【修复敌人寻路穿墙】：配置高精度的动态寻路障碍物
        NavMeshObstacle obstacle = obj.GetComponent<NavMeshObstacle>();
        if (obstacle == null) obstacle = obj.AddComponent<NavMeshObstacle>();

        obstacle.shape = NavMeshObstacleShape.Box;
        // 注意：这里的数值 (1.5, 2, 3, 2) 需要根据你实际墙体的尺寸微调！
        // Center: 中心点相对物体原点的偏移（通常向上抬升一半的高度）
        obstacle.center = new Vector3(0, 1.5f, 0);
        // Size: 障碍物的长宽高，一定要比视觉模型稍大一点点，防止敌人卡缝
        obstacle.size = new Vector3(2f, 3f, 2f);

        obstacle.carving = true; // 开启实时切削
        obstacle.carveOnlyStationary = true; // 只有静止时才切削（墙是静止的，性能更好）
    }

    // ==========================================
    // 陷阱/道具生成
    // ==========================================
    private void SpawnTrapAtPoint(GameObject prefab, Transform point)
    {
        Vector3 spawnPos = point.position;
        // 陷阱通常不需要随机旋转（如果是地刺），但可以保留一点随机
        Quaternion spawnRot = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

        Instantiate(prefab, spawnPos, spawnRot);
    }
}