using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthSliderUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Slider actualSlider;    // 实际血量 Slider
    public Slider bufferSlider;    // 缓冲血量 Slider
    public Image actualFillImage;  // 实际血量的 Fill 图片 (用于改颜色和闪烁)
    public RectTransform uiRect;   // 用于做滑入动画

    [Header("动画设置")]
    public float slideInDuration = 0.5f;
    public float bufferSpeed = 0.5f; // 缓冲条下降速度
    public float finalYPosition = 150f;
    public float startYOffset = -200f;

    private PlayerHealth playerHealth;
    private float targetHpPercent = 1f;
    private bool isInitialized = false;

    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        // 初始位置在屏幕下方（隐藏状态）
        uiRect.anchoredPosition = new Vector2(uiRect.anchoredPosition.x, finalYPosition + startYOffset);
        if (actualSlider != null) actualSlider.value = 1f;
        if (bufferSlider != null) bufferSlider.value = 1f;

        // 移除自动滑入逻辑，等待指令
        isInitialized = false;
    }

    public void ShowUI()
    {
        isInitialized = true;
    }

    private void StartSlideIn()
    {
        isInitialized = true;
    }

    private void Update()
    {
        if (playerHealth == null) return;

        // 1. 滑入动画
        if (isInitialized)
        {
            Vector2 targetPos = new Vector2(uiRect.anchoredPosition.x, finalYPosition);
            uiRect.anchoredPosition = Vector2.Lerp(uiRect.anchoredPosition, targetPos, Time.deltaTime * 10f);
        }

        // 2. 获取当前血量百分比
        targetHpPercent = playerHealth.GetHealthPercent();

        // 3. 实际血条：瞬间变化
        if (actualSlider != null)
        {
            actualSlider.value = targetHpPercent;
        }

        // 4. 缓冲血条：平滑变化
        if (bufferSlider != null)
        {
            if (bufferSlider.value > targetHpPercent)
            {
                // 扣血：缓冲条慢慢掉
                bufferSlider.value = Mathf.MoveTowards(bufferSlider.value, targetHpPercent, Time.deltaTime * bufferSpeed);
            }
            else
            {
                // 回血：缓冲条瞬间跟上
                bufferSlider.value = targetHpPercent;
            }
        }

        // 5. 颜色渐变与低血量闪烁
        if (actualFillImage != null)
        {
            // 血量越少越红，越多越绿
            actualFillImage.color = Color.Lerp(Color.red, Color.green, targetHpPercent);

            if (targetHpPercent <= 0.2f && targetHpPercent > 0)
            {
                // 低血闪烁：用 Mathf.PingPong 控制透明度
                float alpha = Mathf.PingPong(Time.time * 5f, 1f);
                Color c = actualFillImage.color;
                c.a = Mathf.Lerp(0.3f, 1f, alpha);
                actualFillImage.color = c;
            }
            else
            {
                Color c = actualFillImage.color;
                c.a = 1f;
                actualFillImage.color = c;
            }
        }
    }
}