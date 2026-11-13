using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Loading, MainMenu, Cinematic, Playing, PausedForUpgrade, GameOver, Victory }
    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    private float timeScaleBeforeSettings = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ChangeState(GameState.Loading);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        if (newState == GameState.PausedForUpgrade || newState == GameState.GameOver || newState == GameState.Victory)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }

        // 【核心新增】：游戏结束时处理音效
        if (newState == GameState.GameOver)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeOutBGM(2f); // BGM 渐隐
                AudioManager.Instance.PlaySFX(SoundType.GameOver); // 播放 GameOver 音效
            }
        }
        else if (newState == GameState.Victory)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeOutBGM(2f);
                AudioManager.Instance.PlaySFX(SoundType.GameVictory); // 胜利音效（你之前应该已经配过了）
            }
        }

        OnGameStateChanged?.Invoke(newState);
    }

    public void OnLoadingComplete()
    {
        ChangeState(GameState.MainMenu);
        if (CameraController.Instance != null) CameraController.Instance.SetMenuView();

        MainMenuUI ui = FindObjectOfType<MainMenuUI>();
        if (ui != null) ui.ShowMenu();
    }

    public void StartGame()
    {
        MainMenuUI ui = FindObjectOfType<MainMenuUI>();
        if (ui != null) ui.HideMenu();

        // 唤醒血条
        PlayerHealthSliderUI healthUI = FindObjectOfType<PlayerHealthSliderUI>();
        if (healthUI != null) healthUI.ShowUI();

        // 唤醒剩余敌人UI
        EnemyIndicatorUI indicatorUI = FindObjectOfType<EnemyIndicatorUI>();
        if (indicatorUI != null) indicatorUI.ShowUI();

        // 【核心】：唤醒虚拟摇杆
        VirtualJoystick joystick = FindObjectOfType<VirtualJoystick>();
        if (joystick != null) joystick.ShowUI();

        ChangeState(GameState.Cinematic);
        StartCoroutine(CinematicRoutine());
    }

    private System.Collections.IEnumerator CinematicRoutine()
    {
        yield return StartCoroutine(CameraController.Instance.CinematicRoutine());

        ChangeState(GameState.Playing);
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnLoadingComplete();
    }

    public void PauseForSettings()
    {
        timeScaleBeforeSettings = Time.timeScale;
        Time.timeScale = 0f;
    }

    public void ResumeFromSettings()
    {
        Time.timeScale = timeScaleBeforeSettings;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}