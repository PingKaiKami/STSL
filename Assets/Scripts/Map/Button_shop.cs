using UnityEngine;
using UnityEngine.SceneManagement;

public class Button_shop : MonoBehaviour
{
    public void Onclick()
    {
        SceneManager.LoadScene("ShopScene");
    }
}
