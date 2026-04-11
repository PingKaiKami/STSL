using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Card_text : MonoBehaviour
{
    public Card card;
    private TextMeshPro tmp;
    void Start()
    {
        tmp = GetComponent<TextMeshPro>();
        tmp.text = GetText(card.health, card.att, card.def);
    }

    private string GetText(float h, float a, float d)
    {
        // 使用 List 來收集所有需要顯示的行
        List<string> lines = new List<string>();

        // 處理血量 (Health)
        if (h > 0) lines.Add($"HP + {h}");
        else if (h < 0) lines.Add($"HP - {Mathf.Abs(h)}");

        // 處理攻擊 (Attack)
        if (a > 0) lines.Add($"Atk + {a}");
        else if (a < 0) lines.Add($"Atk - {Mathf.Abs(a)}");

        // 處理防禦 (Defense)
        if (d > 0) lines.Add($"Def + {d}");
        else if (d < 0) lines.Add($"Def - {Mathf.Abs(d)}");

        // 使用 string.Join 將所有行合併，並在每一行之間插入換行符號 \n
        return string.Join("\n", lines);
    }

}
