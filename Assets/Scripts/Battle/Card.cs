using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private int originalLayer;
    [Header("移動設定")]
    public float smoothSpeed = 10f;
    private bool isDragging = false;
    private Player lastTargetPlayer;

    [Header("懸停設定")]
    public float moveUpOffset = 1.0f; // 2D 建議不要太大，1.5 ( Inspector 裡的舊數值) 很適合
    private bool isHovering = false; // // 新增：記錄滑鼠是否懸停

    [Header("移動設定")]
    public Vector3 baseLocalPosition; // 新增/修改：記錄 HandManager 分配的「基礎」位置
    private Vector3 currentTargetLocal; // 新增：計算懸停後的「實際」目標位置

    [Header("卡片設定")]
    public float health;
    public float att;
    public float def;

    void Update()
    {
        // 只有在不在拖拽時，才執行平滑移動
        if (!isDragging)
        {
            // 1. 計算最終目標位置
            currentTargetLocal = baseLocalPosition;

            // 2. 如果滑鼠懸停，將目標位置向上偏移
            if (isHovering)
            {
                currentTargetLocal += Vector3.up * moveUpOffset;
            }

            // 3. 執行平滑移動 (從當前位置移向計算出的目標)
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, 
                currentTargetLocal, 
                Time.deltaTime * smoothSpeed
            );
        }
        
        // ... 原有的拖拽偵測邏輯 ...
        if (isDragging) ScanForPlayerUnderCard();
    }
    void ScanForPlayerUnderCard()
    {
        // 將滑鼠螢幕位置轉為世界座標 (2D)
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 檢查該點位下所有的 2D 碰撞體
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);

        Player currentPlayerFound = null;

        foreach (var hit in hits)
        {
            // 偵測是否有 Player 組件
            Player p = hit.GetComponent<Player>();
            
            // 找到了且不是自己
            if (p != null && hit.gameObject != gameObject)
            {
                currentPlayerFound = p;
                break; 
            }
        }

        // --- 以下狀態切換邏輯不變 ---
        if (currentPlayerFound != null && currentPlayerFound != lastTargetPlayer)
        {
            if (lastTargetPlayer != null) lastTargetPlayer.SetSelected(false);
            currentPlayerFound.SetSelected(true);
            lastTargetPlayer = currentPlayerFound;
        }
        else if (currentPlayerFound == null && lastTargetPlayer != null)
        {
            lastTargetPlayer.SetSelected(false);
            lastTargetPlayer = null;
        }
    }

    void OnMouseEnter() 
    {
        if (GameManager.Instance.currentState != GameState.Preparation || isDragging) 
        {
            return;
        }

        isHovering = true;
    }

    void OnMouseExit() 
    {
        isHovering = false;
    }
    
    void OnMouseDown()
    {
        if (GameManager.Instance.currentState != GameState.Preparation) 
        {
            return;
        }
        isDragging = true;
        originalLayer = gameObject.layer;
        gameObject.layer = 2;

        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Vector3 cursorScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(cursorScreenPoint);
    }
    void OnMouseUp()
    {
        isDragging = false;
        isHovering = false; // 修正：放開時也確保 Hover 狀態清除
        
        CheckForPlayer();

        // 原有的清理 lastTargetPlayer 和 Layer 邏輯...
        if (lastTargetPlayer != null)
        {
            lastTargetPlayer.SetSelected(false);
            lastTargetPlayer = null;
        }
        gameObject.layer = originalLayer;
    }

    // 當滑鼠按住並移動時
    void OnMouseDrag()
    {
        if (GameManager.Instance.currentState != GameState.Preparation) 
        {
            return;
        }

        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }

    void CheckForPlayer()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 這裡改用 OverlapPoint 檢查單一物件
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit != null && hit.gameObject != gameObject)
        {
            Player p = hit.GetComponent<Player>();
            if (p != null)
            {
                p.AdjustStats(health, att, def);
                Debug.Log("效果施加成功！");
                Destroy(gameObject);
            }
        }
    }
}
