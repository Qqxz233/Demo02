using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyIndicatorUI : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI enemyCountText;
    public GameObject arrowPrefab;
    public Transform arrowParent;

    [Header("指示器与敌人设置")]
    public int maxArrows = 5;
    public float edgePadding = 50f;
    public float smoothTime = 0.1f;

    [Header("UI进场与动效设置")]
    public float slideSpeed = 5f;
    public float punchScaleAmount = 1.2f;
    public float punchRestoreSpeed = 8f;

    private Camera mainCamera;
    private List<GameObject> arrowPool = new List<GameObject>();
    private List<Vector2> arrowVelocities = new List<Vector2>();

    private RectTransform countTextRect;
    private Vector2 targetCountPos;
    private bool isShowingUI = false;
    private bool hasStartedSlide = false; // 标记是否已经开始滑入
    private int lastEnemyCount = 0;
    private float currentScaleMult = 1f;

    private void Start()
    {
        mainCamera = Camera.main;

        for (int i = 0; i < maxArrows; i++)
        {
            GameObject arrow = Instantiate(arrowPrefab, arrowParent != null ? arrowParent : transform);
            arrow.SetActive(false);
            arrowPool.Add(arrow);
            arrowVelocities.Add(Vector2.zero);
        }

        if (enemyCountText != null)
        {
            countTextRect = enemyCountText.GetComponent<RectTransform>();
            targetCountPos = countTextRect.anchoredPosition;

            // 【核心修复】：在 Start 里直接把它藏在屏幕上方，并禁用
            countTextRect.anchoredPosition = targetCountPos + new Vector2(0, 200f);
            enemyCountText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 【核心修复】：由 UI 自己监听状态。一旦从 Loading 变成 主菜单/运镜/游玩，就立刻唤醒并滑入
        if (!hasStartedSlide && GameManager.Instance.CurrentState != GameManager.GameState.Loading && GameManager.Instance.CurrentState != GameManager.GameState.MainMenu)
        {
            hasStartedSlide = true;
            isShowingUI = true;
            if (enemyCountText != null) enemyCountText.gameObject.SetActive(true);
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            HideAllArrows();
            // 注意：这里不 return，因为运镜时（Cinematic）UI 也要滑下来
        }

        HandleCountTextAnimation();

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            UpdateEnemyCount();
            UpdateOffscreenIndicators();
        }
    }

    private void HandleCountTextAnimation()
    {
        if (countTextRect == null) return;

        // 1. 平滑滑入动画
        if (isShowingUI)
        {
            countTextRect.anchoredPosition = Vector2.Lerp(
                countTextRect.anchoredPosition,
                targetCountPos,
                Time.unscaledDeltaTime * slideSpeed
            );

            if (Vector2.Distance(countTextRect.anchoredPosition, targetCountPos) < 1f)
            {
                countTextRect.anchoredPosition = targetCountPos;
                isShowingUI = false;
            }
        }

        // 2. 缩放平滑恢复
        if (currentScaleMult > 1f)
        {
            currentScaleMult = Mathf.Lerp(currentScaleMult, 1f, Time.unscaledDeltaTime * punchRestoreSpeed);
            countTextRect.localScale = Vector3.one * currentScaleMult;
        }
    }

    private void UpdateEnemyCount()
    {
        if (enemyCountText != null && EnemyManager.Instance != null)
        {
            int currentCount = EnemyManager.Instance.ActiveEnemies.Count;

            if (currentCount < lastEnemyCount)
            {
                currentScaleMult = punchScaleAmount;
                if (countTextRect != null) countTextRect.localScale = Vector3.one * currentScaleMult;
            }

            lastEnemyCount = currentCount;
            enemyCountText.text = $"剩余敌人：{currentCount}";
        }
    }

    // ... (UpdateOffscreenIndicators, ShowArrow, HideAllArrows 保持原样即可) ...
    private void UpdateOffscreenIndicators()
    {
        if (EnemyManager.Instance == null || PlayerController.Instance == null) return;

        List<Transform> enemies = EnemyManager.Instance.ActiveEnemies;
        List<Transform> offscreenEnemies = new List<Transform>();

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(enemy.position);
            if (viewportPos.z > 0 && (viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f))
            {
                offscreenEnemies.Add(enemy);
            }
        }

        offscreenEnemies.Sort((a, b) =>
        {
            float distA = (a.position - PlayerController.Instance.transform.position).sqrMagnitude;
            float distB = (b.position - PlayerController.Instance.transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        for (int i = 0; i < arrowPool.Count; i++)
        {
            if (i < offscreenEnemies.Count && i < maxArrows)
                ShowArrow(i, offscreenEnemies[i]);
            else
                arrowPool[i].SetActive(false);
        }
    }

    private void ShowArrow(int arrowIndex, Transform target)
    {
        GameObject arrow = arrowPool[arrowIndex];
        if (!arrow.activeSelf) arrow.SetActive(true);

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(target.position);
        Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);

        Vector2 canvasSize = (arrowParent as RectTransform).rect.size;
        float halfW = (canvasSize.x / 2) - edgePadding;
        float halfH = (canvasSize.y / 2) - edgePadding;

        float scaleX = Mathf.Abs(halfW / dir.x);
        float scaleY = Mathf.Abs(halfH / dir.y);
        float minScale = Mathf.Min(scaleX, scaleY);

        Vector2 edgeTargetPos = dir * minScale;

        RectTransform rectTransform = arrow.GetComponent<RectTransform>();
        Vector2 currentVelocity = arrowVelocities[arrowIndex];

        rectTransform.anchoredPosition = Vector2.SmoothDamp(rectTransform.anchoredPosition, edgeTargetPos, ref currentVelocity, smoothTime);
        arrowVelocities[arrowIndex] = currentVelocity;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private void HideAllArrows()
    {
        foreach (var arrow in arrowPool)
        {
            if (arrow.activeSelf) arrow.SetActive(false);
        }
    }
    // 【确保 EnemyIndicatorUI.cs 中有这个方法】
    public void ShowUI()
    {
        isShowingUI = true;
        hasStartedSlide = true;
        if (enemyCountText != null)
        {
            enemyCountText.gameObject.SetActive(true);
        }
    }
}