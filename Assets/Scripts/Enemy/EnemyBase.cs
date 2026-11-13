using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    [Header("数据配置")]
    public EnemyConfigSO config;

    [Header("技能列表 (拖入配置好的SO)")]
    public List<EnemySkillSO> skills;

    [Header("视觉与动画")]
    public Transform modelTransform;
    public Animator animator;

    [Header("炮台引用 (逻辑分离)")]
    public EnemyTurret enemyTurret;

    [Header("身体转向与惯性参数")]
    public float bodyTurnSpeed = 3f;
    public float modelRotationOffset = 0f;
    public float enemyAcceleration = 15f;
    public float enemyAngularSpeed = 120f;

    [Header("移动拖尾/粒子特效")]
    public GameObject customMoveVFX;
    public Color trailStartColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    public Color trailEndColor = new Color(1f, 0.2f, 0.2f, 0f);
    public float trailTime = 0.2f;
    public float trailStartWidth = 0.4f;

    [Header("低血量循环特效")]
    public GameObject lowHealthVFX;
    public GameObject criticalHealthVFX;

    [Header("UI与死亡特效")]
    public EnemyHealthSliderUI healthUI;
    public GameObject deathCrossPrefab;

    [Header("爆炸技能闪红部件")]
    public Renderer[] explodeFlashRenderers;
    private Color[] explodeOriginalColors;

    [Header("受击反馈参数")]
    public float hitShakeDuration = 0.1f;
    public float hitShakeMagnitude = 0.15f;
    public float knockbackForce = 2f;
    public float hitStopDuration = 0.03f;

    [Header("受击模型晃动参数")]
    public float modelShakeDuration = 0.2f;
    public float modelShakeStrength = 0.3f;

    [Header("死亡专属震屏")]
    public float deathShakeDuration = 0.35f;
    public float deathShakeMagnitude = 0.5f;

    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float waveHealthMultiplier = 1f;
    [HideInInspector] public float waveDamageMultiplier = 1f;

    protected enum EnemyState { Chase, Wander, Retreat, Casting, Dashing }
    protected EnemyState currentState = EnemyState.Chase;

    protected float currentHealth;
    private float totalMaxHealth;
    protected Vector3 knockbackOffset = Vector3.zero;
    protected Renderer[] allRenderers;
    private MaterialPropertyBlock mpb;
    private TrailRenderer moveTrail;
    private GameObject activeMoveVFX;
    private bool isCurrentlySlowed = false;
    private float currentSlowMultiplier = 1f;
    private float slowTimer = 0f;

    private Coroutine flashCoroutine;
    private Coroutine hitShakeCoroutine;
    private Vector3 originalModelLocalPos;

    protected Transform playerTransform;
    protected PlayerHealth playerHealth;
    private NavMeshAgent agent;

    private Dictionary<EnemySkillSO, float> skillCooldowns = new Dictionary<EnemySkillSO, float>();
    private float globalSkillCooldown = 0f;
    private float wanderTimer = 0f;
    private int wanderDirSign = 1;

    private Collider[] hitColliders = new Collider[10];
    private bool isShooting = false;
    private int playerLayerMask;
    private int wallLayerMask;

    private Vector3 deathPositionCache;
    private AudioSource localAudioSource;

    protected virtual void Start()
    {
        if (config == null) return;

        currentHealth = config.maxHealth * waveHealthMultiplier;
        totalMaxHealth = currentHealth;

        allRenderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        playerLayerMask = LayerMask.GetMask("Player");
        wallLayerMask = LayerMask.GetMask("Wall");

        if (explodeFlashRenderers == null || explodeFlashRenderers.Length == 0)
            explodeFlashRenderers = allRenderers;

        explodeOriginalColors = new Color[explodeFlashRenderers.Length];
        for (int i = 0; i < explodeFlashRenderers.Length; i++)
        {
            if (explodeFlashRenderers[i] != null && explodeFlashRenderers[i].material.HasProperty("_Color"))
                explodeOriginalColors[i] = explodeFlashRenderers[i].material.color;
        }

        EnemyManager.Instance.RegisterEnemy(this.transform);

        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
            playerHealth = playerTransform.GetComponent<PlayerHealth>();
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = config.moveSpeed;
            agent.updateRotation = false;
            agent.acceleration = enemyAcceleration;
            agent.angularSpeed = enemyAngularSpeed;
        }

        localAudioSource = gameObject.AddComponent<AudioSource>();
        localAudioSource.playOnAwake = false;
        localAudioSource.spatialBlend = 0f;

        foreach (var skill in skills) skillCooldowns[skill] = 0f;

        InitMoveVFX();

        if (modelTransform != null)
            originalModelLocalPos = modelTransform.localPosition;

        if (lowHealthVFX != null) lowHealthVFX.SetActive(false);
        if (criticalHealthVFX != null) criticalHealthVFX.SetActive(false);
    }

    public void ApplySlow(float amount, float duration)
    {
        if (amount >= 1f) return;
        currentSlowMultiplier = Mathf.Min(currentSlowMultiplier, amount);
        slowTimer = Mathf.Max(slowTimer, duration);
        UpdateSpeedVisuals();
    }

    public bool IsSlowed() { return currentSlowMultiplier < 0.99f || speedMultiplier < 0.99f; }
    public float GetMaxHealth() { return totalMaxHealth; }
    public float GetHealthPercent() { return currentHealth / totalMaxHealth; }

    private void InitMoveVFX()
    {
        if (customMoveVFX != null)
        {
            activeMoveVFX = Instantiate(customMoveVFX, transform);
            activeMoveVFX.transform.localPosition = Vector3.zero;
            activeMoveVFX.SetActive(false);
        }
        else
        {
            moveTrail = gameObject.AddComponent<TrailRenderer>();
            moveTrail.material = new Material(Shader.Find("Sprites/Default"));
            moveTrail.time = trailTime;
            moveTrail.startWidth = trailStartWidth;
            moveTrail.endWidth = 0f;
            moveTrail.startColor = trailStartColor;
            moveTrail.endColor = trailEndColor;
            moveTrail.emitting = false;
        }
    }

    protected virtual void Update()
    {
        if (config == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f) currentSlowMultiplier = 1f;
        }

        UpdateSpeedVisuals();
        UpdateMoveVFX();
        UpdateCooldowns();

        if (globalSkillCooldown > 0) globalSkillCooldown -= Time.deltaTime;

        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (currentState != EnemyState.Dashing && !isShooting)
        {
            if (currentState != EnemyState.Casting && globalSkillCooldown <= 0 && TryCastSkill(distToPlayer))
                return;
        }

        if (currentState != EnemyState.Dashing && (currentState != EnemyState.Casting || isShooting))
        {
            HandleMovement(distToPlayer);
        }

        HandleRotation();
        CheckPlayerCollision();
    }

    private void HandleRotation()
    {
        if (currentState == EnemyState.Dashing) return;
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f && modelTransform != null)
        {
            Vector3 moveDir = agent.velocity.normalized;
            moveDir.y = 0;
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0, modelRotationOffset, 0);
                modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRot, Time.deltaTime * bodyTurnSpeed);
            }
        }
    }

    private void UpdateMoveVFX()
    {
        bool isMoving = agent != null && agent.velocity.sqrMagnitude > 0.1f;
        if (currentState == EnemyState.Casting && !isShooting) isMoving = false;
        if (currentState == EnemyState.Dashing) isMoving = true;

        if (moveTrail != null) moveTrail.emitting = isMoving;
        if (activeMoveVFX != null) activeMoveVFX.SetActive(isMoving);
    }

    private void HandleMovement(float distToPlayer)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float finalMoveSpeed = config.moveSpeed * speedMultiplier * currentSlowMultiplier;
        float finalWanderSpeed = config.wanderSpeed * speedMultiplier * currentSlowMultiplier;

        if (distToPlayer > config.maxFollowDistance)
        {
            currentState = EnemyState.Chase;
            agent.speed = finalMoveSpeed;
            agent.SetDestination(playerTransform.position);
        }
        else if (distToPlayer < config.minFollowDistance)
        {
            currentState = EnemyState.Retreat;
            agent.speed = finalWanderSpeed;
            Vector3 dirAway = (transform.position - playerTransform.position).normalized;
            dirAway.y = 0;
            agent.SetDestination(transform.position + dirAway * 3f);
        }
        else
        {
            currentState = EnemyState.Wander;
            agent.speed = finalWanderSpeed;
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderDirSign = Random.value > 0.5f ? 1 : -1;
                wanderTimer = Random.Range(1f, 2.5f);
            }
            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
            Vector3 tangentDir = Vector3.Cross(Vector3.up, dirToPlayer) * wanderDirSign;
            Vector3 wanderTarget = transform.position + tangentDir * 3f + dirToPlayer * 1.5f;
            agent.SetDestination(wanderTarget);
        }
    }

    private void UpdateCooldowns()
    {
        List<EnemySkillSO> keys = new List<EnemySkillSO>(skillCooldowns.Keys);
        foreach (var key in keys)
        {
            if (skillCooldowns[key] > 0) skillCooldowns[key] -= Time.deltaTime;
        }
    }

    private bool TryCastSkill(float distToPlayer)
    {
        List<EnemySkillSO> availableSkills = new List<EnemySkillSO>();
        int totalWeight = 0;

        foreach (var skill in skills)
        {
            if (skillCooldowns[skill] <= 0 && distToPlayer <= skill.castRange)
            {
                availableSkills.Add(skill);
                totalWeight += skill.weight;
            }
        }

        if (availableSkills.Count == 0 || totalWeight <= 0) return false;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        EnemySkillSO chosenSkill = null;

        foreach (var skill in availableSkills)
        {
            currentWeight += skill.weight;
            if (randomValue < currentWeight) { chosenSkill = skill; break; }
        }

        StartCoroutine(ExecuteSkill(chosenSkill));
        return true;
    }

    private IEnumerator ExecuteSkill(EnemySkillSO skill)
    {
        skillCooldowns[skill] = skill.cooldown;
        globalSkillCooldown = skill.globalCooldown + 0.5f;

        if (skill.skillType == SkillType.Shoot)
        {
            isShooting = true;
            if (animator != null) animator.SetTrigger("Shoot");
            if (enemyTurret != null) enemyTurret.StartShooting(skill, skill.enemyBulletPrefab, config.collisionDamage, waveDamageMultiplier);

            float waitTime = skill.burstCount * skill.burstInterval + 1.0f;
            yield return new WaitForSeconds(waitTime);
            isShooting = false;
        }
        else if (skill.skillType == SkillType.Dash)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            currentState = EnemyState.Casting;

            if (animator != null) animator.SetTrigger("Charge");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.EnemyCharge);

            yield return new WaitForSeconds(skill.chargeTime);

            Vector3 dashDir = (playerTransform.position - transform.position).normalized;
            dashDir.y = 0;
            currentState = EnemyState.Dashing;

            if (animator != null) animator.SetTrigger("Dash");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.EnemyDash);

            float timer = 0f;
            bool hasHitPlayer = false;

            while (timer < skill.dashDuration)
            {
                if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) break;

                Vector3 oldPos = transform.position;
                transform.position += dashDir * skill.dashSpeed * Time.deltaTime;
                timer += Time.deltaTime;

                // 【恢复最初逻辑】：撞墙直接停下并稍微拉回，不瞬移
                Vector3 moveVec = transform.position - oldPos;
                if (moveVec.magnitude > 0)
                {
                    if (Physics.SphereCast(oldPos, 0.5f, dashDir, out RaycastHit wallHit, moveVec.magnitude, wallLayerMask))
                    {
                        transform.position = wallHit.point - dashDir * 0.5f;
                        break;
                    }
                }

                if (!hasHitPlayer)
                {
                    int numHits = Physics.OverlapSphereNonAlloc(transform.position, skill.dashHitRadius, hitColliders, playerLayerMask, QueryTriggerInteraction.Collide);
                    if (numHits > 0)
                    {
                        hasHitPlayer = true;
                        PlayerHealth player = hitColliders[0].GetComponent<PlayerHealth>();
                        if (player == null) player = hitColliders[0].GetComponentInParent<PlayerHealth>();

                        if (player != null)
                        {
                            player.TakeDamage(skill.dashDamage * waveDamageMultiplier, transform.position);
                            if (PlayerController.Instance != null) PlayerController.Instance.ApplyKnockback(dashDir * skill.dashKnockbackForce, 0.6f);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.EnemyHitPlayer);
                        break;
                    }
                }
                yield return null;
            }

            currentState = EnemyState.Chase;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }
        else if (skill.skillType == SkillType.Explode)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            currentState = EnemyState.Casting;
            yield return StartCoroutine(ExplodeRoutine(skill));
            currentState = EnemyState.Chase;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator ExplodeRoutine(EnemySkillSO skill)
    {
        float timer = skill.countdownDuration;
        speedMultiplier = skill.countdownSpeedMultiplier;

        if (localAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip countdownClip = AudioManager.Instance.GetSoundClip(SoundType.EnemyCountdown);
            if (countdownClip != null)
            {
                localAudioSource.clip = countdownClip;
                localAudioSource.loop = true;
                localAudioSource.Play();
            }
        }

        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;

        while (timer > 0f)
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) { yield return null; continue; }

            timer -= Time.deltaTime;
            float progress = 1f - (timer / skill.countdownDuration);
            float flashSpeed = Mathf.Lerp(5f, 40f, progress);
            float flash = Mathf.PingPong(Time.time * flashSpeed, 1f);

            for (int i = 0; i < explodeFlashRenderers.Length; i++)
            {
                if (explodeFlashRenderers[i] != null && explodeFlashRenderers[i].material.HasProperty("_Color"))
                {
                    Color blinkColor = Color.Lerp(explodeOriginalColors[i], Color.red, flash);
                    explodeFlashRenderers[i].material.color = blinkColor;
                }
            }

            if (localAudioSource != null && localAudioSource.isPlaying)
                localAudioSource.pitch = Mathf.Lerp(1.0f, 2.0f, progress);

            if (playerTransform != null && agent != null && agent.isOnNavMesh)
                agent.SetDestination(playerTransform.position);

            yield return null;
        }

        if (localAudioSource != null) localAudioSource.Stop();

        for (int i = 0; i < explodeFlashRenderers.Length; i++)
        {
            if (explodeFlashRenderers[i] != null && explodeFlashRenderers[i].material.HasProperty("_Color"))
                explodeFlashRenderers[i].material.color = explodeOriginalColors[i];
        }
        speedMultiplier = 1f;

        int numHits = Physics.OverlapSphereNonAlloc(transform.position, skill.explosionRadius, hitColliders, ~0, QueryTriggerInteraction.Collide);
        float finalExplosionDamage = skill.explosionDamage * waveDamageMultiplier;

        for (int i = 0; i < numHits; i++)
        {
            Collider hit = hitColliders[i];
            if (hit == null || hit.gameObject == gameObject) continue;
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null && enemy != this) enemy.TakeDamage(finalExplosionDamage, transform.position);

            PlayerHealth player = hit.GetComponent<PlayerHealth>();
            if (player == null) player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null) player.TakeDamage(finalExplosionDamage, transform.position);
        }

        if (VFXManager.Instance != null) VFXManager.Instance.PlayVFX(VFXType.BombExplosion, transform.position, Quaternion.identity);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(SoundType.BombExplode);
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.4f, 0.8f);

        if (skill.deathAfterExplode) { currentHealth = 0; Die(true); }
    }

    protected virtual void CheckPlayerCollision()
    {
        if (currentState == EnemyState.Dashing || isShooting) return;
        if (playerHealth == null || playerHealth.IsDead()) return;

        float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
        if (sqrDist <= 0.8f * 0.8f)
        {
            playerHealth.TakeDamage(config.collisionDamage * waveDamageMultiplier, transform.position);
            Die();
        }
    }

    public virtual void TakeDamage(float damage, Vector3 hitSourcePos, bool isCrit = false)
    {
        if (currentHealth <= 0) return;
        currentHealth -= damage;

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(hitShakeDuration, hitShakeMagnitude);
        Vector3 dir = (transform.position - hitSourcePos).normalized;
        knockbackOffset = dir * (knockbackForce * 0.1f);
        if (TimeManager.Instance != null) TimeManager.Instance.TriggerHitStop(hitStopDuration);

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashWhiteRoutine());

        if (modelTransform != null)
        {
            if (hitShakeCoroutine != null) StopCoroutine(hitShakeCoroutine);
            hitShakeCoroutine = StartCoroutine(HitShakeRoutine(modelShakeDuration, modelShakeStrength));
        }

        if (FloatingTextManager.Instance != null) FloatingTextManager.Instance.ShowDamageText(Mathf.CeilToInt(damage), transform.position + Vector3.up * 1.5f, isCrit);
        if (TotalScoreUIManager.Instance != null) TotalScoreUIManager.Instance.AddDamage(damage);
        if (healthUI != null) healthUI.UpdateHealthBar(currentHealth, totalMaxHealth);

        UpdateHealthVFX();

        if (currentHealth <= 0) Die();
    }

    private IEnumerator HitShakeRoutine(float duration, float strength)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float damper = 1f - (timer / duration);
            float yOffset = Random.Range(-1f, 1f) * strength * damper;
            float zOffset = Random.Range(-1f, 1f) * strength * damper;
            modelTransform.localPosition = originalModelLocalPos + new Vector3(0f, yOffset, zOffset);
            yield return null;
        }
        modelTransform.localPosition = originalModelLocalPos;
        hitShakeCoroutine = null;
    }

    private void UpdateHealthVFX()
    {
        if (totalMaxHealth <= 0) return;
        float hpPercent = currentHealth / totalMaxHealth;
        if (lowHealthVFX != null) { bool showLow = hpPercent <= 0.5f; if (lowHealthVFX.activeSelf != showLow) lowHealthVFX.SetActive(showLow); }
        if (criticalHealthVFX != null) { bool showCrit = hpPercent <= 0.2f; if (criticalHealthVFX.activeSelf != showCrit) criticalHealthVFX.SetActive(showCrit); }
    }

    protected virtual void Die(bool isSelfDestruct = false)
    {
        if (currentHealth <= -9000) return;
        currentHealth = -9999;

        if (localAudioSource != null)
        {
            localAudioSource.Stop();
            localAudioSource.loop = false;
            localAudioSource.clip = null;
        }

        deathPositionCache = transform.position;

        if (hitShakeCoroutine != null) { StopCoroutine(hitShakeCoroutine); hitShakeCoroutine = null; }
        if (modelTransform != null) modelTransform.localPosition = originalModelLocalPos;

        if (lowHealthVFX != null) lowHealthVFX.SetActive(false);
        if (criticalHealthVFX != null) criticalHealthVFX.SetActive(false);

        if (!isSelfDestruct)
        {
            if (deathCrossPrefab != null)
            {
                Vector3 spawnPos = deathPositionCache + Vector3.up * 1f;
                Instantiate(deathCrossPrefab, spawnPos, Quaternion.identity);
            }
            if (ComboManager.Instance != null) ComboManager.Instance.AddCombo(transform.position);
        }

        TryDropLoot();

        if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyKilled();
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(deathShakeDuration, deathShakeMagnitude);

        StartCoroutine(DieRoutine());
    }

    private void TryDropLoot()
    {
        if (config.lootTable == null || config.lootTable.lootItems.Count == 0) return;

        float dropRoll = UnityEngine.Random.Range(0f, 100f);
        if (dropRoll > config.dropChance) return;

        int randomIndex = UnityEngine.Random.Range(0, config.lootTable.lootItems.Count);
        GameObject itemPrefab = config.lootTable.lootItems[randomIndex].itemPrefab;

        if (itemPrefab == null) return;

        Vector3 dropPos = deathPositionCache;
        GameObject droppedItem = Instantiate(itemPrefab, dropPos, Quaternion.identity);
        StartCoroutine(ForcePositionRoutine(droppedItem, dropPos));
    }

    private IEnumerator ForcePositionRoutine(GameObject item, Vector3 lockedPos)
    {
        float timer = 0f;
        while (timer < 1f)
        {
            if (item != null) item.transform.position = lockedPos;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    protected virtual IEnumerator DieRoutine()
    {
        EnemyManager.Instance.UnregisterEnemy(this.transform);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        if (moveTrail != null) moveTrail.emitting = false;
        if (activeMoveVFX != null) activeMoveVFX.SetActive(false);

        SetFlashAmount(1f);
        yield return new WaitForSecondsRealtime(0.1f);
        Destroy(gameObject);
    }

    private IEnumerator FlashWhiteRoutine()
    {
        SetFlashAmount(1f);
        yield return new WaitForSecondsRealtime(config.hitFlashDuration);
        SetFlashAmount(0f);
        if (isCurrentlySlowed) ApplyColorToRenderers(new Color(0.5f, 0.8f, 1f));
        flashCoroutine = null;
    }

    private void SetFlashAmount(float amount)
    {
        foreach (var rend in allRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(mpb);
            mpb.SetFloat("_FlashAmount", amount);
            rend.SetPropertyBlock(mpb);
        }
    }

    private void UpdateSpeedVisuals()
    {
        bool isSlowed = currentSlowMultiplier < 0.99f || speedMultiplier < 0.99f;
        bool isHasted = speedMultiplier > 1.01f;

        if (isHasted)
        {
            if (moveTrail != null) moveTrail.emitting = true;
            if (isCurrentlySlowed) { SetFlashAmount(0f); isCurrentlySlowed = false; }
        }
        else if (isSlowed)
        {
            if (moveTrail != null) moveTrail.emitting = false;
            if (!isCurrentlySlowed && flashCoroutine == null)
            {
                ApplyColorToRenderers(new Color(0.5f, 0.8f, 1f));
                isCurrentlySlowed = true;
            }
        }
        else
        {
            if (moveTrail != null) moveTrail.emitting = false;
            if (isCurrentlySlowed)
            {
                ApplyColorToRenderers(Color.white);
                isCurrentlySlowed = false;
            }
        }
    }

    private void ApplyColorToRenderers(Color color)
    {
        foreach (var rend in allRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            rend.SetPropertyBlock(mpb);
        }
    }
}