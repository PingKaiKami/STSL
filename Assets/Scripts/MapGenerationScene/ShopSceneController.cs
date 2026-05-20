using UnityEngine;
using UnityEngine.SceneManagement;

// This script belongs to the MapGenerationScene feature flow.
// Attach it to an empty GameObject in ShopScene only when testing this feature.
// Create a UI Button and connect it to ReturnToMap.
public class ShopSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    public void ReturnToMap()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        SceneManager.LoadScene(mapSceneName);
        GameManager.Instance.currentState = GameState.MapSelection;
    }
}
