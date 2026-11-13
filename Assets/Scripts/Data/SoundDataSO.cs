using UnityEngine;

public enum SoundType
{
    BGM,
    PlayerShoot,
    PlayerHit,
    EnemyHit,
    BulletHitWall,
    EnemyDeath,
    EnemySpawn,
    EnemyMove,
    PlayerMove,
    ScoreMilestone,
    UIClick,
    BombExplode,
    GameVictory,
    GameDefeat,
    EnemyShoot,
    Combo,
    TrapHit,
    TrapSlow,
    TrapSpeed,
    BossBGM,
    BossWarning,
    BossAppear,
    EnemyCharge,
    EnemyDash,
    EnemyHitPlayer,
    EnemyCountdown,
    PlayerDeath,      
    GameOver,
    ItemPickup,
    ShieldHitBullet, // 挡子弹
    ShieldHitEnemy
}

[CreateAssetMenu(fileName = "NewSoundData", menuName = "Game/Sound Data")]
public class SoundDataSO : ScriptableObject
{
    public SoundType soundType;
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;

    [Header("音调随机 (防止同一种声音重复听腻，设为1则无变化)")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
}