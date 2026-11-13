using UnityEngine;

public enum VFXType
{
    MuzzleFlash, HitSpark, EnemyDeath, EnemySpawn, ComboBurst, ScoreMilestone, UpgradeGlow, UIClick,
    BombExplosion,
    TrapHit, TrapSlow, TrapSpeed // 新增：机关特效
}

[CreateAssetMenu(fileName = "NewVFXData", menuName = "Game/VFX Data")]
public class VFXDataSO : ScriptableObject
{
    public VFXType vfxType;
    public GameObject vfxPrefab;
    public float lifeTime = 2f;
    public bool followTarget = false;
}