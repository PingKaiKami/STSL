using UnityEngine;
using UnityEngine.SceneManagement;

// This scene is shown after the final boss room is cleared.
public class FinalWinSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    public void StartNewRun()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.ClearRun();
        SceneManager.LoadScene(mapSceneName);
    }

    public void ReturnToMap()
    {
        StartNewRun();
    }
}
