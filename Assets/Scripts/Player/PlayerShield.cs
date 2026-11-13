using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [HideInInspector] public PlayerController targetPlayer;
    [HideInInspector] public float rotationSpeed = 60f;

    [Header("运行时属性")]
    public bool canReflect = false;
    public bool canDamage = false;
    public float shieldDamage = 10f;

    [Header("模型朝向补偿")]
    public float modelRotationOffset = 0f;

    [Header("碰撞反馈 (可配置)")]
    public GameObject hitVFXPrefab;

    [Header("对敌伤害检测")]
    public float damageRadius = 1.5f;
    public float damageCooldown = 0.5f; // 防止一帧打太多次
    private float lastDamageTime = 0f;

    // 【新增】：音效防抖冷却（防止满屏怪时音效重叠爆音）
    private float lastSoundTime = 0f;
    private float soundCooldown = 0.1f;

    private int enemyLayerMask;
    private float currentAngle = 0f;

    public void Setup(PlayerController player, float initialAngle)
    {
        targetPlayer = player;
        currentAngle = initialAngle;
        enemyLayerMask = LayerMask.GetMask("Enemy");
        UpdatePositionAndRotation();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing || targetPlayer == null) return;

        currentAngle += rotationSpeed * Time.deltaTime;
        UpdatePositionAndRotation();

        if (canDamage && shieldDamage > 0)
        {
            if (Time.time > lastDamageTime + damageCooldown)
            {
                CheckEnemyCollision();
            }
        }
    }

    private void UpdatePositionAndRotation()
    {
        if (targetPlayer == null) return;
        float radius = 2.5f;
        Vector3 offset = new Vector3(Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0, Mathf.Cos(currentAngle * Mathf.Deg2Rad)) * radius;
        transform.position = targetPlayer.transform.position + offset;

        Vector3 outwardDir = (transform.position - targetPlayer.transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(outwardDir) * Quaternion.Euler(0, modelRotationOffset, 0);
    }

    // 拦截子弹
    public void InterceptBullet(EnemyBullet bullet)
    {
        // 播放拦截特效
        if (hitVFXPrefab != null && ObjectPoolManager.Instance != null)
        {
            GameObject vfx = ObjectPoolManager.Instance.GetObject(hitVFXPrefab, bullet.transform.position, Quaternion.identity);
            Object.Destroy(vfx, 2f);
        }

        // 【核心新增】：播放拦截子弹音效（带防抖）
        if (Time.time > lastSoundTime + soundCooldown)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(SoundType.ShieldHitBullet);
            lastSoundTime = Time.time;
        }

        // 处理反弹
        if (canReflect && targetPlayer != null)
        {
            AutoTurret turret = targetPlayer.GetComponentInChildren<AutoTurret>();
            if (turret != null && turret.bulletPrefab != null)
            {
                Vector3 reflectDir = -bullet.GetDirection();
                Vector3 spawnPos = bullet.transform.position + (reflectDir * 0.5f);

                GameObject pBulletObj = ObjectPoolManager.Instance.GetObject(turret.bulletPrefab, spawnPos, Quaternion.LookRotation(reflectDir));
                Bullet pBullet = pBulletObj.GetComponent<Bullet>();
                if (pBullet != null)
                {
                    float reflectDamage = turret.currentBulletDamage * 0.5f;
                    pBullet.Init(30f, reflectDamage, false, 1f, 0f, null, turret.currentCritRate, turret.currentCritDamage);
                }
            }
        }
        bullet.Recycle();
    }

    // 撞击敌人
    private void CheckEnemyCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, enemyLayerMask);
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();

            if (enemy != null && enemy.GetHealthPercent() > 0)
            {
                // 造成伤害
                enemy.TakeDamage(shieldDamage, transform.position, false);
                lastDamageTime = Time.time;

                // 播放撞击特效
                if (hitVFXPrefab != null && ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.GetObject(hitVFXPrefab, enemy.transform.position, Quaternion.identity);
                }

                // 【核心新增】：播放撞击敌人音效（带防抖）
                if (Time.time > lastSoundTime + soundCooldown)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(SoundType.ShieldHitEnemy);
                    lastSoundTime = Time.time;
                }

                break;
            }
        }
    }
}