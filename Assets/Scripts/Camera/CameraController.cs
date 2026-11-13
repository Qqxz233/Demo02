using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("跟随目标")]
    public Transform playerTarget;

    [Header("相机跟随设置 (游玩时)")]
    [Tooltip("相机的高度 (决定Y轴，不受玩家影响)")]
    public float cameraHeight = 25f;

    [Tooltip("相机在Z轴上的偏移 (相当于向后拉远)")]
    public float cameraZOffset = -15f;

    [Tooltip("相机预设的俯视旋转角度")]
    public Vector3 gameplayRotation = new Vector3(60f, 0f, 0f);

    [Tooltip("相机平滑跟随的时间 (值越小跟得越紧，推荐 0.1 ~ 0.3)")]
    public float smoothTime = 0.15f;

    [Header("地图边界限制 (如果不需要限制，把 mapRadius 设为 0)")]
    [Tooltip("大于0时才生效。设置地图半径，防止看到地图外")]
    public float mapRadius = 0f;

    [Header("主菜单特写视角 (绝对世界坐标)")]
    public Vector3 menuPosition = new Vector3(0, 5f, -10f);
    public Vector3 menuRotation = new Vector3(30f, 0f, 0f);

    private bool isFollowing = false;

    // SmoothDamp 内部使用的速度变量
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (playerTarget == null && PlayerController.Instance != null)
            playerTarget = PlayerController.Instance.transform;
    }

    private void LateUpdate()
    {
        if (!isFollowing || playerTarget == null) return;

        // 1. 获取玩家 X 和 Z 坐标
        float playerX = playerTarget.position.x;
        float playerZ = playerTarget.position.z;

        // 2. 计算目标位置
        float targetX = playerX;
        float targetZ = playerZ + cameraZOffset;

        // 3. 【边界限制】：如果 mapRadius > 0 才进行限制，防止把相机卡死
        if (mapRadius > 0f)
        {
            float boundsBuffer = 10f;
            float minX = -mapRadius + boundsBuffer;
            float maxX = mapRadius - boundsBuffer;
            float minZ = -mapRadius + boundsBuffer;
            float maxZ = mapRadius - boundsBuffer;

            targetX = Mathf.Clamp(targetX, minX, maxX);
            targetZ = Mathf.Clamp(targetZ, minZ, maxZ);
        }

        Vector3 targetPos = new Vector3(targetX, cameraHeight, targetZ);
        Quaternion targetRot = Quaternion.Euler(gameplayRotation);

        // 4. 【核心优化】：使用 SmoothDamp 代替 Lerp，平滑跟随且不会产生拉扯感
        // 注意这里用的是 unscaledDeltaTime，防止顿帧时相机卡顿
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        // 旋转保持固定
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.unscaledDeltaTime * 10f);
    }

    // 设置主菜单特写视角
    public void SetMenuView()
    {
        isFollowing = false;
        transform.position = menuPosition;
        transform.rotation = Quaternion.Euler(menuRotation);
    }

    // 开始运镜
    public IEnumerator CinematicRoutine()
    {
        isFollowing = false;
        velocity = Vector3.zero; // 清空速度，防止运镜结束瞬间发生弹跳

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // 计算运镜终点
        float targetX = playerTarget.position.x;
        float targetZ = playerTarget.position.z + cameraZOffset;

        if (mapRadius > 0f)
        {
            targetX = Mathf.Clamp(targetX, -mapRadius + 10f, mapRadius - 10f);
            targetZ = Mathf.Clamp(targetZ, -mapRadius + 10f, mapRadius - 10f);
        }

        Vector3 endPos = new Vector3(targetX, cameraHeight, targetZ);
        Quaternion endRot = Quaternion.Euler(gameplayRotation);

        float t = 0f;
        float duration = 2f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);
            yield return null;
        }

        isFollowing = true;
    }
}