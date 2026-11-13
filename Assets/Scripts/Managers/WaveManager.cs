using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("波次配置")]
    public WaveConfigSO levelConfig;

    [Header("刷怪半径 (基于玩家)")]
    public float spawnRadiusMin = 20f;
    public float spawnRadiusMax = 35f;

    [Header("地图绝对边界 (世界坐标限制)")]
    public float minWorldX = -35f;
    public float maxWorldX = 35f;
    public float minWorldZ = -35f;
    public float maxWorldZ = 35f;

    [Header("敌人预制体")]
    public GameObject normalEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject bombEnemyPrefab;
    public GameObject bossEnemyPrefab;

    [Header("Boss专属设置")]
    public float bossWarningDelay = 3f;

    [Header("波次动态难度倍率")]
    public float perWaveStatMultiplier = 0.1f;

    public int CurrentWaveIndex { get; private set; } = 0;
    public int TotalWaves => levelConfig != null ? levelConfig.waves.Count : 0;

    private Transform playerTransform;
    private bool isWaitingForUpgrade = false;
    private int wallLayerMask;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        wallLayerMask = LayerMask.GetMask("Wall");
    }

    public void OnLoadingComplete()
    {
        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform;
        else
        {
            Debug.LogError("<color=red>找不到 PlayerController.Instance！</color>");
            return;
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeFinished -= OnPlayerFinishedUpgrade;
            UpgradeManager.Instance.OnUpgradeFinished += OnPlayerFinishedUpgrade;
        }

        StartCoroutine(WaveLifecycleRoutine());
    }

    private void OnPlayerFinishedUpgrade()
    {
        isWaitingForUpgrade = false;
    }

    private IEnumerator WaveLifecycleRoutine()
    {
        yield return new WaitForSeconds(1f);

        while (CurrentWaveIndex < TotalWaves)
        {
            CurrentWaveIndex++;

            if (WaveUIManager.Instance != null)
                WaveUIManager.Instance.ShowWaveText(CurrentWaveIndex);

            WaveData currentWave = levelConfig.waves[CurrentWaveIndex - 1];

            int waveTotalCount = 0;
            foreach (var batch in currentWave.batches)
                waveTotalCount += batch.normalEnemyCount + batch.rangedEnemyCount + batch.bombEnemyCount + batch.bossCount;

            if (WaveUIManager.Instance != null)
                WaveUIManager.Instance.InitProgressBar(waveTotalCount);

            yield return StartCoroutine(SpawnAllBatchesRoutine(currentWave));

            while (EnemyManager.Instance.ActiveEnemies.Count > 0)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            if (CurrentWaveIndex >= TotalWaves)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Victory);
                yield break;
            }
            else
            {
                isWaitingForUpgrade = true;
                if (UpgradeManager.Instance != null)
                    UpgradeManager.Instance.TriggerUpgrade();

                yield return new WaitUntil(() => !isWaitingForUpgrade);
            }
        }
    }

    private IEnumerator SpawnAllBatchesRoutine(WaveData wave)
    {
        for (int b = 0; b < wave.batches.Count; b++)
        {
            SpawnBatch batch = wave.batches[b];

            if (batch.delayBeforeBatch > 0f)
                yield return new WaitForSeconds(batch.delayBeforeBatch);

            if (batch.bossCount > 0)
            {
                if (BossWarningUI.Instance != null)
                    yield return StartCoroutine(BossWarningUI.Instance.ShowWarning(bossWarningDelay));
                else
                    yield return new WaitForSeconds(bossWarningDelay);

                if (AudioManager.Instance != null)
                    AudioManager.Instance.SwitchBGM(SoundType.BossBGM, 1f);

                for (int i = 0; i < batch.bossCount; i++)
                {
                    SpawnBoss();
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                List<GameObject> spawnQueue = new List<GameObject>();
                for (int i = 0; i < batch.normalEnemyCount; i++) spawnQueue.Add(normalEnemyPrefab);
                for (int i = 0; i < batch.rangedEnemyCount; i++) spawnQueue.Add(rangedEnemyPrefab);
                for (int i = 0; i < batch.bombEnemyCount; i++) spawnQueue.Add(bombEnemyPrefab);

                for (int i = 0; i < spawnQueue.Count; i++)
                {
                    GameObject temp = spawnQueue[i];
                    int randomIndex = UnityEngine.Random.Range(i, spawnQueue.Count);
                    spawnQueue[i] = spawnQueue[randomIndex];
                    spawnQueue[randomIndex] = temp;
                }

                foreach (GameObject prefab in spawnQueue)
                {
                    if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) yield break;

                    if (prefab != null) SpawnEnemy(prefab);
                    yield return new WaitForSeconds(batch.intervalBetweenSpawns);
                }
            }
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GetValidSpawnPosition();
        if (spawnPos != Vector3.zero)
        {
            // 【核心修复】：强制将生成点 Y 轴设为 0.5f（或你的地面高度）
            // 防止由于采样导致生成到地下
            spawnPos.y = 0.5f;

            GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            EnemyBase eb = enemyObj.GetComponent<EnemyBase>();
            if (eb != null)
            {
                float currentMultiplier = 1f + (CurrentWaveIndex - 1) * perWaveStatMultiplier;
                eb.waveHealthMultiplier = currentMultiplier;
                eb.waveDamageMultiplier = currentMultiplier;

                EnemyManager.Instance.RegisterEnemy(enemyObj.transform);
            }
        }
    }

    private void SpawnBoss()
    {
        if (bossEnemyPrefab == null) return;
        Vector3 spawnPos = GetValidSpawnPosition();
        if (spawnPos != Vector3.zero)
        {
            spawnPos.y = 0.5f; // 强制贴地

            GameObject bossObj = Instantiate(bossEnemyPrefab, spawnPos, Quaternion.identity);
            EnemyBase eb = bossObj.GetComponent<EnemyBase>();
            if (eb != null)
            {
                float currentMultiplier = 1f + (CurrentWaveIndex - 1) * perWaveStatMultiplier;
                eb.waveHealthMultiplier = currentMultiplier;
                eb.waveDamageMultiplier = currentMultiplier;

                EnemyManager.Instance.RegisterEnemy(bossObj.transform);
            }
        }
    }

    // ==========================================================
    // 【核心修复】：重构生成位置逻辑，严防贴脸和穿地
    // ==========================================================
    private Vector3 GetValidSpawnPosition()
    {
        int maxAttempts = 30;
        float minSafeSqrDist = spawnRadiusMin * spawnRadiusMin; // 最小安全距离的平方

        for (int i = 0; i < maxAttempts; i++)
        {
            // 1. 基于玩家周围随机环形计算
            float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomDistance = UnityEngine.Random.Range(spawnRadiusMin, spawnRadiusMax);
            float spawnX = playerTransform.position.x + Mathf.Cos(randomAngle) * randomDistance;
            float spawnZ = playerTransform.position.z + Mathf.Sin(randomAngle) * randomDistance;

            // 2. 限制在地图绝对边界内
            spawnX = Mathf.Clamp(spawnX, minWorldX, maxWorldX);
            spawnZ = Mathf.Clamp(spawnZ, minWorldZ, maxWorldZ);

            Vector3 targetPos = new Vector3(spawnX, 0f, spawnZ);

            // 【核心修复】：计算与玩家的距离。如果因为被 Clamp 强行拉扯导致距离玩家过近，直接放弃本次
            float sqrDistToPlayer = (targetPos - playerTransform.position).sqrMagnitude;
            if (sqrDistToPlayer < minSafeSqrDist)
                continue; // 距离太近，重选

            // 3. 检测是否在墙体内
            if (Physics.CheckSphere(targetPos, 1.5f, wallLayerMask))
                continue;

            // 4. NavMesh 采样确定能走
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // 【保底逻辑修正】：如果找了 30 次都没找到，绝对不要在玩家身边兜底。
        // 而是把玩家往外推一个最大刷怪半径的距离，再找一次。
        Debug.LogWarning("<color=orange>【刷怪警告】未找到有效点，尝试向外围强制偏移！</color>");

        Vector3 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 fallbackPos = playerTransform.position + new Vector3(randomDir.x, 0, randomDir.y) * spawnRadiusMax;
        fallbackPos.x = Mathf.Clamp(fallbackPos.x, minWorldX, maxWorldX);
        fallbackPos.z = Mathf.Clamp(fallbackPos.z, minWorldZ, maxWorldZ);
        fallbackPos.y = 0.5f;

        // 兜底采样，放宽搜索范围到 10 米
        if (NavMesh.SamplePosition(fallbackPos, out NavMeshHit fallbackHit, 10f, NavMesh.AllAreas))
        {
            return fallbackHit.position;
        }

        return fallbackPos;
    }

    public void OnEnemyKilled()
    {
        if (WaveUIManager.Instance != null)
            WaveUIManager.Instance.AddKillProgress();
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeFinished -= OnPlayerFinishedUpgrade;
    }
}