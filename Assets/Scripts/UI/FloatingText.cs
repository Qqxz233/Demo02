using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public float moveSpeed = 80f;
    public float lifeTime = 0.8f;

    private float currentLife = 0f;
    private Color currentColor;
    private bool isCrit = false;
    private float baseScale = 1f;

    private void Awake()
    {
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(string text, Color color, bool crit = false)
    {
        if (textComponent == null) return;

        isCrit = crit;
        textComponent.text = text;
        currentColor = color;

        // 暴击时：变为亮橙色/金色，字号变大
        if (isCrit)
        {
            currentColor = new Color(1f, 0.6f, 0f); // 亮橙色
            baseScale = 1.6f; // 暴击字大 60%
            text += "!"; // 暴击后面加个叹号
            textComponent.text = text;
            textComponent.fontStyle = FontStyles.Bold; // 粗体
        }
        else
        {
            baseScale = 1f;
            textComponent.fontStyle = FontStyles.Normal;
        }

        currentColor.a = 1f;
        textComponent.color = currentColor;
        currentLife = 0f;

        transform.position += new Vector3(Random.Range(-20f, 20f), 0, 0);

        // 暴击初始缩放极大，产生“爆字”感
        transform.localScale = Vector3.one * (isCrit ? baseScale * 1.5f : 1f);
    }

    private void Update()
    {
        if (textComponent == null) return;

        float delta = Time.unscaledDeltaTime;

        // 向上飘动
        transform.position += Vector3.up * moveSpeed * delta;

        currentLife += delta;

        // 【核心修改】：动画表现分离
        if (isCrit)
        {
            // 暴击动画：猛地弹回正常暴击大小
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * baseScale, delta * 10f);
        }
        else
        {
            // 普通动画：缓缓变大一点
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * baseScale, delta * 5f);
        }

        // 渐隐
        if (currentLife >= lifeTime * 0.6f)
        {
            float alpha = 1f - ((currentLife - lifeTime * 0.6f) / (lifeTime * 0.4f));
            currentColor.a = Mathf.Clamp01(alpha);
            textComponent.color = currentColor;

            if (currentColor.a <= 0)
            {
                ObjectPoolManager.Instance.ReturnObject(gameObject);
            }
        }
    }
}