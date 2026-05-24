using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Merchandise_text : MonoBehaviour
{
    public Merchandise merchandise;

    void Start()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        TextMeshPro text = GetComponent<TextMeshPro>();

        if (text != null)
        {
            text.text = GetText(merchandise);
        }
    }

    private string GetText(Merchandise input_merchandise)
    {
        List<string> lines = new List<string>();

        if (input_merchandise == null)
        {
            return "";
        }

        lines.Add(input_merchandise.obj_name);

        if (input_merchandise.price > 0)
        {
            lines.Add(input_merchandise.price + " Gold");
        }

        if (input_merchandise.att != 0)
        {
            lines.Add($"att {input_merchandise.att.ToString("+0;-0")}");
        }

        if (input_merchandise.def != 0)
        {
            lines.Add($"def {input_merchandise.def.ToString("+0;-0")}");
        }

        if (input_merchandise.hp != 0)
        {
            lines.Add($"hp {input_merchandise.hp.ToString("+0;-0")}");
        }

        if (input_merchandise.maxHp != 0)
        {
            lines.Add($"max hp {input_merchandise.maxHp.ToString("+0;-0")}");
        }

        return string.Join("\n", lines);
    }
}
