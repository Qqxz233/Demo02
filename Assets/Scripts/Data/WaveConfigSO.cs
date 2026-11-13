using System.Collections.Generic;
using UnityEngine;

// 单个批次的数据
[System.Serializable]
public class SpawnBatch
{
    [Header("怪物数量配置")]
    public int normalEnemyCount = 0;
    public int rangedEnemyCount = 0;
    public int bombEnemyCount = 0;

    [Tooltip("Boss数量，大于0则触发Boss警告并生成Boss")]
    public int bossCount = 0; 

    [Header("时间配置")]
    [Tooltip("距离上一个批次结束后的等待时间(秒)")]
    public float delayBeforeBatch = 0f;
    [Tooltip("该批次内，每个怪物生成的间隔(秒)")]
    public float intervalBetweenSpawns = 0.5f;
}

// 单个波次的数据（包含多个批次）
[System.Serializable]
public class WaveData
{
    [Tooltip("该波次包含的所有批次")]
    public List<SpawnBatch> batches = new List<SpawnBatch>();
}

// 关卡总配置
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class WaveConfigSO : ScriptableObject
{
    public List<WaveData> waves = new List<WaveData>();
}