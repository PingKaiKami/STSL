using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("玩家狀態")]
    public int money;
    public int health;
    void Awake()
    {
        if (Instance == null)
        {
            money = 0;
            health = 3;
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool ModifyMoney(int amount)
    {
        if(money + amount < 0)
        {
            Debug.Log("u r too poor");
            return false;
        }
        else
        {
            money += amount;
            return true;
        }
    }

    public bool ModifyHealth(int amount)
    {
        if(health + amount < 0)
        {
            return false;
        }
        else
        {
            health += amount;
            return true;
        }
    }
}
