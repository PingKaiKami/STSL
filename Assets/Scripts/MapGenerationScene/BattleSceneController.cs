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

    public void WinBattleForTest()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        SceneManager.LoadScene(mapSceneName);
    }

    public void LoseBattleForTest()
    {
        SceneManager.LoadScene(failSceneName);
    }
}
