using UnityEngine;

public enum GameState
{
    Menu,
    MapSelection,
    Preparation,
    Combat,
    Shopping
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("當前遊戲狀態")]
    public GameState currentState;
    public GameObject placeableArea;

    [Header("戰鬥統計")]
    public int playerCount;
    public int enemyCount;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentState = GameState.Preparation;
    }

    public void StartCombatPhase()
    {
        if (currentState == GameState.Preparation)
        {
            // 1. 計算場上所有的玩家與敵人
            playerCount = GameObject.FindGameObjectsWithTag("Player").Length;
            enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

            Debug.Log($"戰鬥開始！玩家數量：{playerCount}，敵人數量：{enemyCount}");

            // 如果場上根本沒敵人或沒玩家，直接結束（防呆）
            if (playerCount == 0 || enemyCount == 0)
            {
                EndCombat();
                return;
            }

            currentState = GameState.Combat;
        }
    }

    public void OnCharacterDied(CharacterBase deadCharacter)
    {
        // 2. 根據 Tag 減少對應的計數
        if (deadCharacter.CompareTag("Enemy"))
        {
            enemyCount--;
            Debug.Log($"一名敵人死亡，剩餘敵人：{enemyCount}");
        }
        else if (deadCharacter.CompareTag("Player"))
        {
            playerCount--;
            Debug.Log($"一名玩家死亡，剩餘玩家：{playerCount}");
        }

        // 3. 檢查勝利或失敗條件
        CheckBattleResult();
    }

    private void CheckBattleResult()
    {
        if (enemyCount <= 0)
        {
            Debug.Log("🏆 戰鬥勝利！所有敵人皆已消滅。");
            EndCombat();
        }
        else if (playerCount <= 0)
        {
            Debug.Log("💀 戰鬥失敗！所有玩家皆已陣亡。");
            EndCombat();
        }
    }

    void EndCombat()
    {
        // currentState = GameState.MapSelection;
        
    }
}