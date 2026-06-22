using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Card_text : MonoBehaviour
{
    private Card card;
    private TextMeshPro tmp;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        card = GetComponentInParent<Card>();
    }

    public void UpdateText()
    {
        if (tmp == null || card == null)
        {
            return;
        }

        List<string> lines = new List<string>();
        lines.Add(card.cardName);

        // if (card.att != 0f)
        // {
        //     lines.Add("att: " + card.att.ToString("+0;-0"));
        // }

        // if (card.def != 0f)
        // {
        //     lines.Add("def: " + card.def.ToString("+0;-0"));
        // }

        // if (card.hp != 0f || card.maxHp != 0f)
        // {
        //     string hpText = card.maxHp > 0f
        //         ? "hp: " + card.hp + "/" + card.maxHp
        //         : "hp: " + card.hp;
        //     float lostHp = card.maxHp - card.hp;

        //     if (lostHp > 0f)
        //     {
        //         hpText += " (-" + lostHp + ")";
        //     }

        //     lines.Add(hpText);
        // }

        tmp.text = string.Join("\n", lines);
    }
}
