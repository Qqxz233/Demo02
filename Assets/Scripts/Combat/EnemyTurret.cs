using System.Collections;
using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    [Header("炮台引用")]
    public Transform turretHead;
    public Transform firePoint;
    public Animator turretAnimator;

    [Header("旋转参数")]
    public float turretTurnSpeed = 10f;

    private Transform playerTransform;
    private bool isShooting = false;

    private EnemySkillSO currentSkill;
    private float currentDamageMultiplier = 1f;
    private GameObject bulletPrefab;
    private float baseBulletDamage;

    // 【核心新增】：炮台专属独立音源，保证开火必定发声
    private AudioSource localShootAudio;

    private void Start()
    {
        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform;

        // 初始化独立的 2D 炮台音效源
        localShootAudio = gameObject.AddComponent<AudioSource>();
        localShootAudio.playOnAwake = false;
        localShootAudio.spatialBlend = 0f; // 设为 2D 音效，不管多远都能听到

        if (AudioManager.Instance != null)
        {
            AudioClip shootClip = AudioManager.Instance.GetSoundClip(SoundType.EnemyShoot);
            if (shootClip != null)
            {
                localShootAudio.clip = shootClip;
                localShootAudio.volume = 0.5f; // 根据素材微调音量
            }
            else
            {
                Debug.LogWarning("<color=yellow>【配置警告】AudioManager 中没有配置 EnemyShoot 音效！</color>");
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (playerTransform == null || turretHead == null) return;

        Vector3 dirToPlayer = playerTransform.position - turretHead.position;
        dirToPlayer.y = 0;
        if (dirToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
            turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRot, Time.deltaTime * turretTurnSpeed);
        }
    }

    public void StartShooting(EnemySkillSO skill, GameObject enemyBulletPrefab, float baseDamage, float damageMultiplier)
    {
        if (isShooting) return;

        currentSkill = skill;
        bulletPrefab = enemyBulletPrefab;
        baseBulletDamage = baseDamage;
        currentDamageMultiplier = damageMultiplier;

        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        isShooting = true;

        for (int i = 0; i < currentSkill.burstCount; i++)
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) break;

            if (turretAnimator != null) turretAnimator.SetTrigger("Shoot");
            else AnimationEvent_Fire();

            yield return new WaitForSeconds(currentSkill.burstInterval);
        }

        yield return new WaitForSeconds(0.5f);
        isShooting = false;
    }

    // 由动画事件或代码直接调用
    public void AnimationEvent_Fire()
    {
        if (currentSkill == null || playerTransform == null || firePoint == null) return;

        Vector3 dirToPlayer = (playerTransform.position - firePoint.position).normalized;
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        float angleStep = currentSkill.spreadAngle;
        float startAngle = -angleStep * ((currentSkill.bulletCount - 1) / 2f);
        float finalDamage = baseBulletDamage * currentDamageMultiplier;

        for (int i = 0; i < currentSkill.bulletCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion bulletRotation = Quaternion.LookRotation(dirToPlayer) * Quaternion.Euler(0, currentAngle, 0);

            GameObject bulletObj = ObjectPoolManager.Instance.GetObject(bulletPrefab, firePoint.position, bulletRotation);
            EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                bulletScript.Init(bulletRotation * Vector3.forward, finalDamage);
            }
        }

        // 【核心修复】：用本地音源播放，绝对会响
        if (localShootAudio != null && localShootAudio.clip != null)
        {
            // 使用 PlayOneShot 允许多个开火声重叠，射速快也不会卡没
            localShootAudio.PlayOneShot(localShootAudio.clip, localShootAudio.volume);
        }
    }
}