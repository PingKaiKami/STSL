using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public const int DefaultLives = 3;
    public const int DefaultGold = 99;

    [Header("Global Player State")]
    public int money;
    public int health;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Normalize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static PlayerManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        PlayerManager existing = FindObjectOfType<PlayerManager>();

        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            existing.Normalize();
            return existing;
        }

        GameObject managerObject = new GameObject("PlayerManager");
        return managerObject.AddComponent<PlayerManager>();
    }

    public void InitializeNewRun()
    {
        money = DefaultGold;
        health = DefaultLives;
    }

    public void ClearRunState()
    {
        money = 0;
        health = DefaultLives;
    }

    public void Normalize()
    {
        money = Mathf.Max(0, money);

        if (health <= 0)
        {
            health = DefaultLives;
        }

        health = Mathf.Clamp(health, 0, DefaultLives);
    }

    public bool ModifyMoney(int amount)
    {
        if (money + amount < 0)
        {
            Debug.Log("u r too poor");
            return false;
        }

        money += amount;
        return true;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        return ModifyMoney(-amount);
    }

    public bool ModifyHealth(int amount)
    {
        int nextHealth = health + amount;

        if (nextHealth < 0)
        {
            health = 0;
            return false;
        }

        health = Mathf.Clamp(nextHealth, 0, DefaultLives);
        return health > 0;
    }

    public bool LoseLife()
    {
        return ModifyHealth(-1);
    }
}
