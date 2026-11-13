using System.Collections;
using UnityEngine;

public class DeathCrossEffect : MonoBehaviour
{
    [Header("下落砸击参数")]
    public float fallDuration = 0.2f;
    public float startHeight = 6f;
    public float startScale = 4f;
    public float endScale = 1.2f;

    [Header("停留与渐隐")]
    public float stayDuration = 1.0f;
    public float fadeDuration = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private Material matInstance;
    private Color originalColor = Color.white;

    // 缓存 Shader 属性 ID，提升性能并避免拼写错误
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                matInstance = meshRenderer.material;
            }
        }

        // 【核心修复】：先判断属性是否存在，再获取颜色，防止报错
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else if (matInstance != null)
        {
            if (matInstance.HasProperty(BaseColorID)) // 兼容 URP
                originalColor = matInstance.GetColor(BaseColorID);
            else if (matInstance.HasProperty(ColorID)) // 兼容内置管线
                originalColor = matInstance.GetColor(ColorID);
            else
                originalColor = Color.white; // 如果没有颜色属性，默认给白色
        }

        StartCoroutine(PlayEffectRoutine());
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }

    private IEnumerator PlayEffectRoutine()
    {
        Vector3 targetPos = transform.position;
        Vector3 spawnPos = targetPos + Vector3.up * startHeight;

        transform.position = spawnPos;
        transform.localScale = Vector3.one * startScale;
        SetAlpha(1f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fallDuration;
            float easeT = t * t;

            transform.position = Vector3.Lerp(spawnPos, targetPos, easeT);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, easeT);
            yield return null;
        }

        transform.position = targetPos;
        transform.localScale = Vector3.one * endScale;

        yield return new WaitForSeconds(stayDuration);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            SetAlpha(1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = alpha;
            spriteRenderer.color = c;
        }
        else if (matInstance != null)
        {
            Color c = originalColor;
            c.a = alpha;

            // 【核心修复】：根据材质支持的属性来设置透明度
            if (matInstance.HasProperty(BaseColorID))
                matInstance.SetColor(BaseColorID, c);

            if (matInstance.HasProperty(ColorID))
                matInstance.SetColor(ColorID, c);
        }
    }
}