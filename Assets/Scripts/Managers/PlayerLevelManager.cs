using System;
using UnityEngine;

public class PlayerLevelManager : MonoBehaviour
{
    public static PlayerLevelManager Instance { get; private set; }

    [Header("等级设置")]
    public int currentLevel = 1;
    public float currentExp = 0;
    public float baseExpRequirement = 5f;
    public float expMultiplier = 1.5f;

    public event Action OnLevelUp; // 升级事件

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddExp(float amount)
    {
        currentExp += amount;

        float requirement = baseExpRequirement * Mathf.Pow(expMultiplier, currentLevel - 1);

        if (currentExp >= requirement)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= (baseExpRequirement * Mathf.Pow(expMultiplier, currentLevel - 1));
        currentLevel++;
        Debug.Log($"<color=green>升级！当前等级: {currentLevel}</color>");

        // 触发升级事件（后续给UI用）
        OnLevelUp?.Invoke();

        // 【临时注释掉，等做完选卡UI再放开】
        GameManager.Instance.ChangeState(GameManager.GameState.PausedForUpgrade);
    }
}