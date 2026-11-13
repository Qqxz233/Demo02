using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    public GameObject floatingTextPrefab;
    public Transform canvasTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 原来的通用方法保留（用于连击等文字）
    public void ShowText(string text, Color color, Vector3 worldPosition)
    {
        ShowInternal(text, color, worldPosition, false);
    }

    // 【新增】：专门用于伤害飘字
    public void ShowDamageText(int damage, Vector3 worldPosition, bool isCrit)
    {
        // 普通伤害传白色，暴击传橙色
        Color color = isCrit ? new Color(1f, 0.6f, 0f) : Color.white;
        ShowInternal(damage.ToString(), color, worldPosition, isCrit);
    }

    private void ShowInternal(string text, Color color, Vector3 worldPosition, bool isCrit)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        GameObject obj = ObjectPoolManager.Instance.GetObject(floatingTextPrefab, screenPos, Quaternion.identity);
        obj.transform.SetParent(canvasTransform, false);
        obj.transform.position = screenPos;

        FloatingText ft = obj.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Setup(text, color, isCrit);
        }
    }
}