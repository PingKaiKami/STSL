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
請繼承 `Player` 並覆寫（`override`）`CombatLogic()` 函式。
`CombatLogic()` 的運作機制類似於 `Update()`，但它**只會在 `GameState.Combat` (戰鬥狀態) 下被自動呼叫**。請將該角色的 AI 或玩家輸入邏輯寫在這裡。

**範例：**
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
        
        // 在此實作攻擊或其他戰鬥邏輯...
    }
}
```

---

## 4. 敵人角色開發 (Enemy)

所有敵人單位（如：Slime, Goblin）都必須繼承自 Enemy 類別。開發方式與 Player 完全相同。

### 如何建立新敵人？
請繼承 `Enemy` 並覆寫（`override`）`CombatLogic()` 函式。
`CombatLogic()` 的運作機制類似於 `Update()`，但它**只會在 `GameState.Combat` (戰鬥狀態) 下被自動呼叫**。請將該角色的 AI 或玩家輸入邏輯寫在這裡。

**範例：**
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