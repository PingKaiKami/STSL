# 基礎系統架構與開發指南 (Base System & Guidelines)

本專案的核心邏輯統一由 `GameManager` 進行狀態管理，所有戰鬥單位（玩家與敵人）皆基於 `CharacterBase` 類別進行擴充。請在開發新角色或敵人時，遵循以下的架構規範。

---

## 1. 遊戲狀態管理 (GameState)

遊戲的進程統一由 `GameManager` 控制。目前的 `GameState` 包含以下幾種狀態：

*   `Menu`：主選單。
*   `MapSelection`：地圖選擇。
*   `Preparation`：戰鬥準備階段（例如：佈署角色、抽牌等）。
*   `Combat`：戰鬥進行中。所有單位的 `CombatLogic()` 只有在此狀態下才會觸發。
*   `Shopping`：商店階段。

---

## 2. 角色核心基底 (CharacterBase)

`CharacterBase` 是所有實體（Player 與 Enemy）的父類別。它包含了通用的屬性與基礎方法。

### 屬性面板 (Inspector)
在 Unity Inspector 中，屬性被分為兩大類：

**[戰鬥屬性]**
| 變數名稱 | 型別 | 預設值 | 說明 |
| :--- | :--- | :--- | :--- |
| `moveSpeed` | `float` | `3f` | 移動速度 |
| `attackTime` | `float` | `1.0f` | 攻擊間隔（執行一次攻擊所需的秒數）|
| `attackRange` | `float` | `1.5f` | 攻擊判定範圍 |

**[基礎屬性]**
| 變數名稱 | 型別 | 說明 |
| :--- | :--- | :--- |
| `unitName` | `string` | 單位名稱 |
| `health` | `float` | 當前生命值 |
| `attack` | `float` | 基礎攻擊力 |
| `defense` | `float` | 基礎防禦力 |

### 核心方法 (Methods)
*   `virtual void TakeDamage(float damage)`：處理受傷與扣血邏輯。
*   `virtual void Die()`：處理死亡邏輯（包含通知 GameManager 與物件隱藏）。
*   **`protected void Move(MoveDirection dir)`**：控制角色位移。
    > ⚠️ **重要開發規範：**
    > 呼叫 `Move()` 之前，**必須**先檢查 `isMoving` 布林值。只有當 `isMoving == false` 時才能呼叫，否則會報錯或產生邏輯衝突。傳入參數為 `MoveDirection` 列舉（例如：`MoveDirection.Up`）。

---

## 3. 玩家角色開發 (Player)

所有玩家角色（如：`Warrior`, `Archer`）都必須繼承自 `Player` 類別（而 `Player` 本身繼承自 `CharacterBase`）。

### 如何建立新角色？

1. **參考現有物件**：可前往 `Prefabs` 資料夾參考 `Warrior` 預製體（Prefab）的設定方式。
2. **撰寫腳本**：請建立新腳本並繼承 `Player`，接著覆寫（`override`）`CombatLogic()` 函式。
    * 💡 `CombatLogic()` 的運作機制類似於 `Update()`，但它**只會在 `GameState.Combat` (戰鬥狀態) 下被自動呼叫**。請將該角色的 AI 或戰鬥邏輯實作於此。

**程式碼範例：**
```csharp
public class Warrior : Player
{
    public override void CombatLogic()
    {
        // 確保角色目前不在移動中，才指派新的移動方向
        if (!isMoving)
        {
            Move(MoveDirection.Right);
        }
        
        // 在此實作攻擊或其他專屬戰鬥邏輯...
    }
}
```

### 如何將新角色實裝至遊戲中？

目前的設計概念為「**手牌佈署系統**」。玩家持有的角色會以卡牌的形式儲存在手牌區，在準備階段時，玩家需透過**拖曳卡牌**的方式將角色實體佈署至戰鬥場景中。

**實裝步驟：**

1.  **建立預製體 (Prefab)**
    *   在 Hierarchy 中完成你的新角色物件設定（包含掛載腳本與數值調整）。
    *   ⚠️ **設定 Tag**：請務必將建立好的 GameObject 的 Tag 設定為 `Player`，否則戰鬥系統將無法正確辨識該單位。
    *   將該 GameObject 從 Hierarchy 拖曳至 Project 的 `Prefabs` 資料夾中，使其成為一個 Prefab。

2.  **配置卡牌對應**
    *   開啟並進入 `Battle Scene` 場景。
    *   在 Hierarchy 中尋找 `HandManager` 物件，並展開選取其子物件 **`Card`**。
    *   在 `Card` 的 Inspector 面板中，找到負責實例化角色的 Prefab 欄位。
    *   將步驟 1 建立的**新角色 Prefab** 拖曳至該欄位中，即可完成角色與卡牌的綁定。

---

## 4. 敵人角色開發 (Enemy)

所有敵人單位（如：Slime, Goblin）都必須繼承自 Enemy 類別。開發方式與 Player 幾乎相同。

### 如何建立新敵人？
請繼承 `Enemy` 並覆寫（`override`）`CombatLogic()` 函式。
`CombatLogic()` 的運作機制類似於 `Update()`，但它**只會在 `GameState.Combat` (戰鬥狀態) 下被自動呼叫**。請將該角色的 AI 或玩家輸入邏輯寫在這裡。

**程式碼範例：**
```csharp
public class Slime : Enemy
{
    public override void CombatLogic()
    {
        // 確保角色目前不在移動中，才指派新的移動方向
        if (!isMoving)
        {
            Move(MoveDirection.Right);
        }
        
        // 在此實作攻擊或其他戰鬥邏輯...
    }
}
```

### 如何將新敵人實裝至遊戲中？

與玩家的手牌佈署系統不同，敵方單位在準備階段前就必須預先配置在場景上。

**實裝步驟：**

1.  **放置實體**
    *   將你製作好（已掛載腳本與設定數值）的敵人 GameObject 或 Prefab，直接拖曳到 `Battle Scene` 場景中。
2.  **設定 Tag**
    *   ⚠️ **設定 Tag**：請務必將敵人物件的 Tag 設定為 `Enemy`，否則玩家將無法對其進行攻擊，戰鬥勝負條件也會判定失敗。
3.  **對齊網格位置**
    *   ⚠️ **精準定位**：請確保敵人放置在地圖格子的**正中心**。
    *   由於地圖每格的邊長為 `1`，因此敵人在 Transform 面板中的 Position 座標數值，理論上應該要是**整數**（例如：`1`, `2`）或是 **0.5 的倍數**（例如：`1.5`, `2.5`）。請務必手動檢查數值，避免角色卡在格子邊緣。

---
## 5. 新功能補充 (動態殘血回收與恢復系統)

### 5.1 `Card.UpdateStats`
* **功能**：動態修改卡牌數值（Att/Def/HP）並即時重新渲染 UI。
* **簽章**：`public void UpdateStats(int att_amount, int def_amount, int hp_amount)`
* **流程**：累加數值 ➡️ 觸發 `RefreshUI()` ➡️ 驅動 `Card_text` 顯示最新數值與戰損。
* **外部呼叫方式**：
    通常是透過拿到某張卡片的引用（例如在卡片互動、裝備附加時）來直接呼叫。
* **呼叫範例**：
    ```csharp
    // 範例：某個裝備或效果讓「這張卡片」增加 2 點攻擊力與 5 點血量
    targetCard.UpdateStats(2, 0, 5);
    ```

---

### 5.2 `HandManager.AddCard`
* **功能**：將配置好資料的新卡片納入手牌管理系統。
* **簽章**：`public void AddCard(GameObject cardObj)`
* **流程**：設為 HandManager 子物件 (`SetParent`) ➡️ 給予基礎區域座標 ➡️ 執行 `ArrangeCards()` 線性排開。
* **外部呼叫方式**：
    透過 `HandManager` 的全域單例（Instance），在任何腳本（如抽卡系統、戰後回收系統）中直接塞入一張卡片物件。
* **呼叫範例**：
    ```csharp
    // 範例：將畫面上剛生成好數據的新卡片「newCardObj」直接塞進手牌系統
    HandManager.Instance.AddCard(newCardObj);
    ```
---

### 5.3 `GameManager` 核心流程
#### 1. 戰鬥結束階段 (RecallAllPlayersToHand)
1. **掃描過濾**：撈取場上所有 `"Player"` 物件，`health <= 0` 的直接 `Destroy` 剔除。
2. **生殼複製**：用 `basicCard` 生成新手牌，將場上 `CharacterBase` 的當前數值與 `maxHealth` 強灌給卡片。
3. **基因繼承**：將角色的 `sourceCardPrefab` 傳給新卡片（確保下次能重新生成該角色）。
4. **清理戰場**：卡片加回手牌後，徹底 `Destroy(playerObj)`。

#### 2. 營地休息階段 (HealAllCardsPercentage)
1. **無實體維護**：場上無角色，純透過 `HandManager` 遍歷手牌。
2. **動態計算**：讀取卡片 `maxHp` 乘以係數（如 `0.5f`），四捨五入算出補量。
3. **防溢補校正**：若 `當前血量 + 補量 > maxHp`，強制截斷至 `maxHp`。
4. **刷新**：將淨補量傳入 `UpdateStats` 更新卡片。

---

### 5.4 新增欄位 (未來會衝突的地方)

1. **`CharacterBase.maxHealth` (float)**
    * **位置**：`CharacterBase.cs`
    * **用途**：計算殘血量與休息區補血基數。

2. **`Player.sourceCardPrefab` (GameObject)**
    * **位置**：`Player.cs`
    * **用途**：指向 Project 資料夾的原始 Prefab 檔案，作為卡片再生成的設計圖。

---

## 6. Bug 修復與遊戲性調整紀錄

### 6.1 Bug 修復
1. 修復寶箱事件沒有與新版 Prefab 同步的問題。
2. 修復戰鬥結束雖然有記錄玩家損失血量，但進入新戰鬥時又被重設為原始數值的問題。
    * 原因：`Player` 相關腳本會自行重設一次基礎數值。
    * 處理方式：採用延遲 reset，保留回收後的殘血狀態。
    * 注意：血條顯示需要進入正式戰鬥流程後才會更新為正確狀態。
3. 修復遊戲註冊衝突，導致遊戲沒有正確結束的問題。
4. 修復玩家名稱被背景遮住，導致無法正確顯示的問題。
5. 修復 default money 沒有歸零的問題。
6. 修復按下獲勝按鈕時，獲得金錢與當前關卡類型不一致的問題。
7. 修復 `StartNewRun` 沒有正確重置手牌的問題。

### 6.2 遊戲性調整
1. 新增寶箱事件隨機性，讓各種職業都有機會成為寶箱獎勵。
2. 調降玩家與敵人的基本素質，使基礎狀態下幾乎無法直接擊敗 Boss，但仍可擊敗一般小怪。
3. 敵人生成位置改為隨機，並同步增加玩家可放置區域。
4. 依照場景類型調整敵人數量：
    * `enemy`：1 名敵人。
    * `elite`：2 名敵人。
    * `boss`：3 名敵人。
5. 刪除原本三條血量的設計，改為玩家輸掉一場就直接結束遊戲。
    * 原因：目前已經有殘留血量系統，若再保留多條命會降低風險感。
6. 調整關卡金錢獲得量：
    * `normal`：10 金錢。
    * `elite`：20 金錢。
7. 刪除商店中新增卡片與刪除卡片的設定，改為以裝備升級作為主要購買內容。
8. 回復場景從原本的回滿血，改為回復 30% Max Health。
    * 原因：若能直接回滿血，避戰路線的策略意義會降低。

### 6.3 實驗相關設計
1. 使用者可以在 Inspector 中透過 `SpeedController` 自行調整遊戲流速。
2. 戰鬥偏好路線：偏好戰鬥的玩家可以透過戰鬥獲得金錢，再進入商店購買更強力的裝備。
3. 中立路線：玩家仍會想取得金錢進商店，但會傾向只購買 CP 值較高的裝備。
4. 避戰路線：玩家雖然較難取得金錢購買裝備，但可以用較高的血量狀態挑戰 Boss。
