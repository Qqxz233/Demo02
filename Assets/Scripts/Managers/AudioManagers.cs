using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频配置列表")]
    public List<SoundDataSO> soundDataList;

    [Header("全局音量 (0-1)")]
    public float masterVolume = 1f;
    public float bgmVolume = 0.5f;
    public float sfxVolume = 0.8f;

    [Header("性能保护 (极端情况拦截)")]
    public int maxConcurrentSFX = 20;

    private AudioSource bgmSource;
    private AudioSource[] sfxSourcesPool;
    private int currentSfxIndex = 0;
    private Dictionary<SoundType, SoundDataSO> soundDict;
    private List<AudioSource> loopingSFXPool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 【核心修复 1】：初始化 BGM 通道
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.priority = 0; // 【绝对重要】：设为最高优先级，引擎绝不会抢占它！
        bgmSource.ignoreListenerPause = true; // 忽略全局 AudioListener 的暂停

        // 初始化 2D 音效池
        sfxSourcesPool = new AudioSource[40];
        for (int i = 0; i < sfxSourcesPool.Length; i++)
        {
            sfxSourcesPool[i] = gameObject.AddComponent<AudioSource>();
            sfxSourcesPool[i].playOnAwake = false;
            sfxSourcesPool[i].priority = 128; // 普通音效优先级中等
        }

        soundDict = new Dictionary<SoundType, SoundDataSO>();
        foreach (var data in soundDataList)
        {
            if (!soundDict.ContainsKey(data.soundType))
                soundDict.Add(data.soundType, data);
        }
    }

    private void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (soundDict.TryGetValue(SoundType.BGM, out SoundDataSO bgmData))
        {
            bgmSource.clip = bgmData.clip;
            bgmSource.volume = bgmData.volume * bgmVolume * masterVolume;
            bgmSource.Play();
        }
    }

    public void PlaySFX(SoundType type, float delay = 0f)
    {
        if (!soundDict.TryGetValue(type, out SoundDataSO data)) return;

        // 极端并发限制，保护移动端 CPU
        int activeCount = 0;
        for (int i = 0; i < sfxSourcesPool.Length; i++)
        {
            if (sfxSourcesPool[i].isPlaying) activeCount++;
        }

        if (activeCount >= maxConcurrentSFX && (type == SoundType.PlayerShoot || type == SoundType.EnemyShoot || type == SoundType.EnemyHit))
        {
            return;
        }

        AudioSource source = sfxSourcesPool[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxSourcesPool.Length;

        source.clip = data.clip;
        source.volume = data.volume * sfxVolume * masterVolume;
        source.pitch = Random.Range(data.minPitch, data.maxPitch);

        if (delay > 0)
            source.PlayDelayed(delay);
        else
            source.PlayOneShot(data.clip, source.volume);
    }

    public AudioClip GetSoundClip(SoundType type)
    {
        if (soundDict != null && soundDict.TryGetValue(type, out SoundDataSO data)) return data.clip;
        return null;
    }

    public void PlaySpecificSFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource source = sfxSourcesPool[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxSourcesPool.Length;
        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    public void SwitchBGM(SoundType newBgmType, float fadeTime = 1f)
    {
        if (!soundDict.TryGetValue(newBgmType, out SoundDataSO newBgmData)) return;
        if (bgmSource.isPlaying && bgmSource.clip == newBgmData.clip) return;
        StartCoroutine(SwitchBGMRoutine(newBgmData, fadeTime));
    }

    private IEnumerator SwitchBGMRoutine(SoundDataSO newBgmData, float fadeTime)
    {
        float startVol = bgmSource.volume;
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0, t / fadeTime);
            yield return null;
        }

        bgmSource.clip = newBgmData.clip;
        bgmSource.volume = 0;
        bgmSource.Play();

        t = 0;
        float targetVol = newBgmData.volume * bgmVolume * masterVolume;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0, targetVol, t / fadeTime);
            yield return null;
        }
        bgmSource.volume = targetVol;
    }

    public AudioSource Play3DLoopingSFX(SoundType type, Transform followTarget)
    {
        if (!soundDict.TryGetValue(type, out SoundDataSO data)) return null;

        int activeCount = 0;
        foreach (var s in loopingSFXPool) if (s.isPlaying) activeCount++;
        if (activeCount >= 5) return null;

        AudioSource source = null;
        foreach (var s in loopingSFXPool)
        {
            if (!s.isPlaying) { source = s; break; }
        }

        if (source == null)
        {
            GameObject go = new GameObject("3DLoopingSFX_" + type.ToString());
            go.transform.SetParent(this.transform);
            source = go.AddComponent<AudioSource>();
            loopingSFXPool.Add(source);
            source.priority = 128; // 3D音效设为普通优先级
        }

        source.transform.position = followTarget.position;
        source.clip = data.clip;
        source.loop = true;
        source.spatialBlend = 1.0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 10f;
        source.maxDistance = 50f;
        source.volume = data.volume * sfxVolume * masterVolume;
        source.pitch = 1f;
        source.time = 0f;
        source.Play();

        return source;
    }

    public void Stop3DLoopingSFX(AudioSource source)
    {
        if (source != null && source.gameObject != null)
        {
            source.Stop();
            source.transform.SetParent(this.transform);
        }
    }

    public void SetMasterVolume(float vol) { masterVolume = vol; UpdateAllVolumes(); }
    public void SetBGMVolume(float vol) { bgmVolume = vol; UpdateAllVolumes(); }
    public void SetSFXVolume(float vol) { sfxVolume = vol; }

    private void UpdateAllVolumes()
    {
        if (bgmSource.isPlaying && soundDict.TryGetValue(SoundType.BGM, out SoundDataSO bgmData))
            bgmSource.volume = bgmData.volume * bgmVolume * masterVolume;
    }

    public void FadeOutBGM(float duration = 2f) { StartCoroutine(FadeOutRoutine(duration)); }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = bgmSource.volume;
        float t = 0;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0, t / duration);
            yield return null;
        }
        bgmSource.Stop();
    }
}