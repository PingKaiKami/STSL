using System;
using System.Collections.Generic;

[Serializable]
public class PlayerCardRuntimeData
{
    public string cardId;
    public string cardName;
    public string unitPrefabId;
    public float att;
    public float def;
    public float hp;
    public float maxHp;

    public PlayerCardRuntimeData()
    {
    }

    public PlayerCardRuntimeData(string cardId, string cardName, string unitPrefabId)
    {
        this.cardId = cardId;
        this.cardName = cardName;
        this.unitPrefabId = unitPrefabId;
    }

    public PlayerCardRuntimeData(string cardId, string cardName, string unitPrefabId, float att, float def, float hp, float maxHp)
    {
        this.cardId = cardId;
        this.cardName = cardName;
        this.unitPrefabId = unitPrefabId;
        this.att = att;
        this.def = def;
        this.hp = hp;
        this.maxHp = maxHp;
    }

    public PlayerCardRuntimeData(PlayerCardRuntimeData source)
    {
        if (source == null)
        {
            return;
        }

        cardId = source.cardId;
        cardName = source.cardName;
        unitPrefabId = source.unitPrefabId;
        att = source.att;
        def = source.def;
        hp = source.hp;
        maxHp = source.maxHp;
    }

    public PlayerCardRuntimeData Clone()
    {
        return new PlayerCardRuntimeData(this);
    }
}

[Serializable]
public class PlayerBattleStartData
{
    public int maxHP;
    public int currentHP;
    public int gold;
    public List<PlayerCardRuntimeData> cards = new List<PlayerCardRuntimeData>();

    public PlayerBattleStartData()
    {
    }

    public PlayerBattleStartData(PlayerRunState state)
    {
        if (state == null)
        {
            return;
        }

        maxHP = state.maxHP;
        currentHP = state.currentHP;
        gold = state.gold;
        cards = state.GetCardsSnapshot();
    }
}

[Serializable]
public class PlayerRunState
{
    public const int DefaultMaxHP = 80;
    public const int DefaultGold = 99;

    public int maxHP;
    public int currentHP;
    public int gold;
    public List<PlayerCardRuntimeData> cards = new List<PlayerCardRuntimeData>();

    public void InitializeNewRun()
    {
        maxHP = DefaultMaxHP;
        currentHP = DefaultMaxHP;
        gold = DefaultGold;
        EnsureCardsList();
        cards.Clear();
        AddStarterDeck();
    }

    public bool IsInitialized()
    {
        return maxHP > 0 && cards != null;
    }

    public void Normalize()
    {
        EnsureCardsList();

        if (maxHP <= 0)
        {
            maxHP = DefaultMaxHP;
        }

        currentHP = Clamp(currentHP, 0, maxHP);
        gold = Math.Max(0, gold);
    }

    public PlayerBattleStartData CreateBattleStartData()
    {
        Normalize();
        return new PlayerBattleStartData(this);
    }

    public List<PlayerCardRuntimeData> GetCardsSnapshot()
    {
        EnsureCardsList();

        List<PlayerCardRuntimeData> snapshot = new List<PlayerCardRuntimeData>();

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
            {
                snapshot.Add(cards[i].Clone());
            }
        }

        return snapshot;
    }

    public void SetCurrentHP(int newCurrentHP)
    {
        Normalize();
        currentHP = Clamp(newCurrentHP, 0, maxHP);
    }

    public void SetMaxHP(int newMaxHP)
    {
        maxHP = Math.Max(1, newMaxHP);
        currentHP = Clamp(currentHP, 0, maxHP);
    }

    public void SetGold(int newGold)
    {
        gold = Math.Max(0, newGold);
    }

    public void AddGold(int amount)
    {
        gold = Math.Max(0, gold + amount);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        Normalize();

        if (gold < amount)
        {
            return false;
        }

        gold -= amount;
        return true;
    }

    public void SetCards(IEnumerable<PlayerCardRuntimeData> newCards)
    {
        EnsureCardsList();
        cards.Clear();

        if (newCards == null)
        {
            return;
        }

        foreach (PlayerCardRuntimeData card in newCards)
        {
            AddCard(card);
        }
    }

    public void AddCard(PlayerCardRuntimeData card)
    {
        if (card == null)
        {
            return;
        }

        EnsureCardsList();
        cards.Add(card.Clone());
    }

    public bool RemoveCardById(string cardId)
    {
        EnsureCardsList();

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].cardId == cardId)
            {
                cards.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool RemoveCardAt(int index)
    {
        EnsureCardsList();

        if (index < 0 || index >= cards.Count)
        {
            return false;
        }

        cards.RemoveAt(index);
        return true;
    }

    private void AddStarterDeck()
    {
        AddCard(CreateStarterWarriorCard("starter_warrior_1"));
        AddCard(CreateStarterWarriorCard("starter_warrior_2"));
        AddCard(CreateStarterWarriorCard("starter_warrior_3"));
    }

    private PlayerCardRuntimeData CreateStarterWarriorCard(string cardId)
    {
        return new PlayerCardRuntimeData(cardId, "Warrior", "Warrior", 0f, 0f, 0f, 0f);
    }

    private void EnsureCardsList()
    {
        if (cards == null)
        {
            cards = new List<PlayerCardRuntimeData>();
        }
    }

    private int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
