using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// RestScene shows current HP, heals the player, writes PlayerRunState, then returns to the map.
public class RestSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    [Header("Rest Settings")]
    public int flatHealAmount = 30;

    [Header("UI References")]
    public Text hpText;
    public Text statusText;
    public Button restButton;
    public Button skipButton;

    private void Start()
    {
        EnsureUI();
        RefreshUI();
    }

    public void RestAndReturnToMap()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        PlayerRunState state = runState.GetPlayerState();
        int beforeHP = state.currentHP;
        int healedHP = Mathf.Min(state.currentHP + flatHealAmount, state.maxHP);

        runState.SetPlayerCurrentHP(healedHP);
        SetStatus("Rested: " + beforeHP + " -> " + healedHP + " HP");
        ReturnToMap();
    }

    public void ReturnToMap()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        SceneManager.LoadScene(mapSceneName);
    }

    private void RefreshUI()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        PlayerRunState state = runState.GetPlayerState();

        if (hpText != null)
        {
            hpText.text = "HP: " + state.currentHP + " / " + state.maxHP;
        }

        if (restButton != null)
        {
            Text buttonText = restButton.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = "Rest +" + flatHealAmount + " HP";
            }
        }

        SetStatus("Recover HP and return to the map.");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void EnsureUI()
    {
        if (hpText != null && restButton != null)
        {
            restButton.onClick.RemoveAllListeners();
            restButton.onClick.AddListener(RestAndReturnToMap);

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(ReturnToMap);
            }

            return;
        }

        Canvas canvas = SceneUIFactory.CreateCanvas("RestCanvas");
        SceneUIFactory.CreateText(canvas.transform, "Rest Site", 44, FontStyle.Bold, new Vector2(0f, 170f), new Vector2(720f, 80f));
        hpText = SceneUIFactory.CreateText(canvas.transform, "", 32, FontStyle.Normal, new Vector2(0f, 65f), new Vector2(720f, 80f));
        restButton = SceneUIFactory.CreateButton(canvas.transform, "Rest", new Vector2(0f, -45f), new Vector2(320f, 64f), RestAndReturnToMap);
        skipButton = SceneUIFactory.CreateButton(canvas.transform, "Leave", new Vector2(0f, -125f), new Vector2(220f, 54f), ReturnToMap);
        statusText = SceneUIFactory.CreateText(canvas.transform, "", 20, FontStyle.Normal, new Vector2(0f, -215f), new Vector2(800f, 54f));
        SceneUIFactory.EnsureEventSystem();
    }
}
