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
    public int lossHPPenalty = 30;

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
        RunStateManager runState = RunStateManager.EnsureExists();

        if (runState.IsPendingBossRoom())
        {
            GameManager.Instance.currentState = GameState.Menu;
            SceneManager.LoadScene(failSceneName);
            Debug.Log("Game Over");
            return;
        }

        PlayerRunState playerState = runState.GetPlayerState();
        int hpAfterLoss = Mathf.Max(0, playerState.currentHP - lossHPPenalty);
        runState.SetPlayerCurrentHP(hpAfterLoss);

        if (hpAfterLoss <= 0)
        {
            GameManager.Instance.currentState = GameState.Menu;
            SceneManager.LoadScene(failSceneName);
            Debug.Log("Game Over");
            return;
        }

        runState.CompletePendingRoom();

        if (HandManager.Instance != null)
        {
            HandManager.Instance.ResetHandAfterBattle();
        }

        Debug.Log("Current run HP after loss: " + hpAfterLoss);
        GameManager.Instance.currentState = GameState.MapSelection;
        SceneManager.LoadScene(mapSceneName);
    }
}
