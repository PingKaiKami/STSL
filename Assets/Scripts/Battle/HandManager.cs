using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public float spacing = 2.0f;

    void Update()
    {
        ArrangeCards();
    }

    void ArrangeCards()
    {
        // 取得所有子物件 (假設卡片都在 Panel 下)
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
                // 計算目標位置
                Vector3 pos = new Vector3(startX + (i * spacing), -3.2f, 0);
                
                // 【核心修改】：直接把位置寫入卡片的變數裡
                cardScript.baseLocalPosition = pos;
            }
        }
    }
}