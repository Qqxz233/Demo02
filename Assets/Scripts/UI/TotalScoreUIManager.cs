using UnityEngine;
using TMPro;

public class TotalScoreUIManager : MonoBehaviour
{
    public static TotalScoreUIManager Instance { get; private set; }

    public TextMeshProUGUI scoreText;

    private float accumulatedDamage = 0f;
    private float previousDamage = 0f;

    // UI 膨胀动画控制
    private float currentScaleMultiplier = 1f;

    // UI 刷新节流 (防止高频伤害导致 UI 重建卡顿)
    private float uiUpdateTimer = 0f;
    private const float UI_UPDATE_INTERVAL = 0.1f;
    private bool isUIDirty = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 【核心修改】：开局强制隐藏面板，直到敌人出现并受到伤害再显示
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 1. 平滑恢复 UI 膨胀动画
        if (currentScaleMultiplier > 1f)
        {
            currentScaleMultiplier = Mathf.Lerp(currentScaleMultiplier, 1f, Time.unscaledDeltaTime * 8f);
            if (scoreText != null)
                scoreText.transform.localScale = Vector3.one * currentScaleMultiplier;
        }

        // 2. 定时刷新 UI 文本内容
        if (isUIDirty)
        {
            uiUpdateTimer += Time.unscaledDeltaTime;
            if (uiUpdateTimer >= UI_UPDATE_INTERVAL)
            {
                UpdateUI();
                isUIDirty = false;
                uiUpdateTimer = 0f;
            }
        }
    }

    public float GetTotalScore()
    {
        return accumulatedDamage;
    }

    public void AddDamage(float damage)
    {
        previousDamage = accumulatedDamage;
        accumulatedDamage += damage;

        isUIDirty = true; // 打上脏标记，等待刷新

        // 【核心新增】：一旦有伤害发生，立刻唤醒 UI 面板
        if (scoreText != null && !scoreText.gameObject.activeSelf)
        {
            scoreText.gameObject.SetActive(true);
        }

        CheckMilestone();
        TriggerPunchScale(damage);
    }

    private void TriggerPunchScale(float damage)
    {
        // 根据单次伤害决定膨胀幅度，设上限防止过大 (最大放大 1.25 倍)
        float punchAmount = Mathf.Clamp(damage * 0.005f, 0.05f, 0.25f);
        float newScale = 1f + punchAmount;
        currentScaleMultiplier = Mathf.Max(currentScaleMultiplier, newScale);
    }

    private void UpdateUI()
    {
        if (scoreText == null) return;

        // 向上取整显示，不留小数
        string text = Mathf.CeilToInt(accumulatedDamage).ToString();
        int exclamationCount = 0;

        // 分数突破变色与加叹号逻辑
        if (accumulatedDamage >= 1000)
        {
            scoreText.color = Color.red;
            exclamationCount = 3;
        }
        else if (accumulatedDamage >= 500)
        {
            scoreText.color = new Color(1f, 0.6f, 0f); // 橙色
            exclamationCount = 2;
        }
        else if (accumulatedDamage >= 100)
        {
            scoreText.color = Color.yellow;
            exclamationCount = 1;
        }
        else
        {
            scoreText.color = Color.white;
        }

        for (int i = 0; i < exclamationCount; i++) text += "!";

        scoreText.text = text;
    }

    private void CheckMilestone()
    {
        // 只有当分数刚好突破这几个阈值时，才播放特效和音效（防止连续触发）
        if (previousDamage < 100 && accumulatedDamage >= 100)
            PlayMilestoneEffect();
        else if (previousDamage < 500 && accumulatedDamage >= 500)
            PlayMilestoneEffect();
        else if (previousDamage < 1000 && accumulatedDamage >= 1000)
            PlayMilestoneEffect();
        else if (previousDamage < 2000 && accumulatedDamage >= 2000)
            PlayMilestoneEffect();
    }

    private void PlayMilestoneEffect()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SoundType.ScoreMilestone);

        if (VFXManager.Instance != null && PlayerController.Instance != null)
            VFXManager.Instance.PlayVFX(VFXType.ScoreMilestone, PlayerController.Instance.transform.position, Quaternion.identity);
    }
}