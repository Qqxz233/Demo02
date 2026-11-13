using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject settingsPanel;
    public Button openButton;
    public Button closeButton;

    [Header("音量滑动条引用")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (openButton != null) openButton.onClick.AddListener(OpenSettings);
        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

        if (AudioManager.Instance != null)
        {
            masterSlider.value = AudioManager.Instance.masterVolume;
            bgmSlider.value = AudioManager.Instance.bgmVolume;
            sfxSlider.value = AudioManager.Instance.sfxVolume;
        }

        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OpenSettings()
    {
        // 防抖：如果面板已经打开了，不要重复触发
        if (settingsPanel.activeSelf) return;

        Debug.Log("<color=yellow>打开设置面板，尝试暂停游戏</color>");

        if (GameManager.Instance != null) GameManager.Instance.PauseForSettings();

        settingsPanel.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SoundType.UIClick);
    }

    private void CloseSettings()
    {
        Debug.Log("<color=green>点击了关闭按钮，尝试恢复游戏</color>");

        if (GameManager.Instance != null) GameManager.Instance.ResumeFromSettings();

        settingsPanel.SetActive(false);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SoundType.UIClick);
    }

    private void OnMasterChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
    }
}