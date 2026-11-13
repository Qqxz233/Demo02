using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    // 维护当前所有存活的敌人，用于自动索敌和碰撞计算
    public List<Transform> ActiveEnemies = new List<Transform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterEnemy(Transform enemy)
    {
        if (!ActiveEnemies.Contains(enemy)) ActiveEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Transform enemy)
    {
        if (ActiveEnemies.Contains(enemy)) ActiveEnemies.Remove(enemy);
    }
}