using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    // 当前的震动偏移量，供 CameraController 读取
    [HideInInspector] public Vector3 shakeOffset = Vector3.zero;

    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 使用 PerlinNoise 代替 Random，震动会非常丝滑，不会显得像癫痫
            float x = (Mathf.PerlinNoise(Time.unscaledTime * 25f, 0f) - 0.5f) * 2f * magnitude;
            float y = (Mathf.PerlinNoise(0f, Time.unscaledTime * 25f) - 0.5f) * 2f * magnitude;

            shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero; // 恢复
    }
}