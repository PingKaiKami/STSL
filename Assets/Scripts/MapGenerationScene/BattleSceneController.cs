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

        if (HandManager.Instance != null)
        {
            HandManager.Instance.RecallAllPlayersToHand();
        }

        PlayerManager.EnsureExists().ModifyMoney(victoryGoldReward);
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
        RunStateManager runState = RunStateManager.EnsureExists();
        PlayerManager playerManager = PlayerManager.EnsureExists();
        bool hasLivesLeft = playerManager.LoseLife();

        if (HandManager.Instance != null)
        {
            HandManager.Instance.RecallAllPlayersToHand();
        }

        if (!hasLivesLeft)
        {
            GameManager.Instance.currentState = GameState.Menu;
            SceneManager.LoadScene(failSceneName);
            Debug.Log("Game Over");
            return;
        }

        if (runState.IsPendingBossRoom())
        {
            Debug.Log("Player lives after loss: " + playerManager.health);
            GameManager.Instance.currentState = GameState.MapSelection;
            SceneManager.LoadScene(mapSceneName);
            return;
        }

        runState.CompletePendingRoom();

        Debug.Log("Player lives after loss: " + playerManager.health);
        GameManager.Instance.currentState = GameState.MapSelection;
        SceneManager.LoadScene(mapSceneName);
    }
}
