using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlSpeed : MonoBehaviour
{
    public static ControlSpeed Instance { get; private set; }

    [Range(1f, 3f)]
    public float speed = 2f;

    void Awake()
    {
        // 處理 DontDestroyOnLoad 的重複檢查邏輯
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        UpdateSimulationSpeed();
    }

    // 當你在 Unity Inspector 中拖動滑條時，會自動觸發這個函數
    private void OnValidate()
    {
        // 確保在遊戲運行中（Playing）修改滑條才會即時生效
        if (Application.isPlaying)
        {
            UpdateSimulationSpeed();
        }
    }

    // 抽出一個統一修改速度的方法
    private void UpdateSimulationSpeed()
    {
        Time.timeScale = speed;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        
        // Debug.Log($"當前模擬速度已更新為: {speed} 倍速");
    }
}