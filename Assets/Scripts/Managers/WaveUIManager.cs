using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WaveUIManager : MonoBehaviour
{
    public static WaveUIManager Instance { get; private set; }

    [Header("UI引用")]
    public Slider progressBar;
    public TextMeshProUGUI waveText;

    private int totalEnemiesInWave = 0;
    private int currentKilled = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (waveText != null) waveText.gameObject.SetActive(false);
    }

    // 每一波开始时，初始化进度条
    public void InitProgressBar(int totalEnemies)
    {
        totalEnemiesInWave = totalEnemies;
        currentKilled = 0;

        if (progressBar != null)
        {
            progressBar.maxValue = totalEnemies;
            progressBar.value = 0;
        }
    }

    // 击杀敌人时更新进度
    public void AddKillProgress()
    {
        currentKilled++;
        if (progressBar != null)
        {
            progressBar.value = currentKilled;
        }
    }

    // 显示“第X波”文字与动画
    public void ShowWaveText(int waveNumber)
    {
        if (waveText != null)
            StartCoroutine(WaveTextRoutine(waveNumber));
    }

    private IEnumerator WaveTextRoutine(int waveNumber)
    {
        waveText.text = $"第 {waveNumber} 波";
        waveText.gameObject.SetActive(true);

        // 简单的缩放弹跳动画
        waveText.transform.localScale = Vector3.one * 0.5f;
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            waveText.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one * 1.2f, t / 0.2f);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);
        waveText.gameObject.SetActive(false);
    }
}