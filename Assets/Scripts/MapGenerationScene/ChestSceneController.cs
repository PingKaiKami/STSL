using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ChestRewardType
{
    Gold,
    Follower
}

[Serializable]
public class ChestRewardData
{
    public ChestRewardType rewardType;
    public string rewardId;
    public string rewardName;
    public string description;
    public int goldAmount;
}

// ChestScene gives one random reward, then returns to the map.
public class ChestSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    [Header("UI References")]
    public Text titleText;
    public Text rewardText;
    public Text statusText;
    public Button claimButton;

    private ChestRewardData currentReward;
    private bool rewardClaimed;

    private void Start()
    {
        EnsureUI();
        RollReward();
        RefreshUI();
    }

    public void ClaimReward()
    {
        if (rewardClaimed)
        {
            ReturnToMap();
            return;
        }

        PlayerManager playerManager = PlayerManager.EnsureExists();

        if (currentReward.rewardType == ChestRewardType.Gold)
        {
            playerManager.ModifyMoney(currentReward.goldAmount);
        }
        else
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.AddCardByUnitId(currentReward.rewardName);
            }
        }

        rewardClaimed = true;
        SetStatus("Reward claimed.");
        ReturnToMap();
    }

    public void ReturnToMap()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        SceneManager.LoadScene(mapSceneName);
    }

    private void RollReward()
    {
        bool giveGold = UnityEngine.Random.Range(0, 2) == 0;

        if (giveGold)
        {
            currentReward = new ChestRewardData();
            currentReward.rewardType = ChestRewardType.Gold;
            currentReward.rewardId = "chest_gold";
            currentReward.rewardName = "Gold";
            currentReward.goldAmount = 40;
            currentReward.description = "+" + currentReward.goldAmount + " Gold";
            return;
        }

        currentReward = new ChestRewardData();
        currentReward.rewardType = ChestRewardType.Follower;
        currentReward.rewardId = "chest_follower_warrior";
        currentReward.rewardName = "Warrior";
        currentReward.description = "Follower card reward from a chest.";
    }

    private void RefreshUI()
    {
        if (titleText != null)
        {
            titleText.text = "Chest Reward";
        }

        if (rewardText != null)
        {
            rewardText.text = currentReward.rewardType
                + "\n"
                + currentReward.rewardName
                + "\n"
                + currentReward.description;
        }

        if (claimButton != null)
        {
            Text buttonText = claimButton.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = "Claim Reward";
            }
        }

        SetStatus("Choose the reward to add it to your run.");
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
        if (titleText != null && rewardText != null && claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(ClaimReward);
            return;
        }

        Canvas canvas = SceneUIFactory.CreateCanvas("ChestCanvas");
        titleText = SceneUIFactory.CreateText(canvas.transform, "Chest Reward", 44, FontStyle.Bold, new Vector2(0f, 170f), new Vector2(720f, 80f));
        rewardText = SceneUIFactory.CreateText(canvas.transform, "", 28, FontStyle.Normal, new Vector2(0f, 45f), new Vector2(720f, 150f));
        claimButton = SceneUIFactory.CreateButton(canvas.transform, "Claim Reward", new Vector2(0f, -115f), new Vector2(280f, 64f), ClaimReward);
        statusText = SceneUIFactory.CreateText(canvas.transform, "", 20, FontStyle.Normal, new Vector2(0f, -205f), new Vector2(800f, 54f));
        SceneUIFactory.EnsureEventSystem();
    }
}
