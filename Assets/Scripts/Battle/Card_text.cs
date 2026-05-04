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
        tmp.text = GetText(card.prefab);
    }

    private string GetText(GameObject prefab)
    {
        List<string> lines = new List<string>();
        CharacterBase cb = prefab.GetComponent<CharacterBase>();

        lines.Add(cb.unitName);
        lines.Add(cb.attack.ToString());
        lines.Add(cb.defense.ToString());
        lines.Add(cb.health.ToString());

        return string.Join("\n", lines);
    }

}
