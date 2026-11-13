using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("敌方子弹属性")]
    public float speed = 12f;
    public float damage = 15f;
    public float lifeTime = 5f;
    public float hitRadius = 0.3f;

    private float currentLife = 0f;
    private Vector3 moveDirection;

    private int targetLayerMask;
    private int wallLayerIndex;

    // 射线检测数组 (用于 SphereCast)
    private RaycastHit[] hitResults = new RaycastHit[5];
    // 【修复 1】：碰撞体检测数组 (用于 OverlapSphere)
    private Collider[] overlapResults = new Collider[5];

    public void Init(Vector3 dir, float setDamage, GameObject shooter = null)
    {
        moveDirection = dir.normalized;
        moveDirection.y = 0;
        damage = setDamage;
        currentLife = 0f;

        targetLayerMask = LayerMask.GetMask("Player", "Wall");
        wallLayerIndex = LayerMask.NameToLayer("Wall");
    }

    // 【修复 2】：补上供护盾反弹子弹使用的方向获取方法
    public Vector3 GetDirection()
    {
        return moveDirection;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        Vector3 oldPos = transform.position;
        transform.position += moveDirection * speed * Time.deltaTime;

        currentLife += Time.deltaTime;
        if (currentLife >= lifeTime)
        {
            Recycle();
            return;
        }

        CheckContinuousCollision(oldPos);
    }

    private void CheckContinuousCollision(Vector3 oldPos)
    {
        Vector3 moveVec = transform.position - oldPos;
        float moveDistance = moveVec.magnitude;

        if (moveDistance <= 0) return;

        // 1. 射线扫描路径上的碰撞体（包含 Trigger）
        int numHits = Physics.SphereCastNonAlloc(oldPos, hitRadius, moveDirection, hitResults, moveDistance, targetLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < numHits; i++)
        {
            Collider hitCol = hitResults[i].collider;

            // 碰到墙
            if (hitCol.gameObject.layer == wallLayerIndex)
            {
                if (VFXManager.Instance != null)
                    VFXManager.Instance.PlayVFX(VFXType.HitSpark, hitResults[i].point, Quaternion.LookRotation(hitResults[i].normal));
                Recycle();
                return;
            }

            // 碰到玩家
            if (hitCol.CompareTag("Player"))
            {
                PlayerHealth player = hitCol.GetComponent<PlayerHealth>();
                if (player == null) player = hitCol.GetComponentInParent<PlayerHealth>();

                if (player != null)
                {
                    player.TakeDamage(damage, transform.position);
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(SoundType.PlayerHit);
                }
                Recycle();
                return;
            }
        }

        // 2. 兜底检测（使用正确的 overlapResults 数组）
        int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, hitRadius, overlapResults, targetLayerMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider hitCol = overlapResults[i];
            if (hitCol.CompareTag("Player"))
            {
                PlayerHealth player = hitCol.GetComponent<PlayerHealth>();
                if (player == null) player = hitCol.GetComponentInParent<PlayerHealth>();

                if (player != null)
                {
                    player.TakeDamage(damage, transform.position);
                }
                Recycle();
                return;
            }
        }
    }

    public void Recycle()
    {
        ObjectPoolManager.Instance.ReturnObject(this.gameObject);
    }
}