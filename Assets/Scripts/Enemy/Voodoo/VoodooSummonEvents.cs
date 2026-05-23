using System;
using UnityEngine;

public static class VoodooSummonEvents
{
    public static event Action<CharacterBase> OnSummonDied;

    public static void NotifySummonDied(CharacterBase summon)
    {
        if (summon == null) return;

        OnSummonDied?.Invoke(summon);
    }
}