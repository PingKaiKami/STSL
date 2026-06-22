using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public int DefaultGold = 0;

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
    }

    public void ClearRunState()
    {
        money = 0;
    }

    public void Normalize()
    {
        money = Mathf.Max(0, money);
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
}
