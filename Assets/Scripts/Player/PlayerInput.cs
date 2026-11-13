using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("虚拟摇杆 (移动端)")]
    [Tooltip("拖入 Canvas 下的摇杆物体")]
    public VirtualJoystick virtualJoystick;

    public Vector2 MoveDirection { get; private set; }

    private void Update()
    {
        // 如果游戏暂停或不在游玩状态，禁止移动
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            MoveDirection = Vector2.zero;
            return;
        }

        // 1. 优先读取虚拟摇杆
        if (virtualJoystick != null && virtualJoystick.Direction != Vector2.zero)
        {
            MoveDirection = virtualJoystick.Direction;
        }
        // 2. 否则读取 PC 键盘输入
        else
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            MoveDirection = new Vector2(horizontal, vertical).normalized;
        }
    }
}