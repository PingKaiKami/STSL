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
    public int victoryGoldReward = 20;

    public void WinBattleForTest()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        bool isFinalBoss = runState.IsPendingBossRoom();

        runState.AddPlayerGold(victoryGoldReward);
        runState.CompletePendingRoom();
        GameManager.Instance.currentState = GameState.MapSelection;
        PlayerManager.Instance.ModifyMoney(10);

        if (isFinalBoss)
        {
            SceneManager.LoadScene(finalWinSceneName);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    public void LoseBattleForTest()
    {
        if (PlayerManager.Instance.ModifyHealth(-1))
        {
            Debug.Log($"Current health: {PlayerManager.Instance.health}");
            GameManager.Instance.currentState = GameState.MapSelection;
            SceneManager.LoadScene(mapSceneName);
            return;
        }
        else
        {
            GameManager.Instance.currentState = GameState.Menu;
            SceneManager.LoadScene(failSceneName);
            Debug.Log("Game Over");
        }
    }
}
