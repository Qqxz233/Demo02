using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeCard", menuName = "Game/Upgrade Card")]
public class UpgradeCardSO : ScriptableObject
{
    [Header("UI展示")]
    public string cardName = "新卡牌";
    public string description = "卡牌效果描述";
    public Sprite icon;

    [Header("卡牌树逻辑 (前置条件)")]
    public string prerequisiteCardName = "";

    [Header("基础数值加成")]
    public float damageMultiplier = 0f;
    public float fireRateIntervalChange = 0f;
    public int spreadCountBonus = 0;
    public float critRateBonus = 0f;
    public float critDamageBonus = 0f;

    [Header("子弹飞行速度加成")]
    [Tooltip("增加子弹飞行速度 (例如填 10，则子弹速度 +10)")]
    public float bulletSpeedAdd = 0f;

    [Header("1. 对减速敌人的额外伤害")]
    public bool enableSlowExtraDamage = false;
    [Tooltip("额外伤害倍率，例如填 0.5 代表对减速敌人多造成 50% 伤害")]
    public float slowExtraDamageMultiplier = 0f;

    [Header("2. 子弹尺寸")]
    [Tooltip("增加子弹体积，例如填 0.2 代表尺寸增加 20%")]
    public float bulletSizeAdd = 0f;

    [Header("3. 子弹穿透")]
    public bool enablePierce = false;
    [Tooltip("增加穿透数量（每次穿透消耗1次）")]
    public int pierceCountAdd = 0;
    [Tooltip("穿透后的伤害衰减比例（例如填 0.2 代表穿透一次后伤害减少 20%）")]
    [Range(0f, 0.9f)] public float pierceDamageFalloff = 0.2f;

    [Header("护盾专属属性")]
    public int shieldCountAdd = 0;
    public float shieldSizeAdd = 0f;
    public float shieldRotationSpeedAdd = 0f;
    public bool enableShieldReflect = false;
    public bool enableShieldDamage = false;
    public float shieldDamageAdd = 0f;

    [Header("减速炮弹专属")]
    public bool isSlowBullet = false;
    public float slowDamageMultiplier = 1.0f;
    [Range(0.1f, 1f)] public float slowAmount = 0.5f;
    public float slowDuration = 2f;

    [Header("视觉特效 (拖尾)")]
    public GameObject bulletTrailPrefab;
}