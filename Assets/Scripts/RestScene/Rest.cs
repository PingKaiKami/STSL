using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Rest : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    public void Recovery()
    {
        // 呼叫 HandManager 執行比例恢復
        HandManager.Instance.HealAllCardsPercentage(0.5f); // 傳入 0.5 代表 50%
        
        Destroy(gameObject);
    }
}