using UnityEngine;
using UnityEngine.SceneManagement;

// This script belongs to the MapGenerationScene feature flow.
// Attach it to an empty GameObject in BattleScene only when testing this feature.
// Create two UI Buttons and connect them to WinBattleForTest and LoseBattleForTest.
public class BattleSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    public void WinBattleForTest()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        GameManager.Instance.currentState = GameState.MapSelection;
        PlayerManager.Instance.ModifyMoney(10);
        SceneManager.LoadScene(mapSceneName);
    }

    public void LoseBattleForTest()
    {
        if (PlayerManager.Instance.ModifyHealth(-1))
        {
            Debug.Log($"Current health: {PlayerManager.Instance.health}");
            GameManager.Instance.currentState = GameState.MapSelection;
            SceneManager.LoadScene("MapGenerationScene");
        }
        else
        {
            GameManager.Instance.currentState = GameState.Menu;
            SceneManager.LoadScene("MenuScene");
            Debug.Log("Game Over");
        }
        SceneManager.LoadScene(mapSceneName);
    }
}
