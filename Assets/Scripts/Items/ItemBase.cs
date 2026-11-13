using System.Collections;
using UnityEngine;

public class ItemBase : MonoBehaviour
{
    [Header("触发目标设置")]
    [Tooltip("玩家踩上去是否生效")]
    public bool affectPlayer = true;
    [Tooltip("敌人踩上去是否生效")]
    public bool affectEnemy = false;

    [Header("基础触发设置")]
    public float cooldown = 1f;

    [Header("瞬时效果：伤害与治疗")]
    public float damage = 0f;
    public float healAmount = 0f;

    [Header("持续效果：移速与射速 (默认持续10秒)")]
    [Tooltip("移动速度倍率：1为正常，0.5为减速，1.5为加速")]
    public float moveSpeedMultiplier = 1f;
    [Tooltip("攻击速度倍率：1为正常，2为射速翻倍")]
    public float fireRateMultiplier = 1f;
    [Tooltip("持续效果的持续时间（秒）")]
    public float effectDuration = 10f;

    [Header("销毁与动画")]
    public bool destroyAfterUse = false;
    public float destroyDelay = 1f;
    public Animator itemAnimator;
    public string destroyAnimTrigger = "Destroy";

    private float lastTriggerTime = 0f;
    private bool isDestroyed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isDestroyed) return;
        if (Time.time < lastTriggerTime + cooldown) return;

        bool isPlayer = other.CompareTag("Player");
        bool isEnemy = other.CompareTag("Enemy");

        // 过滤不合法的触发者
        if (isPlayer && !affectPlayer) return;
        if (isEnemy && !affectEnemy) return;
        if (!isPlayer && !isEnemy) return;

        ApplyEffects(other, isPlayer, isEnemy);
        lastTriggerTime = Time.time;

        // 【核心新增】：触发时播放拾取音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SoundType.ItemPickup);
        }

        if (destroyAfterUse)
        {
            StartCoroutine(DestroyRoutine());
        }
    }

    private void ApplyEffects(Collider target, bool isPlayer, bool isEnemy)
    {
        if (isPlayer)
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = target.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                if (damage > 0) playerHealth.TakeDamage(damage, transform.position);
                if (healAmount > 0) playerHealth.Heal(healAmount);

                if (moveSpeedMultiplier != 1f || fireRateMultiplier != 1f)
                    StartCoroutine(PlayerBuffRoutine(playerHealth, moveSpeedMultiplier, fireRateMultiplier));
            }
        }
        else if (isEnemy)
        {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            if (enemy == null) enemy = target.GetComponentInParent<EnemyBase>();

            if (enemy != null)
            {
                if (damage > 0) enemy.TakeDamage(damage, transform.position);

                // 敌人只有移速 Buff，没有射速 Buff
                if (moveSpeedMultiplier != 1f)
                    StartCoroutine(EnemyBuffRoutine(enemy, moveSpeedMultiplier));
            }
        }
    }

    private IEnumerator PlayerBuffRoutine(PlayerHealth player, float moveMult, float fireMult)
    {
        PlayerController controller = player.GetComponent<PlayerController>();
        AutoTurret turret = player.GetComponentInChildren<AutoTurret>();

        float originalMove = controller != null ? controller.speedMultiplier : 1f;
        float originalFire = turret != null ? turret.temporaryFireRateMultiplier : 1f;

        if (controller != null) controller.speedMultiplier = moveMult;
        if (turret != null) turret.temporaryFireRateMultiplier = fireMult;

        yield return new WaitForSeconds(effectDuration);

        if (controller != null) controller.speedMultiplier = originalMove;
        if (turret != null) turret.temporaryFireRateMultiplier = originalFire;
    }

    private IEnumerator EnemyBuffRoutine(EnemyBase enemy, float moveMult)
    {
        float originalMove = enemy.speedMultiplier;
        enemy.speedMultiplier = moveMult;

        yield return new WaitForSeconds(effectDuration);

        if (enemy != null) enemy.speedMultiplier = originalMove;
    }

    private IEnumerator DestroyRoutine()
    {
        isDestroyed = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (itemAnimator != null && !string.IsNullOrEmpty(destroyAnimTrigger))
        {
            itemAnimator.SetTrigger(destroyAnimTrigger);
        }

        // 【可选项】：这里之前有一句 UIClick 的音效，如果觉得和拾取音效冲突，可以删掉
        // if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.UIClick);

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}