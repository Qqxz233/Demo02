using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthSliderUI : MonoBehaviour
{
    public Slider healthSlider;
    public float hideDelay = 3f;

    [Header("跟随位置偏移 (正下方)")]
    public Vector3 offset = new Vector3(0, -1.5f, 0); // 根据你的模型高度调整

    private EnemyBase enemy;
    private float lastHitTime;
    private float maxHealthCache;

    private void Start()
    {
        enemy = GetComponentInParent<EnemyBase>();
        if (enemy != null) maxHealthCache = enemy.GetMaxHealth();
        gameObject.SetActive(false);
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        gameObject.SetActive(true);
        lastHitTime = Time.time;
        if (healthSlider != null) healthSlider.value = currentHealth / maxHealth;
    }

    private void LateUpdate()
    {
        // 【核心修复】：不要作为子物体跟随，直接在每帧更新其世界坐标到敌人下方
        if (enemy != null)
        {
            transform.position = enemy.transform.position + offset;
        }

        // 始终面向摄像机
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }

        if (gameObject.activeSelf && Time.time - lastHitTime > hideDelay)
        {
            gameObject.SetActive(false);
        }
    }
}