using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public event Action OnUpgradeFinished;

    [Header("卡池与UI")]
    public List<UpgradeCardSO> allCards;
    public GameObject upgradePanel;
    public Transform cardContainer;
    public GameObject cardUIPrefab;

    [Header("转场动画参数")]
    public float transitionDuration = 0.8f;
    public Vector2 startAnchoredPos = new Vector2(800f, 600f);
    public float startRotationZ = 45f;
    public float finalCardScale = 1.11f;
    public float startScaleMultiplier = 0.5f;

    private AutoTurret playerTurret;
    private ShieldManager playerShields;

    private List<UpgradeCardSO> drawnCards = new List<UpgradeCardSO>();
    private List<UpgradeCardSO> tempCardPool = new List<UpgradeCardSO>();
    private List<string> unlockedCardNames = new List<string>();

    private bool isSelecting = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        playerTurret = FindObjectOfType<AutoTurret>();
        playerShields = FindObjectOfType<ShieldManager>();
        upgradePanel.SetActive(false);
    }

    public void TriggerUpgrade()
    {
        isSelecting = false;
        GameManager.Instance.ChangeState(GameManager.GameState.PausedForUpgrade);
        upgradePanel.SetActive(true);
        DrawCards();

        StopAllCoroutines();
        StartCoroutine(PlayTransitionRoutine());
    }

    private void DrawCards()
    {
        drawnCards.Clear();
        tempCardPool.Clear();

        // 【核心修复 1】：使用 HashSet 去重，防止面板里多次拖入同一个SO导致重复
        HashSet<UpgradeCardSO> uniqueCards = new HashSet<UpgradeCardSO>();

        foreach (var card in allCards)
        {
            if (card == null) continue;

            // 检查前置条件
            if (string.IsNullOrEmpty(card.prerequisiteCardName) || unlockedCardNames.Contains(card.prerequisiteCardName))
            {
                uniqueCards.Add(card); // HashSet 会自动过滤掉重复项
            }
        }

        // 将去重后的卡牌放入临时池
        tempCardPool.AddRange(uniqueCards);

        if (tempCardPool.Count == 0)
        {
            Debug.LogError("<color=red>【抽卡失败】经过前置条件过滤和去重后，卡池为空！</color>");
            return;
        }

        // 洗牌（Fisher-Yates 算法）
        for (int i = 0; i < tempCardPool.Count; i++)
        {
            UpgradeCardSO temp = tempCardPool[i];
            int randomIndex = UnityEngine.Random.Range(i, tempCardPool.Count);
            tempCardPool[i] = tempCardPool[randomIndex];
            tempCardPool[randomIndex] = temp;
        }

        // 抽取前 3 张（如果卡池不足 3 张，有多少抽多少）
        int drawCount = Mathf.Min(3, tempCardPool.Count);
        for (int i = 0; i < drawCount; i++)
        {
            drawnCards.Add(tempCardPool[i]);
        }

        // 渲染 UI
        foreach (Transform child in cardContainer) Destroy(child.gameObject);
        for (int i = 0; i < drawnCards.Count; i++)
        {
            GameObject cardObj = Instantiate(cardUIPrefab, cardContainer);
            CardUI ui = cardObj.GetComponent<CardUI>();
            if (ui != null) ui.Setup(drawnCards[i], this);
        }
    }

    private IEnumerator PlayTransitionRoutine()
    {
        RectTransform containerRect = cardContainer.GetComponent<RectTransform>();
        if (containerRect == null) yield break;

        Vector3 targetScale = Vector3.one * finalCardScale;
        Vector3 initialScale = targetScale * startScaleMultiplier;

        containerRect.anchoredPosition = startAnchoredPos;
        containerRect.localRotation = Quaternion.Euler(0, 0, startRotationZ);
        containerRect.localScale = initialScale;

        Vector2 targetPos = Vector2.zero;
        Quaternion targetRot = Quaternion.identity;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / transitionDuration;
            float easeT = EaseOutBack(t);

            containerRect.anchoredPosition = Vector2.LerpUnclamped(startAnchoredPos, targetPos, easeT);
            containerRect.localRotation = Quaternion.SlerpUnclamped(Quaternion.Euler(0, 0, startRotationZ), targetRot, easeT);
            containerRect.localScale = Vector3.LerpUnclamped(initialScale, targetScale, easeT);

            yield return null;
        }

        containerRect.anchoredPosition = targetPos;
        containerRect.localRotation = targetRot;
        containerRect.localScale = targetScale;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }

    public void SelectCard(UpgradeCardSO card)
    {
        if (isSelecting) return;
        isSelecting = true;

        if (!unlockedCardNames.Contains(card.cardName))
            unlockedCardNames.Add(card.cardName);

        ApplyCardEffect(card);
        upgradePanel.SetActive(false);

        GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        OnUpgradeFinished?.Invoke();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SoundType.UIClick);
    }

    private void ApplyCardEffect(UpgradeCardSO card)
    {
        if (playerTurret != null)
        {
            if (card.damageMultiplier > 0f) playerTurret.currentBulletDamage += playerTurret.currentBulletDamage * card.damageMultiplier;
            if (card.fireRateIntervalChange != 0f) playerTurret.currentFireRate = Mathf.Max(0.1f, playerTurret.currentFireRate + card.fireRateIntervalChange);
            playerTurret.bulletCount += card.spreadCountBonus;
            playerTurret.currentCritRate += card.critRateBonus;
            playerTurret.currentCritDamage += card.critDamageBonus;

            if (card.bulletSpeedAdd != 0f) playerTurret.currentBulletSpeed += card.bulletSpeedAdd;

            if (card.enableSlowExtraDamage)
            {
                playerTurret.hasSlowExtraDamage = true;
                playerTurret.slowExtraDamageMult += card.slowExtraDamageMultiplier;
            }

            playerTurret.bulletSizeMultiplier += card.bulletSizeAdd;

            if (card.enablePierce)
            {
                playerTurret.currentPierceCount += card.pierceCountAdd;
                playerTurret.currentPierceFalloff = card.pierceDamageFalloff;
            }

            if (card.isSlowBullet)
            {
                playerTurret.hasSlowBullet = true;
                playerTurret.slowBulletDamageMult = card.slowDamageMultiplier;
                playerTurret.slowAmount = card.slowAmount;
                playerTurret.slowDuration = card.slowDuration;
            }

            if (card.bulletTrailPrefab != null)
            {
                if (!playerTurret.activeTrailPrefabs.Contains(card.bulletTrailPrefab))
                    playerTurret.activeTrailPrefabs.Add(card.bulletTrailPrefab);
            }
        }

        if (playerShields != null)
        {
            playerShields.currentShieldCount += card.shieldCountAdd;
            playerShields.currentSizeMultiplier += card.shieldSizeAdd;
            playerShields.currentRotationSpeed += card.shieldRotationSpeedAdd;

            if (card.enableShieldReflect) playerShields.canReflect = true;
            if (card.enableShieldDamage) playerShields.canDamage = true;
            playerShields.currentShieldDamage += card.shieldDamageAdd;

            playerShields.RefreshShields();
        }
    }
}