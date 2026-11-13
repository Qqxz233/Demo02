using System.Collections.Generic;
using UnityEngine;

public class AutoTurret : MonoBehaviour
{
    [Header("炮台基础引用")]
    public Transform turretHead;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public Animator turretAnimator; // 【新增】：用于播放开火动画

    [Header("基础属性 (每局初始值)")]
    public float baseFireRate = 2.0f;
    public float baseBulletSpeed = 30f;
    public float baseBulletDamage = 10f;
    public int baseBulletCount = 1;
    public float detectRange = 30f;
    public float rotationSpeed = 15f;

    [Header("基础暴击属性")]
    public float baseCritRate = 0f;
    public float baseCritDamage = 0f;

    [Header("基础子弹尺寸")]
    public float baseBulletSizeMultiplier = 3f;

    [Header("运行时属性 (肉鸽升级修改这里)")]
    public float currentFireRate;
    public float currentBulletSpeed;
    public float currentBulletDamage;
    public int bulletCount;
    [HideInInspector] public float temporaryFireRateMultiplier = 1f;

    [Header("运行时暴击属性")]
    public float currentCritRate;
    public float currentCritDamage;

    [Header("运行时子弹特殊属性")]
    public bool hasSlowExtraDamage = false;
    public float slowExtraDamageMult = 0f;
    public float bulletSizeMultiplier = 1f;
    public int currentPierceCount = 0;
    public float currentPierceFalloff = 0f;

    [Header("减速弹属性")]
    public bool hasSlowBullet = false;
    public float slowBulletDamageMult = 1f;
    public float slowAmount = 1f;
    public float slowDuration = 0f;

    [Header("拖尾特效")]
    public List<GameObject> activeTrailPrefabs = new List<GameObject>();

    private float nextFireTime = 0f;
    private Transform targetEnemy;

    private void Start()
    {
        ResetStats();
    }

    public void ResetStats()
    {
        currentFireRate = baseFireRate;
        currentBulletSpeed = baseBulletSpeed;
        currentBulletDamage = baseBulletDamage;
        bulletCount = baseBulletCount;
        temporaryFireRateMultiplier = 1f;

        currentCritRate = baseCritRate;
        currentCritDamage = baseCritDamage;

        hasSlowExtraDamage = false;
        slowExtraDamageMult = 0f;
        bulletSizeMultiplier = baseBulletSizeMultiplier;
        currentPierceCount = 0;
        currentPierceFalloff = 0f;

        hasSlowBullet = false;
        slowBulletDamageMult = 1f;
        slowAmount = 1f;
        slowDuration = 0f;

        activeTrailPrefabs.Clear();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        FindClosestEnemy();

        if (targetEnemy != null)
        {
            RotateTurretToTarget();

            if (Time.time >= nextFireTime)
            {
                // 【核心逻辑】：优先播放动画，由动画事件触发真实开火
                if (turretAnimator != null)
                {
                    turretAnimator.SetTrigger("Shoot");
                }
                else
                {
                    // 如果没挂动画，直接开火兜底
                    AnimationEvent_Fire();
                }

                float finalFireInterval = currentFireRate / Mathf.Max(0.1f, temporaryFireRateMultiplier);
                nextFireTime = Time.time + finalFireInterval;
            }
        }
    }

    private void FindClosestEnemy()
    {
        if (EnemyManager.Instance.ActiveEnemies.Count == 0)
        {
            targetEnemy = null;
            return;
        }

        float minSqrDistance = detectRange * detectRange;
        Transform closest = null;
        Vector3 currentPos = transform.position;

        for (int i = 0; i < EnemyManager.Instance.ActiveEnemies.Count; i++)
        {
            Transform enemy = EnemyManager.Instance.ActiveEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float sqrDist = (enemy.position - currentPos).sqrMagnitude;
            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                closest = enemy;
            }
        }
        targetEnemy = closest;
    }

    private void RotateTurretToTarget()
    {
        Vector3 direction = targetEnemy.position - turretHead.position;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // ==========================================================
    // 【核心新增】：供 Animation Event 调用的真实开火方法
    // ==========================================================
    public void AnimationEvent_Fire()
    {
        if (targetEnemy == null) return;

        float angleStep = 15f;
        float startAngle = -angleStep * ((bulletCount - 1) / 2f);
        Vector3 aimDir = (targetEnemy.position - firePoint.position).normalized;

        float finalDamage = currentBulletDamage;
        if (hasSlowBullet) finalDamage *= slowBulletDamageMult;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion bulletRotation = Quaternion.LookRotation(aimDir) * Quaternion.Euler(0, currentAngle, 0);

            GameObject bulletObj = ObjectPoolManager.Instance.GetObject(bulletPrefab, firePoint.position, bulletRotation);

            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Init(
                    currentBulletSpeed, finalDamage,
                    hasSlowBullet, slowAmount, slowDuration,
                    activeTrailPrefabs,
                    currentCritRate, currentCritDamage,
                    hasSlowExtraDamage, slowExtraDamageMult,
                    bulletSizeMultiplier,
                    currentPierceCount, currentPierceFalloff
                );
            }
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.PlayerShoot);
        if (VFXManager.Instance != null) VFXManager.Instance.PlayVFX(VFXType.MuzzleFlash, firePoint.position, firePoint.rotation);

        Physics.SyncTransforms();
    }
} // <--- 确保文件末尾有这个闭合的大括号