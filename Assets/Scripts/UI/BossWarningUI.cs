using System.Collections;
using UnityEngine;
using TMPro;

public class BossWarningUI : MonoBehaviour
{
    public static BossWarningUI Instance { get; private set; }

    [Header("UI引用")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    public float blinkInterval = 0.3f;

    [Header("音效渐隐设置")]
    public float fadeOutDuration = 0.5f; // 渐隐耗时(秒)
    private AudioSource warningAudioSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
            warningPanel.transform.SetAsLastSibling();
        }

        // 初始化独立音源
        warningAudioSource = gameObject.AddComponent<AudioSource>();
        warningAudioSource.playOnAwake = false;
        warningAudioSource.spatialBlend = 0f; // UI音效通常设为2D
    }

    public IEnumerator ShowWarning(float delay)
    {
        Debug.Log($"<color=red>【Boss警告】开始播放警告UI，持续 {delay} 秒！</color>");

        if (warningPanel != null) warningPanel.SetActive(true);

        // 【核心修改】：使用独立音源播放警告音
        if (AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetSoundClip(SoundType.BossWarning);
            if (clip != null)
            {
                warningAudioSource.clip = clip;
                warningAudioSource.volume = 1f;
                warningAudioSource.Play();
            }
        }

        float startTime = Time.realtimeSinceStartup;
        bool isVisible = true;

        // 倒计时闪烁
        while (Time.realtimeSinceStartup - startTime < delay)
        {
            if (warningText != null)
                warningText.text = isVisible ? "<color=red>WARNING</color>\nBoss Approaching" : "";

            isVisible = !isVisible;
            yield return new WaitForSecondsRealtime(blinkInterval);
        }

        // 隐藏 UI
        if (warningPanel != null) warningPanel.SetActive(false);

        // 【核心修改】：UI消失时，音效开始渐隐并停止
        if (warningAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutRoutine(fadeOutDuration));
        }

        // 播放 Boss 出场音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SoundType.BossAppear);

    }

    // 音量渐隐协程
    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = warningAudioSource.volume;
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            warningAudioSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        warningAudioSource.Stop();
    }
}