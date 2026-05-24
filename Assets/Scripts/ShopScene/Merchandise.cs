using UnityEngine;

public enum MerchandiseActionType
{
    StatModifier,
    AddCardToDeck,
    RemoveTargetCard
}

public class Merchandise : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private int originalLayer;
    private Vector3 currentTargetLocal;
    private bool isDragging;
    private bool isHovering;
    private Card targetCard;

    [Header("Shop Item")]
    public MerchandiseActionType actionType = MerchandiseActionType.StatModifier;
    public string obj_name;
    public int price;
    public string cardId;
    public string unitPrefabId;

    [Header("Movement")]
    public float smoothSpeed = 10f;
    public Vector3 baseLocalPosition;
    public float moveUpOffset = 1.0f;

    [Header("Card Stat Modifiers")]
    public float att;
    public float def;
    public float hp;
    public float maxHp;

    private void Start()
    {
        if (baseLocalPosition == Vector3.zero)
        {
            baseLocalPosition = transform.localPosition;
        }

        RefreshText();
    }

    private void Update()
    {
        if (!isDragging)
        {
            currentTargetLocal = baseLocalPosition;

            if (isHovering)
            {
                currentTargetLocal += Vector3.up * moveUpOffset;
            }

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                currentTargetLocal,
                Time.deltaTime * smoothSpeed
            );
        }

        if (isDragging)
        {
            ScanForCardUnderMouse();
        }
    }

    private void ScanForCardUnderMouse()
    {
        if (Camera.main == null)
        {
            targetCard = null;
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);

        targetCard = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Card card = hits[i].GetComponent<Card>();

            if (card != null && hits[i].gameObject != gameObject)
            {
                targetCard = card;
                return;
            }
        }
    }

    private void OnMouseEnter()
    {
        if (!CanUseShopInput() || isDragging)
        {
            return;
        }

        isHovering = true;
    }

    private void OnMouseExit()
    {
        isHovering = false;
    }

    private void OnMouseDown()
    {
        if (!CanUseShopInput() || Camera.main == null)
        {
            return;
        }

        isDragging = true;
        originalLayer = gameObject.layer;
        gameObject.layer = 2;

        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 cursorScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        offset = transform.position - Camera.main.ScreenToWorldPoint(cursorScreenPoint);
    }

    private void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        isHovering = false;
        gameObject.layer = originalLayer;
        TryUse();
    }

    private void OnMouseDrag()
    {
        if (!CanUseShopInput() || Camera.main == null)
        {
            return;
        }

        Vector3 cursorScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        transform.position = Camera.main.ScreenToWorldPoint(cursorScreenPoint) + offset;
    }

    public void Configure(
        MerchandiseActionType newActionType,
        string newName,
        int newPrice,
        float newAtt,
        float newDef,
        float newHp,
        float newMaxHp,
        string newCardId,
        string newUnitPrefabId
    )
    {
        actionType = newActionType;
        obj_name = newName;
        price = newPrice;
        att = newAtt;
        def = newDef;
        hp = newHp;
        maxHp = newMaxHp;
        cardId = newCardId;
        unitPrefabId = newUnitPrefabId;
        baseLocalPosition = transform.localPosition;
        RefreshText();
    }

    public void RefreshText()
    {
        Merchandise_text text = GetComponentInChildren<Merchandise_text>();

        if (text != null)
        {
            text.UpdateText();
        }
    }

    private void TryUse()
    {
        switch (actionType)
        {
            case MerchandiseActionType.AddCardToDeck:
                TryBuyCard();
                break;

            case MerchandiseActionType.RemoveTargetCard:
                TryRemoveTargetCard();
                break;

            default:
                TryApplyStats();
                break;
        }
    }

    private void TryApplyStats()
    {
        if (targetCard == null)
        {
            return;
        }

        if (!TrySpendGold())
        {
            return;
        }

        targetCard.UpdateStats(att, def, hp, maxHp);

        if (HandManager.Instance != null)
        {
            HandManager.Instance.SyncRunStateFromHand();
        }

        NotifyShop("Bought " + obj_name + " for " + price + " Gold.");
        Destroy(gameObject);
    }

    private void TryBuyCard()
    {
        if (!TrySpendGold())
        {
            return;
        }

        string runtimeUnitPrefabId = string.IsNullOrEmpty(unitPrefabId) ? obj_name : unitPrefabId;

        if (HandManager.Instance == null || !HandManager.Instance.AddCardByUnitId(runtimeUnitPrefabId))
        {
            PlayerManager.EnsureExists().ModifyMoney(price);
            NotifyShop("Could not add card: " + obj_name + ".");
            return;
        }

        NotifyShop("Bought card: " + obj_name + ".");
        Destroy(gameObject);
    }

    private void TryRemoveTargetCard()
    {
        if (targetCard == null)
        {
            return;
        }

        if (!TrySpendGold())
        {
            return;
        }

        int cardIndex = targetCard.transform.GetSiblingIndex();
        Transform parent = targetCard.transform.parent;

        if (parent != null && parent.GetComponent<HandManager>() != null)
        {
            targetCard.transform.SetParent(null);
            Destroy(targetCard.gameObject);
        }
        else
        {
            if (HandManager.Instance == null || !HandManager.Instance.RemoveCardAt(0))
            {
                PlayerManager.EnsureExists().ModifyMoney(price);
                NotifyShop("No cards to remove.");
                return;
            }
        }

        string removedCardName = targetCard.cardName;

        if (HandManager.Instance != null)
        {
            HandManager.Instance.SyncRunStateFromHand();
        }

        NotifyShop("Removed card: " + removedCardName + ".");
        Destroy(gameObject);
    }

    private bool TrySpendGold()
    {
        if (price <= 0)
        {
            return true;
        }

        if (PlayerManager.EnsureExists().TrySpendGold(price))
        {
            return true;
        }

        NotifyShop("Not enough gold for " + obj_name + ".");
        return false;
    }

    private bool CanUseShopInput()
    {
        return GameManager.Instance != null && GameManager.Instance.currentState == GameState.Shopping;
    }

    private void NotifyShop(string message)
    {
        ShopSceneController controller = FindObjectOfType<ShopSceneController>();

        if (controller != null)
        {
            controller.RefreshShopUi(message);
        }
    }
}
