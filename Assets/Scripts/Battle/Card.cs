using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Card : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private int originalLayer;

    [Header("移動設定")]
    public float smoothSpeed = 10f;
    public Vector3 baseLocalPosition; // 新增/修改：記錄 HandManager 分配的「基礎」位置
    private Vector3 currentTargetLocal; // 新增：計算懸停後的「實際」目標位置
    private bool isDragging = false;

    [Header("懸停設定")]
    public float moveUpOffset = 1.0f; // 2D 建議不要太大，1.5 ( Inspector 裡的舊數值) 很適合
    private bool isHovering = false; // // 新增：記錄滑鼠是否懸停
    
    [Header("卡片設定")]
    public GameObject prefab;
    public string cardName;
    public float att;
    public float def;
    public float hp;
    public float maxHp;
    public GameObject sourceCardPrefab;
    private Card_text cardTextComponent;

    void Start()
    {
        cardTextComponent = GetComponentInChildren<Card_text>();

        if (sourceCardPrefab == null && prefab != null)
        {
            sourceCardPrefab = prefab;
        }

        // 如果是全新抽到的卡，直接去讀取 prefab 檔案的 CharacterBase 初始化
        if (string.IsNullOrEmpty(cardName) && prefab != null)
        {
            CharacterBase cb = prefab.GetComponent<CharacterBase>();
            if (cb != null)
            {
                cardName = cb.unitName;
                att = Mathf.RoundToInt(cb.attack);
                def = Mathf.RoundToInt(cb.defense);
                hp = Mathf.RoundToInt(cb.health);
                maxHp = Mathf.RoundToInt(cb.maxHealth > 0f ? cb.maxHealth : cb.health);
                sourceCardPrefab = prefab;
            }
        }

        RefreshUI();
    }

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

        // 顯示可放置區域
        GameManager.Instance.SetAreaActive(true);
    }
    void OnMouseUp()
    {
        isDragging = false;
        isHovering = false; // 修正：放開時也確保 Hover 狀態清除
        
        CheckForPlace();

        gameObject.layer = originalLayer;

        GameManager.Instance.SetAreaActive(false);
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

    void CheckForPlace()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit != null && hit.gameObject != gameObject)
        {
            GridSlot slot = hit.GetComponent<GridSlot>();
            
            if (hit.gameObject.name.Contains("PlayerArea") && slot != null && !slot.isOccupied)
            {
                // 1. 生成場上角色 (使用卡片紀錄的原始 prefab)
                GameObject unitPrefab = sourceCardPrefab != null ? sourceCardPrefab : prefab;

                if (unitPrefab == null && HandManager.Instance != null)
                {
                    unitPrefab = HandManager.Instance.ResolveUnitPrefabForCard(cardName);
                }

                if (unitPrefab == null)
                {
                    Debug.LogError("Card has no unit prefab assigned: " + gameObject.name);
                    return;
                }

                GameObject playerObj = Instantiate(unitPrefab, hit.gameObject.transform.position, Quaternion.identity);
                
                // 2. 將這張卡片的「當前數據」（可能是殘血）同步給場上角色的 CharacterBase
                CharacterBase cb = playerObj.GetComponent<CharacterBase>();
                if (cb != null)
                {
                    cb.unitName = this.cardName;
                    cb.attack = this.att;
                    cb.defense = this.def;
                    cb.health = this.hp;      // 灌入當前血量
                    cb.maxHealth = this.maxHp; // 灌入最大血量
                    
                    // 讓場上的 Player 腳本（如果有繼承 CharacterBase）記住原始 Prefab 檔案
                    Player p = playerObj.GetComponent<Player>();
                    if (p != null) p.sourceCardPrefab = unitPrefab;
                }
                
                slot.isOccupied = true;
                hit.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                hit.enabled = false; 

                // 3. 卡牌功成身退，摧毀物件
                Destroy(gameObject); 
            }
        }
    }

    public void UpdateStats(float att_amount, float def_amount, float hp_amount, float maxHp_amount = 0f)
    {
        att += att_amount;
        def += def_amount;
        maxHp += maxHp_amount;

        if (maxHp < 1f)
        {
            maxHp = 1f;
        }

        hp += hp_amount;

        if (hp > maxHp)
        {
            hp = maxHp;
        }

        if (hp < 0f)
        {
            hp = 0f;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (cardTextComponent != null)
        {
            cardTextComponent.UpdateText();
        }
    }

}
