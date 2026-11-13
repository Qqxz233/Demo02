using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "Game/Enemy Config")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("基础属性")]
    public string enemyName = "普通敌人";
    public float maxHealth = 10f;
    public float moveSpeed = 5f;
    public int expDrop = 1;

    [Header("AI 移动与徘徊区间")]
    [Tooltip("大于此距离则继续追击玩家")]
    public float maxFollowDistance = 8f;
    [Tooltip("小于此距离则往后退，防止贴脸")]
    public float minFollowDistance = 4f;
    [Tooltip("在区间内绕着玩家徘徊时的移动速度")]
    public float wanderSpeed = 3f;

    [Header("伤害配置")]
    public float collisionDamage = 10f;  // 撞击玩家的基础伤害

    [Header("视觉与表现")]
    public float hitFlashDuration = 0.1f;

    [Header("掉落配置")]
    [Tooltip("该敌人死亡时的掉落池配置")]
    public LootTableSO lootTable;

    [Tooltip("死亡时触发掉落的总体概率 (0-100)。例如填30，就是30%概率掉东西")]
    [Range(0f, 100f)]
    public float dropChance = 30f;
}