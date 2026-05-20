using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Card_text : MonoBehaviour
{
    private Card card;
    private TextMeshPro tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        card = GetComponentInParent<Card>();
    }

    public void UpdateText()
    {
        List<string> lines = new List<string>();

        lines.Add(card.cardName);

        if (card.att != 0) lines.Add($"att: {card.att.ToString("+0;-0")}");
        if (card.def != 0) lines.Add($"def: {card.def.ToString("+0;-0")}");
        
        if (card.hp != 0)  
        {
            string hpText = $"hp: {card.hp}";
            float lostHp = card.maxHp - card.hp; // 透過 maxHp 減去當前 hp 算出損失血量
            
            if (lostHp > 0)
            {
                hpText += $" (-{lostHp})";
            }
            lines.Add(hpText);
        }

        tmp.text = string.Join("\n", lines);
    }
}