using System;
using System.Collections.Generic;
using UnityEngine;

public static class VoodooTotemRegistry
{
    private static readonly HashSet<TotemBase> activeTotems = new HashSet<TotemBase>();

    public static event Action<int> OnTotemCountChanged;

    public static int ActiveTotemCount
    {
        get { return activeTotems.Count; }
    }

    public static void Register(TotemBase totem)
    {
        if (totem == null) return;

        if (activeTotems.Add(totem))
        {
            OnTotemCountChanged?.Invoke(activeTotems.Count);
        }
    }

    public static void Unregister(TotemBase totem)
    {
        if (totem == null) return;

        if (activeTotems.Remove(totem))
        {
            OnTotemCountChanged?.Invoke(activeTotems.Count);
        }
    }
}