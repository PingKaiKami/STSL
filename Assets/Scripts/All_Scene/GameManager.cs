using System.Collections.Generic;
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

public enum BattleRegistrationMode
{
    CountForBattle,     // 計入戰鬥：生成 +1，死亡 -1，影響勝利條件
    IgnoreForBattle,    // 有註冊但不計數：生成不加，死亡不扣
    DoNotRegister       // 完全不註冊
}

[System.Serializable]
public class EnemyRegistrationRule
{
    public GameObject prefab;
    public BattleRegistrationMode registrationMode = BattleRegistrationMode.CountForBattle;
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

    [Header("Scene Names")]
    [SerializeField] private string mapSceneName = "MapGenerationScene";
    [SerializeField] private string failSceneName = "FailScene";
    [SerializeField] private string finalWinSceneName = "FinalWinScene";

    [Header("Battle Rewards")]
    [SerializeField] private int victoryGoldReward = 20;

    [Header("Enemy Registration Rules")]
    [SerializeField] private List<EnemyRegistrationRule> enemyRegistrationRules =
        new List<EnemyRegistrationRule>();

    [Header("Default Registration")]
    [SerializeField] private BattleRegistrationMode defaultEnemyRegistrationMode =
        BattleRegistrationMode.CountForBattle;

    [SerializeField] private BattleRegistrationMode defaultPlayerRegistrationMode =
        BattleRegistrationMode.CountForBattle;

    /*
     * countedEnemies / countedPlayers：
     * 會影響勝負條件的單位。
     *
     * ignoredEnemies / ignoredPlayers：
     * 有註冊，但不影響勝負條件。
     *
     * 使用 InstanceID 避免：
     * 1. 同一單位重複註冊
     * 2. 死亡事件重複觸發造成重複扣數
     */
    private readonly HashSet<int> countedEnemies = new HashSet<int>();
    private readonly HashSet<int> ignoredEnemies = new HashSet<int>();

    private readonly HashSet<int> countedPlayers = new HashSet<int>();
    private readonly HashSet<int> ignoredPlayers = new HashSet<int>();

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
        else if (currentScene.name == "ShopScene")
        {
            currentState = GameState.Shopping;
        }
        else if (currentScene.name == "RestScene")
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

    // =========================================================
    // Battle Registry
    // =========================================================

    public void ResetBattleRegistry()
    {
        countedEnemies.Clear();
        ignoredEnemies.Clear();

        countedPlayers.Clear();
        ignoredPlayers.Clear();

        enemyCount = 0;
        playerCount = 0;

        Debug.Log("戰鬥註冊資料已重置");
    }

    /*
     * 給「場上一開始就存在」的單位使用。
     * 通常初始 Enemy / Player 都應該計入勝負。
     */
    public void RegisterEnemy(CharacterBase enemy, BattleRegistrationMode mode)
    {
        if (enemy == null) return;

        int id = enemy.GetInstanceID();

        if (countedEnemies.Contains(id) || ignoredEnemies.Contains(id))
        {
            return;
        }

        switch (mode)
        {
            case BattleRegistrationMode.CountForBattle:
                countedEnemies.Add(id);
                enemyCount++;

                Debug.Log($"敵人註冊：{enemy.name}，目前敵人數量：{enemyCount}");
                break;

            case BattleRegistrationMode.IgnoreForBattle:
                ignoredEnemies.Add(id);

                Debug.Log($"非計數敵人註冊：{enemy.name}，不影響 enemyCount");
                break;

            case BattleRegistrationMode.DoNotRegister:
                Debug.Log($"敵人不註冊：{enemy.name}");
                break;
        }
    }

    public void RegisterEnemy(CharacterBase enemy, bool countForBattle = true)
    {
        RegisterEnemy(
            enemy,
            countForBattle
                ? BattleRegistrationMode.CountForBattle
                : BattleRegistrationMode.IgnoreForBattle
        );
    }

    /*
     * 給召喚物使用。
     * 召喚腳本要把「原始 Prefab」傳進來。
     */
    public void RegisterEnemyFromPrefab(CharacterBase enemy, GameObject sourcePrefab)
    {
        if (enemy == null) return;

        BattleRegistrationMode mode = GetEnemyRegistrationMode(sourcePrefab);

        RegisterEnemy(enemy, mode);
    }

    private BattleRegistrationMode GetEnemyRegistrationMode(GameObject sourcePrefab)
    {
        if (sourcePrefab == null)
        {
            return defaultEnemyRegistrationMode;
        }

        for (int i = 0; i < enemyRegistrationRules.Count; i++)
        {
            EnemyRegistrationRule rule = enemyRegistrationRules[i];

            if (rule == null) continue;
            if (rule.prefab == null) continue;

            if (rule.prefab == sourcePrefab)
            {
                return rule.registrationMode;
            }
        }

        return defaultEnemyRegistrationMode;
    }

    public void RegisterPlayer(CharacterBase player, BattleRegistrationMode mode)
    {
        if (player == null) return;

        int id = player.GetInstanceID();

        if (countedPlayers.Contains(id) || ignoredPlayers.Contains(id))
        {
            return;
        }

        switch (mode)
        {
            case BattleRegistrationMode.CountForBattle:
                countedPlayers.Add(id);
                playerCount++;

                Debug.Log($"玩家註冊：{player.name}，目前玩家數量：{playerCount}");
                break;

            case BattleRegistrationMode.IgnoreForBattle:
                ignoredPlayers.Add(id);

                Debug.Log($"非計數玩家註冊：{player.name}，不影響 playerCount");
                break;

            case BattleRegistrationMode.DoNotRegister:
                Debug.Log($"玩家不註冊：{player.name}");
                break;
        }
    }

    public void RegisterPlayer(CharacterBase player, bool countForBattle = true)
    {
        RegisterPlayer(
            player,
            countForBattle
                ? BattleRegistrationMode.CountForBattle
                : BattleRegistrationMode.IgnoreForBattle
        );
    }

    /*
     * 可以在戰鬥開始時呼叫。
     * 會把場上已存在的 Player / Enemy 註冊進統計。
     *
     * 注意：
     * 這個方法無法可靠判斷「場景物件來自哪個 Prefab」。
     * 所以初始 Enemy 會用 defaultEnemyRegistrationMode。
     * 召喚物請用 RegisterEnemyFromPrefab()。
     */
    public void RegisterInitialBattleUnits()
    {
        ResetBattleRegistry();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in players)
        {
            if (playerObj == null) continue;
            if (!playerObj.activeInHierarchy) continue;

            CharacterBase character = playerObj.GetComponent<CharacterBase>();

            if (character != null)
            {
                RegisterPlayer(character, defaultPlayerRegistrationMode);
            }
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemyObj in enemies)
        {
            if (enemyObj == null) continue;
            if (!enemyObj.activeInHierarchy) continue;

            CharacterBase character = enemyObj.GetComponent<CharacterBase>();

            if (character != null)
            {
                RegisterEnemy(character, defaultEnemyRegistrationMode);
            }
        }

        Debug.Log($"初始戰鬥單位註冊完成：Player={playerCount}, Enemy={enemyCount}");
    }

    public void OnCharacterDied(CharacterBase deadCharacter)
    {
        if (deadCharacter == null) return;

        int id = deadCharacter.GetInstanceID();
        bool shouldCheckBattleResult = false;

        if (deadCharacter.CompareTag("Enemy"))
        {
            if (countedEnemies.Remove(id))
            {
                enemyCount--;

                if (enemyCount < 0)
                {
                    enemyCount = 0;
                }

                shouldCheckBattleResult = true;

                Debug.Log($"一名計數敵人死亡：{deadCharacter.name}，剩餘敵人：{enemyCount}");
            }
            else if (ignoredEnemies.Remove(id))
            {
                Debug.Log($"一名非計數敵人死亡：{deadCharacter.name}，不扣 enemyCount");
            }
            else
            {
                Debug.LogWarning($"未註冊敵人死亡：{deadCharacter.name}，不扣 enemyCount");
            }
        }
        else if (deadCharacter.CompareTag("Player"))
        {
            if (countedPlayers.Remove(id))
            {
                playerCount--;

                if (playerCount < 0)
                {
                    playerCount = 0;
                }

                shouldCheckBattleResult = true;

                Debug.Log($"一名計數玩家死亡：{deadCharacter.name}，剩餘玩家：{playerCount}");
            }
            else if (ignoredPlayers.Remove(id))
            {
                Debug.Log($"一名非計數玩家死亡：{deadCharacter.name}，不扣 playerCount");
            }
            else
            {
                Debug.LogWarning($"未註冊玩家死亡：{deadCharacter.name}，不扣 playerCount");
            }
        }

        if (shouldCheckBattleResult)
        {
            CheckBattleResult();
        }
    }

    private void CheckBattleResult()
    {
        if (enemyCount <= 0)
        {
            Debug.Log("🏆 戰鬥勝利！所有計數敵人皆已消滅。");
            EndCombat(true);
        }
        else if (playerCount <= 0)
        {
            Debug.Log("💀 戰鬥失敗！所有計數玩家皆已陣亡。");
            EndCombat(false);
        }
    }

    public void EndCombat(bool isWin)
    {
        currentState = GameState.EndCombat;

        if (isWin)
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.RecallAllPlayersToHand();
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            
            if(currentSceneName == "BattleScene_normal")
            {
                PlayerManager.EnsureExists().ModifyMoney(victoryGoldReward);
            }
            else
            {
                PlayerManager.EnsureExists().ModifyMoney(victoryGoldReward * 2);
            }

            

            if (!isDebug)
            {
                RunStateManager runState = RunStateManager.EnsureExists();
                bool isFinalBoss = runState.IsPendingBossRoom();

                runState.CompletePendingRoom();

                if (isFinalBoss)
                {
                    currentState = GameState.Menu;
                    SceneManager.LoadScene(finalWinSceneName);
                    return;
                }

                currentState = GameState.MapSelection;
                SceneManager.LoadScene(mapSceneName);
            }
        }
        else
        {
            RunStateManager runState = RunStateManager.EnsureExists();

            if (HandManager.Instance != null)
            {
                HandManager.Instance.RecallAllPlayersToHand();
            }

            if (!isDebug)
            {
                currentState = GameState.Menu;
                SceneManager.LoadScene(failSceneName);
                Debug.Log("Game Over");
                return;
            }

            if (!isDebug)
            {
                if (runState.IsPendingBossRoom())
                {
                    currentState = GameState.MapSelection;
                    SceneManager.LoadScene(mapSceneName);
                    return;
                }

                runState.CompletePendingRoom();
                currentState = GameState.MapSelection;
                SceneManager.LoadScene(mapSceneName);
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
