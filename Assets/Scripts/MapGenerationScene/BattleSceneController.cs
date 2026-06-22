using UnityEngine;
using UnityEngine.SceneManagement;

// This script belongs to the MapGenerationScene feature flow.
// Attach it to an empty GameObject in BattleScene only when testing this feature.
// Create two UI Buttons and connect them to WinBattleForTest and LoseBattleForTest.
public class BattleSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";
    public string failSceneName = "FailScene";
    public string finalWinSceneName = "FinalWinScene";

    [Header("Rewards")]
    public int victoryGoldReward = 10;

    public void WinBattleForTest()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        bool isFinalBoss = runState.IsPendingBossRoom();

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

        runState.CompletePendingRoom();
        GameManager.Instance.currentState = GameState.MapSelection;

        if (isFinalBoss)
        {
            SceneManager.LoadScene(finalWinSceneName);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    public void LoseBattleForTest()
    {
        GameManager.Instance.currentState = GameState.Menu;
        SceneManager.LoadScene(failSceneName);
        Debug.Log("Game Over");
        return;
    }
}
