using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    MapSelection,
    Preparation,
    Combat,
    EndCombat,
    Shopping,
    Rest
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
    private bool isFound = false;

    [Header("開發參數")]
    public bool isDebug;

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
    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name == "MenuScene")
        {
            currentState = GameState.Menu;
        }
        else if (currentScene.name == "BattleScene")
        {
            currentState = GameState.Preparation;
        }
        else if (currentScene.name == "MapGenerationScene")
        {
            currentState = GameState.MapSelection;
        }
        else if(currentScene.name == "ShopScene")
        {
            currentState = GameState.Shopping;
        }
        else if(currentScene.name == "RestScene")
        {
            currentState = GameState.Rest;
        }
    }
    void Update()
    {
        if (currentState == GameState.Preparation)
        {
            if (!isFound)
            {
                placeableArea = GameObject.FindGameObjectWithTag("PlaceableArea");

                if (placeableArea != null)
                {
                    isFound = true; 
                }
            }
        }
        else
        {
            if (isFound)
            {
                isFound = false; 
            }
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
            EndCombat(true);
        }
        else if (playerCount <= 0)
        {
            Debug.Log("💀 戰鬥失敗！所有玩家皆已陣亡。");
            EndCombat(false);
        }
    }

    public void EndCombat(bool isWin)
    {
        currentState = GameState.EndCombat;
        if (isWin)
        {
            HandManager.Instance.RecallAllPlayersToHand();
            PlayerManager.Instance.ModifyMoney(10);
            if (!isDebug)
            {
                RunStateManager runState = RunStateManager.EnsureExists();
                runState.CompletePendingRoom();
                currentState = GameState.MapSelection;
                SceneManager.LoadScene("MapGenerationScene");
            }
        }
        else
        {
            if (PlayerManager.Instance.ModifyHealth(-1))
            {
                Debug.Log($"Current health: {PlayerManager.Instance.health}");
                if (!isDebug)
                {
                    RunStateManager runState = RunStateManager.EnsureExists();
                    runState.CompletePendingRoom();
                    currentState = GameState.MapSelection;
                    SceneManager.LoadScene("MapGenerationScene");
                }
            }
            else
            {
                if (!isDebug)
                {
                    currentState = GameState.Menu;
                    SceneManager.LoadScene("MenuScene");
                    Debug.Log("Game Over");
                }
            }
        }
    }

    public void SetAreaActive(bool isActive)
    {
        if (placeableArea == null) return;

        foreach (Transform child in placeableArea.transform)
        {
            GridSlot slot = child.GetComponent<GridSlot>();
            if (slot != null)
            {
                slot.SetSlotVisible(isActive);
            }
        }
    }
}