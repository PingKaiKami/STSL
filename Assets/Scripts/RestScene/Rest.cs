using UnityEngine;
using UnityEngine.SceneManagement;

public class Rest : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    public void Recovery()
    {
        if (HandManager.Instance != null)
        {
            HandManager.Instance.RestoreAllCardsToFullHealth();
        }

        RunStateManager.EnsureExists().CompletePendingRoom();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentState = GameState.MapSelection;
        }

        SceneManager.LoadScene(mapSceneName);
    }
}
