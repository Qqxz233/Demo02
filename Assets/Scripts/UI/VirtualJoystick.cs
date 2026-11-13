using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI 引用")]
    public RectTransform background;
    public RectTransform handle;

    [Header("参数设置")]
    public float handleRange = 100f;
    public float deadZone = 10f;

    [Header("旋转设置")]
    [Tooltip("拖拽时摇杆把手是否跟随左右旋转")]
    public bool rotateHandle = true;
    [Tooltip("旋转跟随的平滑度")]
    public float rotationSmoothSpeed = 15f;

    public Vector2 Direction { get; private set; } = Vector2.zero;

    private CanvasGroup canvasGroup;
    private Quaternion originalHandleRotation;

    private void Awake()
    {
        // 自动获取或添加 CanvasGroup，用于控制显隐和射线检测
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (handle != null)
        {
            originalHandleRotation = handle.localRotation;
        }
    }

    private void Start()
    {
        // 初始状态：完全透明且不接受点击，这样 FindObjectOfType 永远能找到它
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    // 供 GameManager 调用
    public void ShowUI()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        ResetJoystick();
    }

    private void Update()
    {
        // 如果使用键盘，自动重置摇杆
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            ResetJoystick();
        }

        // 平滑旋转把手
        if (rotateHandle && handle != null)
        {
            handle.localRotation = Quaternion.Slerp(handle.localRotation,
                Direction == Vector2.zero ? originalHandleRotation : handle.localRotation,
                Time.deltaTime * rotationSmoothSpeed);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 如果 UI 还没显示，不接受拖拽
        if (canvasGroup.alpha < 0.1f) return;

        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out localPointerPos))
        {
            Vector2 clampedPos = Vector2.ClampMagnitude(localPointerPos, handleRange);

            if (handle != null)
            {
                handle.anchoredPosition = clampedPos;

                // 【核心新增】：计算拖拽角度并让把手旋转
                if (rotateHandle && clampedPos.magnitude > deadZone)
                {
                    // 计算 X 和 Y 的夹角（假设初始朝上，即 12 点钟方向为 0 度）
                    float angle = Mathf.Atan2(clampedPos.x, clampedPos.y) * Mathf.Rad2Deg;
                    // 因为 UI 旋转是逆时针为正，而屏幕坐标向右是 X 正，所以取负号修正
                    handle.localRotation = Quaternion.Euler(0, 0, -angle);
                }
            }

            if (clampedPos.magnitude > deadZone) Direction = clampedPos.normalized;
            else Direction = Vector2.zero;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    private void ResetJoystick()
    {
        Direction = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
            // 松手时，平滑转回初始朝向
            handle.localRotation = originalHandleRotation;
        }
    }
}