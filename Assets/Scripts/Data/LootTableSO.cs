using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory
{
    Healing,     // 回血类
    Offensive,   // 攻击、移速、射速类 (玩家血多时易掉)
    Defensive    // 无敌、护盾类 (玩家血多时易掉)
}

[System.Serializable]
public class LootDropItem
{
    public GameObject itemPrefab;
    public ItemCategory category;    // 【新增】：物品分类
    public float baseWeight = 10f;   // 【修改】：将概率改为权重制 (权重越大概率越高)
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Game/Loot Table")]
public class LootTableSO : ScriptableObject
{
    public List<LootDropItem> lootItems = new List<LootDropItem>();
}