using UnityEngine;
using TMPro;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("UI 引用")]
    public TextMeshProUGUI comboText; // 屏幕上的Combo文字

    [Header("连击设置")]
    public float comboTimeout = 1.5f; // 多久没有击杀则重置连击
    private int comboCount = 0;
    private float comboTimer = 0f;

    // 用于 UI 弹跳动画的变量
    private Vector3 originalTextScale = Vector3.one;
    private float currentScaleMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (comboText != null)
        {
            originalTextScale = comboText.transform.localScale;
            comboText.text = ""; // 初始清空
        }
    }

    private void Update()
    {
        // 1. 处理连击超时重置
        if (comboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
        }

        // 2. 处理 Combo UI 的弹性缩放动画 (Punch Scale)
        if (comboText != null && currentScaleMultiplier > 1f)
        {
            currentScaleMultiplier = Mathf.Lerp(currentScaleMultiplier, 1f, Time.deltaTime * 10f);
            comboText.transform.localScale = originalTextScale * currentScaleMultiplier;
        }
    }

    // 每次击杀敌人时调用
    public void AddCombo(Vector3 enemyPos)
    {
        comboCount++;
        comboTimer = comboTimeout;

        // 1. 飘字表现
        string text = comboCount > 1 ? $"Combo x{comboCount}!" : "Kill!";
        Color color = comboCount >= 10 ? Color.red : Color.yellow;

        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowText(text, color, enemyPos);
        }

        // 2. UI 显示与缩放动画
        if (comboText != null)
        {
            comboText.text = comboCount > 1 ? $"x{comboCount} Combo" : "";
            // 触发缩放跳动
            currentScaleMultiplier = 1.4f;
            comboText.transform.localScale = originalTextScale * currentScaleMultiplier;
        }

        // 3. 播放连击音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.Combo);
        }

        // 4. 里程碑特效 (每10连击爆发一次光效)
        if (comboCount % 10 == 0)
        {
            if (VFXManager.Instance != null && PlayerController.Instance != null)
            {
                // 在玩家脚下生成一个连击爆发特效
                VFXManager.Instance.PlayVFX(VFXType.ComboBurst, PlayerController.Instance.transform.position, Quaternion.identity);
            }
        }
    }

    // 重置连击
    private void ResetCombo()
    {
        comboCount = 0;
        comboTimer = 0f;

        if (comboText != null)
        {
            comboText.text = "";
        }
    }
}