using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("×Óµ¯»ù´¡ÊôÐÔ")]
    public float speed = 25f;
    public float damage = 10f;
    public float lifeTime = 5f;
    public float baseHitRadius = 0.4f;

    private int pierceCount = 0;
    private float pierceFalloff = 0f;
    private float currentDamageMult = 1f;

    private bool isSlowBullet = false;
    private float slowAmount = 1f;
    private float slowDuration = 0f;

    private float critRate = 0f;
    private float critDamage = 0f;

    private bool hasSlowExtraDamage = false;
    private float slowExtraDamageMult = 0f;

    private List<GameObject> targetTrailPrefabs = new List<GameObject>();
    private List<GameObject> spawnedTrails = new List<GameObject>();

    private float currentLife = 0f;
    private float actualSpeed;
    private float actualDamage;
    private float currentHitRadius;
    private Vector3 lastPosition;

    private List<EnemyBase> hitEnemies = new List<EnemyBase>();
    private RaycastHit[] hitResults = new RaycastHit[10];
    private int targetLayerMask;
    private int wallLayerIndex;

    public void Init(float setSpeed, float setDamage, bool slowBullet = false, float slowAmt = 1f, float slowDur = 0f, List<GameObject> trails = null, float critRate = 0f, float critDamage = 0f, bool slowExtra = false, float slowExtraMult = 0f, float sizeMult = 1f, int pierce = 0, float falloff = 0f)
    {
        actualSpeed = setSpeed;
        actualDamage = setDamage;

        isSlowBullet = slowBullet;
        slowAmount = slowAmt;
        slowDuration = slowDur;

        if (trails != null) targetTrailPrefabs = trails;

        this.critRate = critRate;
        this.critDamage = critDamage;

        hasSlowExtraDamage = slowExtra;
        slowExtraDamageMult = slowExtraMult;

        pierceCount = pierce;
        pierceFalloff = falloff;
        currentDamageMult = 1f;

        currentHitRadius = baseHitRadius * sizeMult;
        transform.localScale = Vector3.one * sizeMult;
    }

    private void OnEnable()
    {
        currentLife = 0f;
        lastPosition = transform.position;
        if (actualSpeed <= 0) actualSpeed = speed;

        currentDamageMult = 1f;
        hitEnemies.Clear();

        targetLayerMask = LayerMask.GetMask("Enemy", "Wall");
        wallLayerIndex = LayerMask.NameToLayer("Wall");

        SetupTrails();
    }

    private void OnDisable()
    {
        ClearTrails();
        transform.localScale = Vector3.one;
    }

    private void SetupTrails()
    {
        ClearTrails();
        foreach (var prefab in targetTrailPrefabs)
        {
            if (prefab == null) continue;
            GameObject trailObj = ObjectPoolManager.Instance.GetObject(prefab, transform.position, Quaternion.identity);
            trailObj.transform.SetParent(transform);
            trailObj.transform.localPosition = Vector3.zero;
            trailObj.transform.localRotation = Quaternion.identity;
            spawnedTrails.Add(trailObj);
        }
    }

    private void ClearTrails()
    {
        foreach (var trail in spawnedTrails)
        {
            if (trail != null) ObjectPoolManager.Instance.ReturnObject(trail);
        }
        spawnedTrails.Clear();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        Vector3 oldPos = transform.position;
        transform.Translate(Vector3.forward * actualSpeed * Time.deltaTime);

        currentLife += Time.deltaTime;
        if (currentLife >= lifeTime)
        {
            Recycle();
            return;
        }

        CheckCollisionPath(oldPos);
    }

    private void CheckCollisionPath(Vector3 oldPos)
    {
        Vector3 moveDir = transform.position - oldPos;
        float moveDistance = moveDir.magnitude;
        if (moveDistance <= 0) return;

        // Ç½Ìå¼ì²â
        if (Physics.Raycast(oldPos, moveDir.normalized, out RaycastHit wallHit, moveDistance + currentHitRadius, targetLayerMask))
        {
            if (wallHit.collider.gameObject.layer == wallLayerIndex)
            {
                if (VFXManager.Instance != null) VFXManager.Instance.PlayVFX(VFXType.HitSpark, wallHit.point, Quaternion.LookRotation(wallHit.normal));
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.BulletHitWall);
                Recycle();
                return;
            }
        }

        // µÐÈË¼ì²â
        int numHits = Physics.SphereCastNonAlloc(oldPos, currentHitRadius, moveDir.normalized, hitResults, moveDistance, targetLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < numHits; i++)
        {
            Collider hitCol = hitResults[i].collider;
            if (hitCol.CompareTag("Enemy"))
            {
                EnemyBase enemyScript = hitCol.GetComponent<EnemyBase>();
                if (enemyScript == null) enemyScript = hitCol.GetComponentInParent<EnemyBase>();

                if (enemyScript != null && !hitEnemies.Contains(enemyScript))
                {
                    hitEnemies.Add(enemyScript);

                    // --- ÉËº¦¼ÆËã ---
                    bool isCrit = false;
                    float finalDamage = actualDamage * currentDamageMult;

                    // 1. ¼õËÙÔöÉË
                    if (hasSlowExtraDamage && enemyScript.IsSlowed())
                    {
                        finalDamage *= (1f + slowExtraDamageMult);
                    }

                    // 2. ±©»÷
                    if (critRate > 0f && Random.value <= critRate)
                    {
                        isCrit = true;
                        finalDamage *= (1f + critDamage);
                    }

                    enemyScript.TakeDamage(finalDamage, transform.position, isCrit);

                    if (isSlowBullet) enemyScript.ApplySlow(slowAmount, slowDuration);

                    if (VFXManager.Instance != null) VFXManager.Instance.PlayVFX(VFXType.HitSpark, hitCol.transform.position, Quaternion.identity);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.EnemyHit);

                    // --- ´©Í¸ÅÐ¶¨ ---
                    if (pierceCount > 0)
                    {
                        pierceCount--;
                        currentDamageMult *= (1f - pierceFalloff); // ÉËº¦Ë¥¼õ
                    }
                    else
                    {
                        Recycle();
                        return;
                    }
                }
            }
        }
    }

    private void Recycle()
    {
        hitEnemies.Clear();
        ObjectPoolManager.Instance.ReturnObject(this.gameObject);
    }
}