using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slime : Enemy
{
    void Start()
    {
        unitName = "史萊姆";
        health = 50f;
        attack = 5f;
        defense = 2f;
    }
}
