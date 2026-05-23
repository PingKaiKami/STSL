using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ShopItemType
{
    Card,
    Equipment,
    RemoveCardService
}

[Serializable]
public class ShopItemData
{
    public ShopItemType itemType;
    public string itemId;
    public string itemName;
    public string description;
    public int price;
}

// ShopScene displays gold, sells cards/equipment/removal, writes PlayerRunState, then returns to the map.
public class ShopSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    [Header("Shop Prices")]
    public int cardPrice = 45;
    public int equipmentPrice = 60;
    public int removeCardPrice = 35;

    [Header("UI References")]
    public Text goldText;
    public Text deckText;
    public Text statusText;
    public RectTransform itemRoot;
    public Button leaveButton;

    private readonly List<ShopItemData> shopItems = new List<ShopItemData>();

    private void Start()
    {
        EnsureUI();
        CreateDefaultShopItems();
        RenderShopItems();
        RefreshUI("Choose an item.");
    }

    public void ReturnToMap()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        SceneManager.LoadScene(mapSceneName);
        GameManager.Instance.currentState = GameState.MapSelection;
    }

    public void BuyCard()
    {
        TryBuyItem(CreateCardItem());
    }

    public void BuyEquipment()
    {
        TryBuyItem(CreateEquipmentItem());
    }

    public void BuyRemoveCardService()
    {
        TryBuyItem(CreateRemoveCardServiceItem());
    }

    public void TryBuyItem(ShopItemData item)
    {
        if (item == null)
        {
            return;
        }

        RunStateManager runState = RunStateManager.EnsureExists();

        if (!runState.TrySpendPlayerGold(item.price))
        {
            RefreshUI("Not enough gold for " + item.itemName + ".");
            return;
        }

        if (item.itemType == ShopItemType.Card)
        {
            runState.AddPlayerCard(new PlayerCardRuntimeData(item.itemId, item.itemName, item.itemName));
            RefreshUI("Bought card: " + item.itemName + ".");
            return;
        }

        if (item.itemType == ShopItemType.Equipment)
        {
            runState.AddPlayerEquipment(new PlayerEquipmentRuntimeData(item.itemId, item.itemName, item.description));
            RefreshUI("Bought equipment: " + item.itemName + ".");
            return;
        }

        if (item.itemType == ShopItemType.RemoveCardService)
        {
            if (!runState.RemovePlayerCardAt(0))
            {
                runState.AddPlayerGold(item.price);
                RefreshUI("No cards to remove.");
                return;
            }

            RefreshUI("Removed the first card in your deck.");
        }
    }

    private void CreateDefaultShopItems()
    {
        shopItems.Clear();
        shopItems.Add(CreateCardItem());
        shopItems.Add(CreateEquipmentItem());
        shopItems.Add(CreateRemoveCardServiceItem());
    }

    private ShopItemData CreateCardItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.Card;
        item.itemId = "shop_card_warrior";
        item.itemName = "Warrior";
        item.description = "Adds one Warrior follower card to your deck.";
        item.price = cardPrice;
        return item;
    }

    private ShopItemData CreateEquipmentItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.Equipment;
        item.itemId = "shop_equipment_training_sword";
        item.itemName = "Training Sword";
        item.description = "Equipment bought from the shop.";
        item.price = equipmentPrice;
        return item;
    }

    private ShopItemData CreateRemoveCardServiceItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.RemoveCardService;
        item.itemId = "shop_service_remove_card";
        item.itemName = "Remove First Card";
        item.description = "Remove the first card from your deck.";
        item.price = removeCardPrice;
        return item;
    }

    private void RenderShopItems()
    {
        if (itemRoot == null)
        {
            return;
        }

        for (int i = itemRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(itemRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < shopItems.Count; i++)
        {
            ShopItemData capturedItem = shopItems[i];
            Vector2 position = new Vector2(0f, -i * 88f);
            string label = capturedItem.itemName + " - " + capturedItem.price + " Gold";
            Button button = SceneUIFactory.CreateButton(itemRoot, label, position, new Vector2(520f, 64f), delegate
            {
                TryBuyItem(capturedItem);
            });

            Text text = button.GetComponentInChildren<Text>();

            if (text != null)
            {
                text.fontSize = 20;
                text.resizeTextMaxSize = 20;
            }
        }
    }

    private void RefreshUI(string message)
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        PlayerRunState state = runState.GetPlayerState();

        if (goldText != null)
        {
            goldText.text = "Gold: " + state.gold;
        }

        if (deckText != null)
        {
            deckText.text = "Cards: " + state.cards.Count + " | Equipment: " + state.equipment.Count;
        }

        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void EnsureUI()
    {
        if (goldText != null && itemRoot != null)
        {
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(ReturnToMap);
            }

            return;
        }

        Canvas canvas = SceneUIFactory.CreateCanvas("ShopCanvas");
        SceneUIFactory.CreateText(canvas.transform, "Shop", 44, FontStyle.Bold, new Vector2(0f, 230f), new Vector2(720f, 80f));
        goldText = SceneUIFactory.CreateText(canvas.transform, "", 28, FontStyle.Bold, new Vector2(-260f, 155f), new Vector2(360f, 54f));
        deckText = SceneUIFactory.CreateText(canvas.transform, "", 22, FontStyle.Normal, new Vector2(240f, 155f), new Vector2(460f, 54f));

        GameObject itemRootObject = new GameObject("ShopItems");
        itemRootObject.transform.SetParent(canvas.transform, false);
        itemRoot = itemRootObject.AddComponent<RectTransform>();
        itemRoot.anchorMin = new Vector2(0.5f, 0.5f);
        itemRoot.anchorMax = new Vector2(0.5f, 0.5f);
        itemRoot.pivot = new Vector2(0.5f, 0.5f);
        itemRoot.anchoredPosition = new Vector2(0f, 60f);
        itemRoot.sizeDelta = new Vector2(620f, 260f);

        leaveButton = SceneUIFactory.CreateButton(canvas.transform, "Leave Shop", new Vector2(0f, -230f), new Vector2(260f, 58f), ReturnToMap);
        statusText = SceneUIFactory.CreateText(canvas.transform, "", 20, FontStyle.Normal, new Vector2(0f, -165f), new Vector2(860f, 54f));
        SceneUIFactory.EnsureEventSystem();
    }
}
