using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Summoner : MonoBehaviour
{
    [SerializeField] private List<GameObject> enemyList = new List<GameObject>();
    [SerializeField] private float space = 1f;
    
    [Header("生成範圍設定")]
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;

    void Start()
    {
        SummonEnemiesOnGrid();
    }

    void SummonEnemiesOnGrid()
    {
        if (enemyList == null || enemyList.Count == 0)
        {
            Debug.LogWarning("Enemy List 是空的！");
            return;
        }

        // 1. 計算出範圍內所有合法的格子點
        List<Vector2> availableSlots = GetAllGridSlots();

        if (availableSlots.Count < enemyList.Count)
        {
            Debug.LogWarning($"警告：格子總數 ({availableSlots.Count}) 小於想生成的怪物數量 ({enemyList.Count})，部分怪物將無法生成！");
        }

        // 2. 開始依序為每隻怪物隨機挑選格子
        foreach (GameObject enemyPrefab in enemyList)
        {
            if (enemyPrefab == null) continue;
            if (availableSlots.Count == 0) break; // 沒格子可用了，提早結束

            // 隨機抽一個格子的索引 (還記得我們第一題學的 Random.Range 嗎？最大值不包含)
            int randomIndex = Random.Range(0, availableSlots.Count);
            Vector2 spawnPosition = availableSlots[randomIndex];

            // 3. 在該格子生成怪物
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            // 4. 重要：把這格從「可用清單」中移除，這樣下一隻怪物就不會抽到同一格！
            availableSlots.RemoveAt(randomIndex);
        }
    }

    // 核心邏輯：用 double for 迴圈把所有格子點抓出來
    private List<Vector2> GetAllGridSlots()
    {
        List<Vector2> slots = new List<Vector2>();

        // 從 xMin 開始，每次增加一個 space，直到 xMax
        for (float x = xMin; x <= xMax; x += space)
        {
            // 從 yMin 開始，每次增加一個 space，直到 yMax
            for (float y = yMin; y <= yMax; y += space)
            {
                Vector2 targetSlot = new Vector2(x, y);

                // 【選用】物理碰撞檢查：如果這格已經有障礙物或牆壁，就不把它當作可用格子
                Collider2D hit = Physics2D.OverlapCircle(targetSlot, space * 0.4f); // 半徑稍微縮小一點避免誤判
                if (hit != null)
                {
                    continue; // 這裡有東西，跳過這格
                }

                slots.Add(targetSlot);
            }
        }
    
        return slots;
    }
}