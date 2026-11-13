using UnityEngine;

public enum SkillType { Shoot, Dash, Explode }

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Enemy Skill")]
public class EnemySkillSO : ScriptableObject
{
    [Header("基础设置")]
    public SkillType skillType;
    public float cooldown = 3f;
    public float castRange = 10f;
    public float globalCooldown = 0.5f;
    public int weight = 50;

    [Header("射击技能参数 (Shoot)")]
    public GameObject enemyBulletPrefab;
    public int burstCount = 1;
    public float burstInterval = 0.2f;
    public int bulletCount = 1;
    public float spreadAngle = 15f;

    [Header("冲撞技能参数 (Dash)")]
    public float chargeTime = 1f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.5f;
    public float dashDamage = 20f;
    public float dashKnockbackForce = 15f;
    public float dashHitRadius = 1.5f;
    // 【已删除】：不再需要 AudioClip 字段

    [Header("爆炸技能参数 (Explode)")]
    public float countdownDuration = 2.5f;
    public float explosionRadius = 8f;
    public float explosionDamage = 40f;
    public bool deathAfterExplode = true;
    public float countdownSpeedMultiplier = 1.3f;
    // 【已删除】：不再需要 AudioClip 字段
}