using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardRuntimeData
{
    public string cardName;
    public float att;
    public float def;
    public float hp;
    public float maxHp;
    public GameObject originalPrefab; // 記錄這個數據原本對應專案裡的哪一個角色 Prefab
}
