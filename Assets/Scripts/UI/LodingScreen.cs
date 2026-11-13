using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("UI引用")]
    [Tooltip("拖入你要隐藏的加载面板UI，千万不要拖入挂载本脚本的物体自己！")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public TextMeshProUGUI loadingText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 【防呆机制】：防止把自身拖进槽位导致自杀
        if (loadingPanel != null)
        {
            if (loadingPanel == gameObject)
            {
                Debug.LogError("<color=red>【严重错误】你把挂载了 LoadingScreen 的物体拖到了 Loading Panel 槽位里！这会导致游戏卡死！请重新拖入正确的子面板！</color>");
            }
            else
            {
                loadingPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("<color=red>【致命错误】Loading Panel 槽位是空的！</color>");
        }
    }

    private void Start()
    {
        // 检查状态
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Loading)
        {
            if (WaveManager.Instance != null && WaveManager.Instance.levelConfig != null)
            {
                StartLoadingAsync(
                    WaveManager.Instance.levelConfig,
                    WaveManager.Instance.normalEnemyPrefab,
                    WaveManager.Instance.rangedEnemyPrefab,
                    WaveManager.Instance.bombEnemyPrefab
                );
            }
            else
            {
                Debug.LogError("<color=red>【加载失败】找不到 WaveManager 或未配置 Level Config！</color>");
                if (GameManager.Instance != null) GameManager.Instance.OnLoadingComplete();
            }
        }
    }

    public void StartLoadingAsync(WaveConfigSO config, GameObject normalPrefab, GameObject rangedPrefab, GameObject bombPrefab)
    {
        if (loadingPanel != null && loadingPanel != gameObject)
            loadingPanel.SetActive(true);

        StartCoroutine(LoadingRoutine(config, normalPrefab, rangedPrefab, bombPrefab));
    }

    private IEnumerator LoadingRoutine(WaveConfigSO config, GameObject normal, GameObject ranged, GameObject bomb)
    {
        Time.timeScale = 0f;
        if (progressBar != null) progressBar.value = 0f;
        if (loadingText != null) loadingText.text = "Loding...";

        // 1. 计算总数并预热对象池
        int totalNormal = 0, totalRanged = 0, totalBomb = 0;
        if (config != null && config.waves != null)
        {
            foreach (var wave in config.waves)
            {
                foreach (var batch in wave.batches)
                {
                    totalNormal += batch.normalEnemyCount;
                    totalRanged += batch.rangedEnemyCount;
                    totalBomb += batch.bombEnemyCount + batch.bossCount;
                }
            }
        }

        if (normal != null && totalNormal > 0)
            yield return StartCoroutine(ObjectPoolManager.Instance.PrewarmAsync(normal, Mathf.CeilToInt(totalNormal * 1.5f), (p) => { if (progressBar != null) progressBar.value = p * 0.15f; }));

        if (ranged != null && totalRanged > 0)
            yield return StartCoroutine(ObjectPoolManager.Instance.PrewarmAsync(ranged, Mathf.CeilToInt(totalRanged * 1.5f), (p) => { if (progressBar != null) progressBar.value = 0.15f + (p * 0.15f); }));

        if (bomb != null && totalBomb > 0)
            yield return StartCoroutine(ObjectPoolManager.Instance.PrewarmAsync(bomb, Mathf.CeilToInt(totalBomb * 1.5f), (p) => { if (progressBar != null) progressBar.value = 0.30f + (p * 0.20f); }));

        // 2. 【核心修复】：加入地形生成与陷阱生成（上次代码精简时遗漏了）
        if (loadingText != null) loadingText.text = "正在生成地形与机关...";
        if (LevelGeneratorMC.Instance != null)
        {
            yield return StartCoroutine(LevelGeneratorMC.Instance.GenerateLevelAsync((p) => {
                if (progressBar != null) progressBar.value = 0.50f + (p * 0.40f);
            }));
        }
        else
        {
            Debug.LogWarning("场景中没有挂载 LevelGeneratorMC，跳过地形生成！");
        }

        // 3. 收尾
        if (loadingText != null) loadingText.text = "加载完成，准备进入...";
        yield return new WaitForSecondsRealtime(0.3f);
        if (progressBar != null) progressBar.value = 1f;
        yield return new WaitForSecondsRealtime(0.5f);

        // 4. 关闭加载界面
        if (loadingPanel != null && loadingPanel != gameObject)
            loadingPanel.SetActive(false);

        // 5. 通知 GameManager 进入主菜单
        if (GameManager.Instance != null)
            GameManager.Instance.OnLoadingComplete();

        // 6. 关闭加载界面
        if (loadingPanel != null) loadingPanel.SetActive(false);

        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.SpawnPlayer();
        }

        // 7. 通知 GameManager 进入主菜单（此时相机会寻找刚刚生成的玩家）
        if (GameManager.Instance != null)
            GameManager.Instance.OnLoadingComplete();
    }
}