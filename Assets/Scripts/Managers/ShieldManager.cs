using System.Collections.Generic;
using UnityEngine;

public class ShieldManager : MonoBehaviour
{
    public static ShieldManager Instance { get; private set; }

    [Header("护盾预制体")]
    public GameObject shieldPrefab;

    // 护盾属性
    public int currentShieldCount = 0;
    public float currentSizeMultiplier = 1f;
    public float currentRotationSpeed = 60f;
    public bool canReflect = false;
    public bool canDamage = false;
    public float currentShieldDamage = 0f;

    private List<PlayerShield> activeShields = new List<PlayerShield>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 当卡牌属性改变时，刷新所有护盾
    public void RefreshShields()
    {
        // 1. 生成缺失的护盾
        while (activeShields.Count < currentShieldCount)
        {
            GameObject obj = Instantiate(shieldPrefab, transform.position, Quaternion.identity);
            PlayerShield shield = obj.GetComponent<PlayerShield>();
            activeShields.Add(shield);
        }

        // 2. 移除多余的护盾
        while (activeShields.Count > currentShieldCount)
        {
            PlayerShield shield = activeShields[activeShields.Count - 1];
            activeShields.RemoveAt(activeShields.Count - 1);
            Destroy(shield.gameObject);
        }

        // 3. 更新所有护盾属性
        for (int i = 0; i < activeShields.Count; i++)
        {
            PlayerShield shield = activeShields[i];

            // 尺寸
            shield.transform.localScale = Vector3.one * currentSizeMultiplier;
            // 速度
            shield.rotationSpeed = currentRotationSpeed;
            // 功能
            shield.canReflect = canReflect;
            shield.canDamage = canDamage;
            shield.shieldDamage = currentShieldDamage;

            // 初始角度均匀分布
            float angle = i * (360f / currentShieldCount);
            shield.Setup(PlayerController.Instance, angle);
        }
    }
}