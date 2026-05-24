using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandManager : MonoBehaviour
{
    public float spacing = 2.0f;
    public static HandManager Instance;
    public GameObject basicCard;
    public GameObject warriorPrefab;
    private bool handBuiltForCurrentPreparation;

    public int CardCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<Card>() != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePrefabReferences();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EnsurePrefabReferences();
        EnsureCardsReady();
        ArrangeCards();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.currentState == GameState.Preparation && !handBuiltForCurrentPreparation)
        {
            EnsureCardsReady();
            ArrangeCards();
            handBuiltForCurrentPreparation = true;
        }

        if (GameManager.Instance.currentState != GameState.Preparation)
        {
            handBuiltForCurrentPreparation = false;
        }

        if (GameManager.Instance.currentState != GameState.Preparation
            && GameManager.Instance.currentState != GameState.Shopping)
        {
            SetCardsActive(false);
            return;
        }

        SetCardsActive(true);
        ArrangeCards();
    }

    public void RebuildHandFromRunState()
    {
        EnsureCardsReady();
        ArrangeCards();
        RefreshAllCardText();
    }

    public void ResetHandAfterBattle()
    {
        RecallAllPlayersToHand();
        handBuiltForCurrentPreparation = false;
    }

    public void SyncRunStateFromHand()
    {
        EnsureCardsReady();
        ArrangeCards();
        RefreshAllCardText();
    }

    public bool AddCardByUnitId(string unitPrefabId)
    {
        EnsurePrefabReferences();
        GameObject unitPrefab = ResolveUnitPrefab(unitPrefabId);

        if (unitPrefab == null)
        {
            Debug.LogError("HandManager: no unit prefab found for " + unitPrefabId);
            return false;
        }

        GameObject templateCard = GetCardTemplate();

        if (templateCard == null)
        {
            Debug.LogError("HandManager: no card template is available.");
            return false;
        }

        GameObject newCardObject = Instantiate(templateCard, transform);
        newCardObject.SetActive(true);

        Card card = newCardObject.GetComponent<Card>();

        if (card != null)
        {
            ApplyUnitPrefabToCard(card, unitPrefab);
        }

        ArrangeCards();
        return true;
    }

    public bool RemoveCardAt(int index)
    {
        if (index < 0 || index >= transform.childCount)
        {
            return false;
        }

        Card card = transform.GetChild(index).GetComponent<Card>();

        if (card == null)
        {
            return false;
        }

        Destroy(card.gameObject);
        ArrangeCards();
        return true;
    }

    public void RestoreAllCardsToFullHealth()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card == null)
            {
                continue;
            }

            if (card.maxHp > 0f)
            {
                card.UpdateStats(0f, 0f, card.maxHp - card.hp);
            }
        }

        RefreshAllCardText();
    }

    private GameObject GetCardTemplate()
    {
        EnsurePrefabReferences();

        if (basicCard != null)
        {
            return basicCard;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card != null)
            {
                return card.gameObject;
            }
        }

        return null;
    }

    private void EnsureCardsReady()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card == null)
            {
                continue;
            }

            GameObject unitPrefab = card.sourceCardPrefab != null
                ? card.sourceCardPrefab
                : card.prefab != null
                    ? card.prefab
                    : ResolveUnitPrefab(card.cardName);

            if (unitPrefab == null)
            {
                continue;
            }

            if (card.prefab == null)
            {
                card.prefab = unitPrefab;
            }

            if (card.sourceCardPrefab == null)
            {
                card.sourceCardPrefab = unitPrefab;
            }

            bool hasNoStats = card.att == 0f && card.def == 0f && card.hp == 0f && card.maxHp == 0f;

            if (hasNoStats || string.IsNullOrEmpty(card.cardName))
            {
                ApplyUnitPrefabToCard(card, unitPrefab);
            }
        }
    }

    private void ApplyUnitPrefabToCard(Card card, GameObject unitPrefab)
    {
        if (card == null || unitPrefab == null)
        {
            return;
        }

        card.prefab = unitPrefab;
        card.sourceCardPrefab = unitPrefab;

        CharacterBase character = unitPrefab.GetComponent<CharacterBase>();

        if (character != null)
        {
            card.cardName = character.unitName;
            card.att = Mathf.RoundToInt(character.attack);
            card.def = Mathf.RoundToInt(character.defense);
            card.hp = Mathf.RoundToInt(character.health);
            card.maxHp = Mathf.RoundToInt(character.maxHealth > 0f ? character.maxHealth : character.health);
        }

        Card_text cardText = card.GetComponentInChildren<Card_text>();

        if (cardText != null)
        {
            cardText.UpdateText();
        }
    }

    private GameObject ResolveUnitPrefab(string unitPrefabId)
    {
        EnsurePrefabReferences();

        if (string.IsNullOrEmpty(unitPrefabId) && warriorPrefab != null)
        {
            return warriorPrefab;
        }

        if (unitPrefabId == "Warrior" && warriorPrefab != null)
        {
            return warriorPrefab;
        }

        if (basicCard != null)
        {
            Card basicCardScript = basicCard.GetComponent<Card>();

            if (basicCardScript != null && basicCardScript.prefab != null)
            {
                return basicCardScript.prefab;
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card == null || card.prefab == null)
            {
                continue;
            }

            CharacterBase character = card.prefab.GetComponent<CharacterBase>();

            if (character != null && character.unitName == unitPrefabId)
            {
                return card.prefab;
            }

            if (card.prefab.name == unitPrefabId)
            {
                return card.prefab;
            }
        }

        return null;
    }

    private void EnsurePrefabReferences()
    {
#if UNITY_EDITOR
        if (basicCard == null)
        {
            basicCard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Card.prefab");
        }

        if (warriorPrefab == null)
        {
            warriorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Warrior.prefab");
        }
#endif
    }

    public GameObject ResolveUnitPrefabForCard(string unitPrefabId)
    {
        return ResolveUnitPrefab(unitPrefabId);
    }

    private void ArrangeCards()
    {
        int count = transform.childCount;
        if (count == 0) return;

        float totalWidth = (count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            Card cardScript = child.GetComponent<Card>();

            if (cardScript != null)
            {
                cardScript.baseLocalPosition = new Vector3(startX + (i * spacing), -3.2f, 0);
            }
        }
    }

    private void SetCardsActive(bool isActive)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.activeSelf != isActive)
            {
                child.SetActive(isActive);
            }
        }
    }

    private void RefreshAllCardText()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Card_text cardText = transform.GetChild(i).GetComponentInChildren<Card_text>();

            if (cardText != null)
            {
                cardText.UpdateText();
            }
        }
    }

    public void AddCard(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("HandManager: card prefab is null.");
            return;
        }

        GameObject newCardObject = Instantiate(cardPrefab, transform);
        newCardObject.SetActive(true);

        Card card = newCardObject.GetComponent<Card>();

        if (card != null)
        {
            card.baseLocalPosition = new Vector3(0f, -3.2f, 0f);
        }

        EnsureCardsReady();
        ArrangeCards();
    }

    public void HealAllCardsPercentage(float percentage)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Card card = transform.GetChild(i).GetComponent<Card>();

            if (card == null)
            {
                continue;
            }

            int healAmount = Mathf.RoundToInt(card.maxHp * percentage);
            float targetHp = Mathf.Min(card.hp + healAmount, card.maxHp);
            card.UpdateStats(0f, 0f, targetHp - card.hp);
        }

        RefreshAllCardText();
    }

    public void RecallAllPlayersToHand()
    {
        GameObject[] playersOnField = GameObject.FindGameObjectsWithTag("Player");
        GameObject templateCard = GetCardTemplate();

        if (templateCard == null)
        {
            Debug.LogError("HandManager: no card template is available.");
            return;
        }

        foreach (GameObject playerObj in playersOnField)
        {
            CharacterBase character = playerObj.GetComponent<CharacterBase>();
            Player player = playerObj.GetComponent<Player>();

            if (character != null && player != null && player.sourceCardPrefab != null)
            {
                if (character.health <= 0f)
                {
                    Destroy(playerObj);
                    continue;
                }

                GameObject newCardObject = Instantiate(templateCard, transform);
                newCardObject.SetActive(true);

                Card card = newCardObject.GetComponent<Card>();

                if (card != null)
                {
                    card.cardName = character.unitName;
                    card.att = Mathf.RoundToInt(character.attack);
                    card.def = Mathf.RoundToInt(character.defense);
                    card.hp = Mathf.RoundToInt(character.health);
                    card.maxHp = Mathf.RoundToInt(character.maxHealth);
                    card.prefab = player.sourceCardPrefab;
                    card.sourceCardPrefab = player.sourceCardPrefab;

                    Card_text cardText = card.GetComponentInChildren<Card_text>();

                    if (cardText != null)
                    {
                        cardText.UpdateText();
                    }
                }
            }

            Destroy(playerObj);
        }

        ArrangeCards();
    }
}
