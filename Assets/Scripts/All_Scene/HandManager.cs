using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public float spacing = 2.0f;
    public static HandManager Instance;
    public GameObject basicCard;
    public GameObject warriorPrefab;
    private bool handBuiltForCurrentPreparation;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.currentState == GameState.Preparation && !handBuiltForCurrentPreparation)
        {
            RebuildHandFromRunState();
            handBuiltForCurrentPreparation = true;
        }

        if (GameManager.Instance.currentState != GameState.Preparation)
        {
            handBuiltForCurrentPreparation = false;
        }

        if (GameManager.Instance.currentState != GameState.Preparation
            && GameManager.Instance.currentState != GameState.Shopping)
        {
            SetCardsActive(false);
            return;
        }

        SetCardsActive(true);
        ArrangeCards();
    }

    public void RebuildHandFromRunState()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        List<PlayerCardRuntimeData> cardData = runState.GetPlayerCards();
        GameObject templateCard = GetCardTemplate();

        if (templateCard == null)
        {
            Debug.LogError("HandManager: no card template is available.");
            return;
        }

        List<GameObject> oldCards = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            oldCards.Add(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < cardData.Count; i++)
        {
            GameObject newCardObject = Instantiate(templateCard, transform);
            newCardObject.SetActive(true);

            Card card = newCardObject.GetComponent<Card>();

            if (card != null)
            {
                ApplyRuntimeCardData(card, cardData[i]);
            }
        }

        for (int i = 0; i < oldCards.Count; i++)
        {
            Destroy(oldCards[i]);
        }

        ArrangeCards();
    }

    public void ResetHandAfterBattle()
    {
        DestroyPlayersOnField();
        RebuildHandFromRunState();
        handBuiltForCurrentPreparation = false;
    }

    public void SyncRunStateFromHand()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        List<PlayerCardRuntimeData> cards = new List<PlayerCardRuntimeData>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card == null)
            {
                continue;
            }

            cards.Add(CreateRuntimeCardData(card, i));
        }

        runState.SetPlayerCards(cards);
    }

    private GameObject GetCardTemplate()
    {
        if (basicCard != null)
        {
            return basicCard;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card != null)
            {
                return card.gameObject;
            }
        }

        return null;
    }

    private void ApplyRuntimeCardData(Card card, PlayerCardRuntimeData data)
    {
        if (data == null)
        {
            return;
        }

        card.cardName = data.cardName;
        GameObject unitPrefab = ResolveUnitPrefab(data.unitPrefabId);

        if (unitPrefab != null)
        {
            card.prefab = unitPrefab;
            card.sourceCardPrefab = unitPrefab;

            CharacterBase character = unitPrefab.GetComponent<CharacterBase>();

            if (character != null)
            {
                card.att = Mathf.RoundToInt(character.attack);
                card.def = Mathf.RoundToInt(character.defense);
                card.hp = Mathf.RoundToInt(character.health);
                card.maxHp = Mathf.RoundToInt(character.maxHealth);
            }
        }

        if (data.att != 0f || data.def != 0f || data.hp != 0f || data.maxHp != 0f)
        {
            card.att = data.att;
            card.def = data.def;
            card.hp = data.hp;
            card.maxHp = data.maxHp;
        }

        Card_text cardText = card.GetComponentInChildren<Card_text>();

        if (cardText != null)
        {
            cardText.UpdateText();
        }
    }

    private GameObject ResolveUnitPrefab(string unitPrefabId)
    {
        if (unitPrefabId == "Warrior" && warriorPrefab != null)
        {
            return warriorPrefab;
        }

        if (basicCard != null)
        {
            Card basicCardScript = basicCard.GetComponent<Card>();

            if (basicCardScript != null && basicCardScript.prefab != null)
            {
                return basicCardScript.prefab;
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card == null || card.prefab == null)
            {
                continue;
            }

            CharacterBase character = card.prefab.GetComponent<CharacterBase>();

            if (character != null && character.unitName == unitPrefabId)
            {
                return card.prefab;
            }

            if (card.prefab.name == unitPrefabId)
            {
                return card.prefab;
            }
        }

        return null;
    }

    private PlayerCardRuntimeData CreateRuntimeCardData(Card card, int index)
    {
        string runtimeCardId = string.IsNullOrEmpty(card.cardName)
            ? "card_" + index
            : card.cardName.ToLower().Replace(" ", "_") + "_" + index;
        string unitPrefabId = card.sourceCardPrefab != null
            ? card.sourceCardPrefab.name
            : card.prefab != null
                ? card.prefab.name
                : card.cardName;

        return new PlayerCardRuntimeData(
            runtimeCardId,
            card.cardName,
            unitPrefabId,
            card.att,
            card.def,
            card.hp,
            card.maxHp
        );
    }

    void ArrangeCards()
    {
        // 取得所有子物件 (假設卡片都在 Panel 下)
        int count = transform.childCount;
        if (count == 0) return;

        float totalWidth = (count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            Card cardScript = child.GetComponent<Card>();

            if (cardScript != null)
            {
                // 計算目標位置
                Vector3 pos = new Vector3(startX + (i * spacing), -3.2f, 0);
                
                // 【核心修改】：直接把位置寫入卡片的變數裡
                cardScript.baseLocalPosition = pos;
            }
        }
    }

    void SetCardsActive(bool isActive)
    {
        // 遍歷所有子物件
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.activeSelf != isActive)
            {
                child.SetActive(isActive);
            }
        }
    }

    /// <summary>
    /// 由外部呼叫，傳入卡片 Prefab 並生成為 HandManager 的子物件
    /// </summary>
    /// <param name="cardPrefab">想要新增的卡片 Prefab</param>
    public void AddCard(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("HandManager: 傳入的卡片 Prefab 是空的 (null)！");
            return;
        }

        // 生成卡片，並直接指定父物件 (transform) 為當前的 HandManager
        GameObject newCardObj = Instantiate(cardPrefab, transform);

        // 取得卡片元件進行基礎位置初始化，避免生成瞬間在畫面上亂閃
        Card cardScript = newCardObj.GetComponent<Card>();
        if (cardScript != null)
        {
            // 先給一個預設的局部位置
            cardScript.baseLocalPosition = new Vector3(0, -3.2f, 0);
        }

        // 效能優化小提示：因為新增了卡片，可以在這裡手動呼叫一次 ArrangeCards()
        // ArrangeCards(); 
    }

    /// <summary>
    /// 按百分比恢復目前手牌中所有卡片的 HP (會自動防溢補)
    /// </summary>
    /// <param name="percentage">恢復比例 (例如 0.5 代表 50%)</param>
    public void HealAllCardsPercentage(float percentage)
    {
        // 遍歷 HandManager 底下的所有卡片
        foreach (Transform child in transform)
        {
            Card cardScript = child.GetComponent<Card>();
            
            if (cardScript != null)
            {
                // 1. 根據這張卡片原本的最大血量，計算 50% 是多少補量 (四捨五入成 int)
                int healAmount = Mathf.RoundToInt(cardScript.maxHp * percentage);

                // 2. 計算補血後的理想血量
                float targetHp = cardScript.hp + healAmount;

                // 3. 確保補血後的血量不會超過上限 (maxHp)
                if (targetHp > cardScript.maxHp)
                {
                    targetHp = cardScript.maxHp;
                }

                // 4. 計算出「實際上真正補了多少血」
                float actualHeal = targetHp - cardScript.hp;

                // 5. 呼叫原本的 UpdateStats 灌入真正增加的血量
                // 註：因為是加血，actualHeal 會是正數 (例如 0, 0, 3)
                cardScript.UpdateStats(0, 0, actualHeal);
            }
        }
    }

    /// <summary>
    /// 回收場上所有的 Player 並將對應的卡片加回手牌
    /// </summary>
    public void RecallAllPlayersToHand()
    {
        GameObject[] playersOnField = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in playersOnField)
        {
            CharacterBase cb = playerObj.GetComponent<CharacterBase>();
            Player p = playerObj.GetComponent<Player>();

            if (cb != null && p != null && p.sourceCardPrefab != null)
            {
                // 如果死掉了，直接清除，不回收
                if (cb.health <= 0)
                {
                    Destroy(playerObj);
                    continue;
                }

                // 1. 生成空白的基礎卡牌
                GameObject newCardObj = Instantiate(basicCard);
                Card newCardScript = newCardObj.GetComponent<Card>();

                if (newCardScript != null)
                {
                    // 2. 透過 CharacterBase 的數值，直接賦值給新卡片的變數
                    newCardScript.cardName = cb.unitName;
                    newCardScript.att = Mathf.RoundToInt(cb.attack);
                    newCardScript.def = Mathf.RoundToInt(cb.defense);
                    newCardScript.hp = Mathf.RoundToInt(cb.health);       // 場上剩多少，卡片就記多少
                    newCardScript.maxHp = Mathf.RoundToInt(cb.maxHealth); // 帶回原本的最大血量上限
                    
                    // 3. 傳遞原始檔案指標，確保這張殘血卡下次還能再次被正確打出場
                    newCardScript.sourceCardPrefab = p.sourceCardPrefab; 
                }

                // 4. 丟進手牌面板
                AddCard(newCardObj);
                Destroy(newCardObj);
            }

            // 5. 順利轉移，刪除場上實體角色
            Destroy(playerObj);
        }

        ArrangeCards();
        SyncRunStateFromHand();
    }

    private void DestroyPlayersOnField()
    {
        GameObject[] playersOnField = GameObject.FindGameObjectsWithTag("Player");

        for (int i = 0; i < playersOnField.Length; i++)
        {
            Destroy(playersOnField[i]);
        }
    }
}
