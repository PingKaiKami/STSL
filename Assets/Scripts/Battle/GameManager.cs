using UnityEngine;

public enum GameState
{
    Preparation,
    Combat
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("當前遊戲狀態")]
    public GameState currentState;

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
            currentState = GameState.Combat;
        }
    }
    public void OnCharacterDied(CharacterBase deadCharacter)
    {
        // 如果死的是敵人，代表我們贏了！
        if (deadCharacter.CompareTag("Enemy"))
        {
            Debug.Log("🏆 戰鬥勝利！所有敵人皆已消滅。");
            EndCombat();
        }
        // 如果死的是玩家，代表我們輸了
        else if (deadCharacter.CompareTag("Player"))
        {
            Debug.Log("💀 戰鬥失敗！勇者倒下了。");
            EndCombat();
        }
    }

    // 新增：結束戰鬥，回到備戰狀態
    void EndCombat()
    {
        currentState = GameState.Preparation;
        Debug.Log("--- 進入備戰階段 ---");
        Debug.Log("請重新調整卡片與陣型。");

        // (未來可以寫在這邊：復活所有死掉的角色、把他們拉回原本站的位子、發放勝利金幣等)
    }
}