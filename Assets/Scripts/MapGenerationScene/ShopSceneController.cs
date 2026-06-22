using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ShopItemType
{
    Card,
    StatModifier
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

// ShopScene displays gold, sells cards/stat upgrades, then returns to the map.
public class ShopSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mapSceneName = "MapGenerationScene";

    [Header("Shop Prices")]
    public int cardPrice = 45;
    public int swordPrice = 60;
    public int shieldPrice = 60;
    public int armorPrice = 60;

    [Header("Upgrade Amounts")]
    public float swordAttackBonus = 10f;
    public float shieldDefenseBonus = 5f;
    public float armorMaxHpBonus = 20f;

    [Header("UI References")]
    public Text goldText;
    public Text deckText;
    public Text statusText;
    public RectTransform itemRoot;
    public Button leaveButton;

    [Header("Merchandise Layout")]
    public Vector3 merchandiseStartPosition = new Vector3(-6f, 1.1f, 0f);
    public float merchandiseSpacing = 3f;
    public Vector2 merchandiseSize = new Vector2(2.2f, 3.2f);
    public Color merchandiseBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    public Color merchandiseBorderColor = new Color(0.37f, 0.12f, 0f, 1f);
    public Color merchandiseTextColor = Color.white;

    private readonly List<ShopItemData> shopItems = new List<ShopItemData>();
    private const int MerchandiseSlotCount = 5;
    private static Sprite generatedWhiteSprite;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentState = GameState.Shopping;
        }

        if (HandManager.Instance != null)
        {
            HandManager.Instance.RebuildHandFromRunState();
        }

        EnsureUI();
        BuildMerchandiseShelf();
        RefreshShopUi("Drag shop items onto cards.");
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

    public void BuySword()
    {
        RefreshUI("Drag Sword onto a card to buy it.");
    }

    public void TryBuyItem(ShopItemData item)
    {
        if (item == null)
        {
            return;
        }

        PlayerManager playerManager = PlayerManager.EnsureExists();

        if (item.itemType == ShopItemType.StatModifier)
        {
            RefreshUI("Drag " + item.itemName + " onto a card to buy it.");
            return;
        }

        if (!playerManager.TrySpendGold(item.price))
        {
            RefreshUI("Not enough gold for " + item.itemName + ".");
            return;
        }

        if (item.itemType == ShopItemType.Card)
        {
            if (HandManager.Instance == null || !HandManager.Instance.AddCardByUnitId(item.itemName))
            {
                playerManager.ModifyMoney(item.price);
                RefreshUI("Could not add card: " + item.itemName + ".");
                return;
            }

            RefreshUI("Bought card: " + item.itemName + ".");
            return;
        }

    }

    public void RefreshShopUi(string message)
    {
        RefreshUI(message);
    }

    private void CreateDefaultShopItems()
    {
        shopItems.Clear();

        for (int i = 0; i < MerchandiseSlotCount; i++)
        {
            shopItems.Add(CreateModifierItem(PickRandomUpgradeName()));
        }
    }

    private ShopItemData CreateCardItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.Card;
        item.itemId = "shop_card_warrior";
        item.itemName = "StormWarriar";
        item.description = "Adds one card to your deck.";
        item.price = cardPrice;
        return item;
    }

    private ShopItemData CreateSwordModifierItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.StatModifier;
        item.itemId = "shop_modifier_training_sword";
        item.itemName = "Sword";
        item.description = "Drag onto a card to add attack.";
        item.price = swordPrice;
        return item;
    }

    private ShopItemData CreateShieldModifierItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.StatModifier;
        item.itemId = "shop_modifier_training_shield";
        item.itemName = "Shield";
        item.description = "Drag onto a card to add defense.";
        item.price = shieldPrice;
        return item;
    }

    private ShopItemData CreateArmorModifierItem()
    {
        ShopItemData item = new ShopItemData();
        item.itemType = ShopItemType.StatModifier;
        item.itemId = "shop_modifier_training_armor";
        item.itemName = "Armor";
        item.description = "Drag onto a card to add max HP.";
        item.price = armorPrice;
        return item;
    }

    private ShopItemData CreateModifierItem(string upgradeName)
    {
        if (upgradeName == "Shield")
        {
            return CreateShieldModifierItem();
        }

        if (upgradeName == "Armor")
        {
            return CreateArmorModifierItem();
        }

        return CreateSwordModifierItem();
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

    private void BuildMerchandiseShelf()
    {
        ClearControlledMerchandise();

        for (int i = 0; i < MerchandiseSlotCount; i++)
        {
            CreateUpgradeMerchandise(PickRandomUpgradeName(), i);
        }
    }

    private string PickRandomUpgradeName()
    {
        int roll = UnityEngine.Random.Range(0, 3);

        if (roll == 0)
        {
            return "Sword";
        }

        if (roll == 1)
        {
            return "Shield";
        }

        return "Armor";
    }

    private void CreateUpgradeMerchandise(string upgradeName, int index)
    {
        if (upgradeName == "Shield")
        {
            CreateOrConfigureMerchandise(
                null,
                index,
                MerchandiseActionType.StatModifier,
                "Shield",
                shieldPrice,
                0f,
                shieldDefenseBonus,
                0f,
                0f,
                "",
                ""
            );
            return;
        }

        if (upgradeName == "Armor")
        {
            CreateOrConfigureMerchandise(
                null,
                index,
                MerchandiseActionType.StatModifier,
                "Armor",
                armorPrice,
                0f,
                0f,
                0f,
                armorMaxHpBonus,
                "",
                ""
            );
            return;
        }

        CreateOrConfigureMerchandise(
            null,
            index,
            MerchandiseActionType.StatModifier,
            "Sword",
            swordPrice,
            swordAttackBonus,
            0f,
            0f,
            0f,
            "",
            ""
        );
    }

    private void ClearControlledMerchandise()
    {
        Merchandise[] merchandises = FindObjectsOfType<Merchandise>();

        for (int i = 0; i < merchandises.Length; i++)
        {
            Merchandise merchandise = merchandises[i];

            if (merchandise == null)
            {
                continue;
            }

            if (merchandise.obj_name == "Sword"
                || merchandise.obj_name == "Shield"
                || merchandise.obj_name == "Armor"
                || merchandise.obj_name == "Warrior"
                || merchandise.obj_name == "StormWarriar"
                || merchandise.obj_name == "Remove Card")
            {
                Destroy(merchandise.gameObject);
            }
        }
    }

    private Merchandise FindExistingMerchandise(string itemName)
    {
        Merchandise[] merchandises = FindObjectsOfType<Merchandise>();

        for (int i = 0; i < merchandises.Length; i++)
        {
            if (merchandises[i] != null && merchandises[i].obj_name == itemName)
            {
                return merchandises[i];
            }
        }

        return null;
    }

    private void CreateOrConfigureMerchandise(
        Merchandise merchandise,
        int index,
        MerchandiseActionType actionType,
        string itemName,
        int itemPrice,
        float att,
        float def,
        float hp,
        float maxHp,
        string cardId,
        string unitPrefabId
    )
    {
        if (merchandise == null)
        {
            merchandise = CreateMerchandiseObject(itemName);
        }

        Vector3 position = merchandiseStartPosition + new Vector3(index * merchandiseSpacing, 0f, 0f);
        merchandise.transform.position = position;
        merchandise.transform.localPosition = position;
        merchandise.Configure(actionType, itemName, itemPrice, att, def, hp, maxHp, cardId, unitPrefabId);
    }

    private Merchandise CreateMerchandiseObject(string itemName)
    {
        GameObject itemObject = new GameObject(itemName + "Merchandise", typeof(BoxCollider2D), typeof(Merchandise));
        itemObject.transform.localScale = Vector3.one;

        BoxCollider2D collider = itemObject.GetComponent<BoxCollider2D>();
        collider.size = merchandiseSize;

        CreateSpriteChild(itemObject.transform, "BG", merchandiseBackgroundColor, merchandiseSize, 0);
        CreateSpriteChild(itemObject.transform, "Border", merchandiseBorderColor, merchandiseSize + new Vector2(0.2f, 0.2f), -1);
        CreateMerchandiseText(itemObject.transform);

        return itemObject.GetComponent<Merchandise>();
    }

    private void CreateSpriteChild(Transform parent, string objectName, Color color, Vector2 size, int sortingOrder)
    {
        GameObject child = new GameObject(objectName, typeof(SpriteRenderer));
        child.transform.SetParent(parent, false);
        child.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        renderer.sprite = GetGeneratedWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void CreateMerchandiseText(Transform parent)
    {
        GameObject textObject = new GameObject("text", typeof(TextMeshPro), typeof(Merchandise_text));
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = merchandiseTextColor;
        text.fontSize = 34f;
        text.enableWordWrapping = true;
        text.rectTransform.sizeDelta = new Vector2(22f, 10f);

        Merchandise_text merchandiseText = textObject.GetComponent<Merchandise_text>();
        merchandiseText.merchandise = parent.GetComponent<Merchandise>();
    }

    private Sprite GetGeneratedWhiteSprite()
    {
        if (generatedWhiteSprite != null)
        {
            return generatedWhiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        generatedWhiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return generatedWhiteSprite;
    }

    private void RefreshUI(string message)
    {
        PlayerManager playerManager = PlayerManager.EnsureExists();
        int cardCount = HandManager.Instance != null ? HandManager.Instance.CardCount : 0;

        if (goldText != null)
        {
            goldText.text = "Gold: " + playerManager.money;
        }

        if (deckText != null)
        {
            deckText.text = "Cards: " + cardCount;
        }

        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void EnsureUI()
    {
        if (goldText != null)
        {
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(ReturnToMap);
            }

            ApplyShopUILayout();
            return;
        }

        Canvas canvas = SceneUIFactory.CreateCanvas("ShopCanvas");
        SceneUIFactory.CreateText(canvas.transform, "Shop", 44, FontStyle.Bold, new Vector2(0f, 250f), new Vector2(720f, 80f));
        goldText = SceneUIFactory.CreateText(canvas.transform, "", 28, FontStyle.Bold, new Vector2(-440f, 252f), new Vector2(360f, 54f));
        deckText = SceneUIFactory.CreateText(canvas.transform, "", 22, FontStyle.Normal, new Vector2(380f, 252f), new Vector2(360f, 54f));

        leaveButton = SceneUIFactory.CreateButton(canvas.transform, "Leave Shop", new Vector2(500f, -310f), new Vector2(240f, 58f), ReturnToMap);
        statusText = SceneUIFactory.CreateText(canvas.transform, "", 20, FontStyle.Normal, new Vector2(0f, -75f), new Vector2(760f, 54f));
        SceneUIFactory.EnsureEventSystem();
        ApplyShopUILayout();
    }

    private void ApplyShopUILayout()
    {
        SetAnchoredPosition(goldText, new Vector2(-440f, 252f), new Vector2(360f, 54f));
        SetAnchoredPosition(deckText, new Vector2(380f, 252f), new Vector2(360f, 54f));
        SetAnchoredPosition(statusText, new Vector2(0f, -75f), new Vector2(760f, 54f));

        if (leaveButton != null)
        {
            RectTransform buttonTransform = leaveButton.GetComponent<RectTransform>();

            if (buttonTransform != null)
            {
                buttonTransform.anchoredPosition = new Vector2(500f, -310f);
                buttonTransform.sizeDelta = new Vector2(240f, 58f);
            }
        }
    }

    private void SetAnchoredPosition(Graphic graphic, Vector2 position, Vector2 size)
    {
        if (graphic == null)
        {
            return;
        }

        RectTransform rectTransform = graphic.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }
}
