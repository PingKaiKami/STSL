using UnityEngine;
using UnityEngine.SceneManagement;

// Optional bridge from your classmates' MapScene into this feature scene.
// Attach this to a button object in MapScene and connect the button OnClick to OpenMapGenerationScene.
public class OpenMapGenerationSceneButton : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapGenerationSceneName = "MapGenerationScene";

    public void OpenMapGenerationScene()
    {
        SceneManager.LoadScene(mapGenerationSceneName);
    }
}
