using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public void StartGameButton()
    {
        GameManager.Instance.currentState = GameState.MapSelection;
        SceneManager.LoadScene("MapGenerationScene");
    }
}
