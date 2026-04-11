using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warrior : Player
{
    void Start()
    {        
        unitName = "Warrior";
        health = 100f;
        attack = 20f;
        defense = 5f;
    }
}
