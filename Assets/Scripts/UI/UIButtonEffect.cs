using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("点击缩放设置")]
    public float pressScale = 0.9f;     // 按下时缩小到的比例
    public float animationSpeed = 15f;  // 弹回的速度

    private Vector3 originalScale;
    private bool isPressed = false;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void Update()
    {
        // 如果按下了，就缩小；松开了就弹回原大小
        Vector3 targetScale = isPressed ? originalScale * pressScale : originalScale;

        // 使用 UnscaledDeltaTime，确保游戏暂停时（TimeScale=0）点击依然有动画
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
    }
}