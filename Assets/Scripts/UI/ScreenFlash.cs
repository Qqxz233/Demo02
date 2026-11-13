using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }
    private Image flashImage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        flashImage = GetComponent<Image>();
    }

    // 触发屏幕闪红
    public void FlashRed(float duration = 0.3f)
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        float elapsed = 0f;
        Color c = flashImage.color;
        c.a = 0.4f; // 初始透明度 40%
        flashImage.color = c;

        while (elapsed < duration)
        {
            // 使用 unscaledDeltaTime，确保顿帧时闪红依然平滑
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            flashImage.color = c;
            yield return null;
        }
        c.a = 0f;
        flashImage.color = c;
    }
}