using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI finalScoreText;
    public Button restartButton;

    private void Start()
    {
        panel.SetActive(false);
        restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());

        // 监听 GameManager 的状态改变
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver || state == GameManager.GameState.Victory)
        {
            panel.SetActive(true);

            if (state == GameManager.GameState.Victory)
            {
                resultText.text = "<color=yellow>游戏胜利！</color>";
            }
            else
            {
                resultText.text = "<color=red>游戏失败</color>";
            }

            // 获取最终得分
            if (TotalScoreUIManager.Instance != null)
            {
                // 这里假设 TotalScoreUIManager 存有 accumulatedDamage，如果没有，你可以把分数放到 GameManager
                finalScoreText.text = $"最终得分: {Mathf.RoundToInt(TotalScoreUIManager.Instance.GetTotalScore())}";
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }
}