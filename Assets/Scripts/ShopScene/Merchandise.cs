using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Merchandise : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private int originalLayer;

    [Header("基礎資訊")]
    public string obj_name;

    [Header("移動設定")]
    public float smoothSpeed = 10f;
    public Vector3 baseLocalPosition;   // 記錄商店或背包分配的「基礎」位置
    private Vector3 currentTargetLocal; // 計算懸停後的「實際」目標位置
    private bool isDragging = false;

    [Header("懸停設定")]
    public float moveUpOffset = 1.0f;   // 滑鼠懸停時向上偏移的距離
    private bool isHovering = false;    // 記錄滑鼠是否懸停
    
    [Header("商品屬性變數")]
    public float att;
    public float def;
    public float hp;

    private Card targetCard; // 用於記錄目前商品拖拽到哪一個玩家上方

    void Update()
    {
        // 只有在不在拖拽時，才執行平滑移動（歸位或懸停浮起）
        if (!isDragging)
        {
            currentTargetLocal = baseLocalPosition;

            if (isHovering)
            {
                currentTargetLocal += Vector3.up * moveUpOffset;
            }

            transform.localPosition = Vector3.Lerp(
                transform.localPosition, 
                currentTargetLocal, 
                Time.deltaTime * smoothSpeed
            );
        }
        
        // 如果正在拖拽，持續掃描滑鼠下方有沒有 Player
        if (isDragging) 
        {
            ScanForPlayerUnderCard();
        }
    }

    void ScanForPlayerUnderCard()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);

        targetCard = null; // 每幀重置

        foreach (var hit in hits)
        {
            Card c = hit.GetComponent<Card>();
            
            // 找到了 Player 且不是自己
            if (c != null && hit.gameObject != gameObject)
            {
                targetCard = c;
                break; 
            }
        }
    }

    void OnMouseEnter() 
    {
        // 限制只有在準備階段且沒有在拖拽時才能觸發懸停
        if (GameManager.Instance.currentState != GameState.Shopping || isDragging) 
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
        if (GameManager.Instance.currentState != GameState.Shopping) 
        {
            return;
        }

        isDragging = true;
        originalLayer = gameObject.layer;
        gameObject.layer = 2; // 切換到 Ignore Raycast 層，避免擋到射線

        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Vector3 cursorScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(cursorScreenPoint);
    }

    void OnMouseUp()
    {
        isDragging = false;
        isHovering = false; 
        
        // 執行放置或裝備判定
        CheckForPlace();

        gameObject.layer = originalLayer;
        GameManager.Instance.SetAreaActive(false);
    }

    void OnMouseDrag()
    {
        if (GameManager.Instance.currentState != GameState.Shopping) 
        {
            return;
        }

        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }

    void CheckForPlace()
    {
        // 檢查 1：如果放開滑鼠時，下方正對著一個 Player，就把裝備塞給他！
        if (targetCard != null)
        {
            // 呼叫 Player 身上的 UpdateStats，把商品的數值加進去
            targetCard.UpdateStats(att, def, hp);
            
            Debug.Log($"已將 {obj_name} 裝備給 {targetCard.gameObject.name}！");
            Destroy(gameObject); // 裝備成功後銷毀商品物件
            return;
        }
    }
}