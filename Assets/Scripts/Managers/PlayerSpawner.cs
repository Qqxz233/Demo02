using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("引用设置")]
    [Tooltip("场景中提前放置的玩家（请确保在场景中默认处于禁用状态）")]
    public GameObject playerObject;
    [Tooltip("玩家出生的空物体锚点")]
    public Transform spawnAnchor;

    // 加载完毕后调用，激活并放置玩家
    public void SpawnPlayer()
    {
        if (playerObject != null && spawnAnchor != null)
        {
            // 1. 移动到锚点位置
            playerObject.transform.position = spawnAnchor.position;
            playerObject.transform.rotation = spawnAnchor.rotation;

            // 2. 激活玩家
            playerObject.SetActive(true);

            Debug.Log("<color=green>【PlayerSpawner】玩家已生成在锚点位置！</color>");
        }
        else
        {
            Debug.LogError("<color=red>【PlayerSpawner】引用丢失！请检查 Player 和 Anchor 是否拖入！</color>");
        }
    }
}