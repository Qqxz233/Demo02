using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("玩家属性")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("动画与结算设置")]
    public Animator animator;
    public float gameOverDelay = 2.0f;

    [Header("低血量循环特效")]
    [Tooltip("血量低于50%时启用的特效物体")]
    public GameObject lowHealthVFX;
    [Tooltip("血量低于20%时启用的特效物体")]
    public GameObject criticalHealthVFX;

    [Header("受击反馈设置")]
    public float invincibleDuration = 1.5f;
    public float hitShakeDuration = 0.3f;
    public float hitShakeMagnitude = 0.5f;
    public float hitKnockbackForce = 10f;
    public float hitStopDuration = 0.05f;

    private bool isInvincible = false;
    private Renderer[] allRenderers;
    private Rigidbody rb;

    private void Start()
    {
        currentHealth = maxHealth;
        allRenderers = GetComponentsInChildren<Renderer>();
        rb = GetComponent<Rigidbody>();

        // 【核心新增】：开局默认关闭低血量特效
        if (lowHealthVFX != null) lowHealthVFX.SetActive(false);
        if (criticalHealthVFX != null) criticalHealthVFX.SetActive(false);
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    public void Heal(float amount)
    {
        if (IsDead() || amount <= 0) return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        float actualHealed = currentHealth - oldHealth;

        if (actualHealed > 0 && FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowText("+" + Mathf.CeilToInt(actualHealed), Color.green, transform.position + Vector3.up * 2f);
        }

        UpdateHealthVFX(); // 【新增】：回血后刷新特效状态
    }

    public void TakeDamage(float damage, Vector3 damageSourcePos)
    {
        if (isInvincible || IsDead() || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        currentHealth -= damage;

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(hitShakeDuration, hitShakeMagnitude);
        if (TimeManager.Instance != null) TimeManager.Instance.TriggerHitStop(hitStopDuration, 0.1f);
        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashRed();

        if (PlayerController.Instance != null)
        {
            Vector3 knockbackDir = (transform.position - damageSourcePos).normalized;
            knockbackDir.y = 0;
            PlayerController.Instance.ApplyKnockback(knockbackDir * hitKnockbackForce, 0.4f);
        }

        UpdateHealthVFX(); // 【新增】：受伤后刷新特效状态

        if (currentHealth <= 0) Die();
        else StartCoroutine(InvincibilityRoutine());
    }

    // 【核心新增】：根据血量控制特效
    private void UpdateHealthVFX()
    {
        float hpPercent = GetHealthPercent();

        if (lowHealthVFX != null)
        {
            // 血量低于 50% 开启
            bool showLow = hpPercent <= 0.5f;
            if (lowHealthVFX.activeSelf != showLow)
                lowHealthVFX.SetActive(showLow);
        }

        if (criticalHealthVFX != null)
        {
            // 血量低于 20% 开启
            bool showCrit = hpPercent <= 0.2f;
            if (criticalHealthVFX.activeSelf != showCrit)
                criticalHealthVFX.SetActive(showCrit);
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float blinkDuration = 0.1f;
        float endTime = Time.time + invincibleDuration;

        while (Time.time < endTime)
        {
            foreach (var rend in allRenderers) rend.enabled = false;
            yield return new WaitForSeconds(blinkDuration);
            foreach (var rend in allRenderers) rend.enabled = true;
            yield return new WaitForSeconds(blinkDuration);
        }

        foreach (var rend in allRenderers) rend.enabled = true;
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("<color=red>玩家死亡，播放死亡动画！</color>");

        // 【核心新增】：刚死亡时瞬间播放音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SoundType.PlayerDeath);

        if (animator != null) animator.SetTrigger("Die");

        // 禁用操作
        if (PlayerController.Instance != null)
            PlayerController.Instance.enabled = false;

        AutoTurret turret = GetComponentInChildren<AutoTurret>();
        if (turret != null) turret.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(DeathDelayRoutine());
    }
    private IEnumerator DeathDelayRoutine()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
    }
}