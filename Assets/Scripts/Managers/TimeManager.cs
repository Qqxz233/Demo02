using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private Coroutine hitStopCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerHitStop(float duration, float timeScale = 0.05f)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (hitStopCoroutine != null) return;

        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, timeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float timeScale)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            Time.timeScale = 1f;
        }
        hitStopCoroutine = null;
    }

    // 【核心修复】：去掉 Update 里的强制改回 1，或者加严格判断
    private void Update()
    {
        // 只有在 Playing 状态，且没被顿帧、没被加载界面暂停时，才检查是否异常
        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing && hitStopCoroutine == null)
        {
            // 如果时间流速是 0，说明可能被加载界面或者其他东西暂停了，不要乱动它
            if (Time.timeScale > 0.01f && Time.timeScale < 0.99f)
            {
                Time.timeScale = 1f;
            }
        }
    }
}