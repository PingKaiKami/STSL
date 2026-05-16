using UnityEngine;
using UnityEngine.SceneManagement;

// This script belongs to the MapGenerationScene feature flow.
// Attach it to an empty GameObject in FailScene.
// Create a UI Button and connect it to RestartRun.
public class FailSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    public void RestartRun()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.ClearRun();
        SceneManager.LoadScene(mapSceneName);
    }
}
