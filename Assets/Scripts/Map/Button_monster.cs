using UnityEngine;
using UnityEngine.SceneManagement;

public class Button_monster : MonoBehaviour
{
    public void Onclick()
    {
        SceneManager.LoadScene("BattleScene");
    }
}
