using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public Image iconImage;
    public Button selectButton;

    private UpgradeCardSO currentCard;
    private UpgradeManager manager;

    public void Setup(UpgradeCardSO card, UpgradeManager mgr)
    {
        currentCard = card;
        manager = mgr;

        titleText.text = card.cardName;
        descText.text = card.description;
        if (iconImage != null && card.icon != null) iconImage.sprite = card.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => manager.SelectCard(currentCard));

        // 【新增】：开启自身淡入和缩放动画
        StartCoroutine(EntranceRoutine());
    }

    private IEnumerator EntranceRoutine()
    {
        transform.localScale = Vector3.zero;
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // 随机的微小延迟，产生错峰出场的效果
        yield return new WaitForSecondsRealtime(Random.Range(0f, 0.15f));

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 6f;
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f); // EaseOut
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, easeT);
            group.alpha = Mathf.Lerp(0f, 1f, easeT);
            yield return null;
        }
        transform.localScale = Vector3.one;
        group.alpha = 1f;
    }
}