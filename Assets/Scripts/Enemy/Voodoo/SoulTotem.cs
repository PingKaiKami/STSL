using System;
using System.Collections.Generic;
using UnityEngine;

public class SoulTotem : TotemBase
{
    [Header("Soul Totem")]
    [SerializeField] private float healAmount = 3f;

    [Header("Heal Target Prefabs")]
    [SerializeField] private List<CharacterBase> healTargetPrefabs = new List<CharacterBase>();

    [Header("Fallback")]
    [SerializeField] private bool healOwnerIfNoMatchedTarget = true;

    protected override void Start()
    {
        showRangeOverlay = false;

        base.Start();

        VoodooSummonEvents.OnSummonDied += HandleSummonDied;
    }

    private void HandleSummonDied(CharacterBase summon)
    {
        if (summon == null) return;

        bool healedAny = HealMatchedSceneTargets();

        if (!healedAny && healOwnerIfNoMatchedTarget)
        {
            HealOwner();
        }
    }

    private bool HealMatchedSceneTargets()
    {
        bool healedAny = false;

        CharacterBase[] sceneCharacters = FindObjectsOfType<CharacterBase>();

        foreach (CharacterBase sceneCharacter in sceneCharacters)
        {
            if (sceneCharacter == null) continue;
            if (!sceneCharacter.gameObject.activeInHierarchy) continue;

            if (!IsMatchedHealTarget(sceneCharacter)) continue;

            HealCharacter(sceneCharacter);
            healedAny = true;
        }

        return healedAny;
    }

    private bool IsMatchedHealTarget(CharacterBase sceneCharacter)
    {
        if (sceneCharacter == null) return false;

        Type sceneType = sceneCharacter.GetType();

        foreach (CharacterBase prefabTarget in healTargetPrefabs)
        {
            if (prefabTarget == null) continue;

            Type prefabType = prefabTarget.GetType();

            if (sceneType == prefabType)
            {
                return true;
            }
        }

        return false;
    }

    private void HealOwner()
    {
        CharacterBase ownerCharacter = GetOwner();

        if (ownerCharacter == null) return;
        if (!ownerCharacter.gameObject.activeInHierarchy) return;

        HealCharacter(ownerCharacter);
    }

    private void HealCharacter(CharacterBase target)
    {
        if (target == null) return;
        if (!target.gameObject.activeInHierarchy) return;

        target.health += healAmount;

        PlayTargetEffect(target);

        Debug.Log(
            $"{unitName} 觸發靈魂回復：{target.unitName} 回復 {healAmount} HP，目前 HP={target.health}"
        );
    }

    public void AddHealTargetPrefab(CharacterBase targetPrefab)
    {
        if (targetPrefab == null) return;

        if (!healTargetPrefabs.Contains(targetPrefab))
        {
            healTargetPrefabs.Add(targetPrefab);
        }
    }

    public void RemoveHealTargetPrefab(CharacterBase targetPrefab)
    {
        if (targetPrefab == null) return;

        if (healTargetPrefabs.Contains(targetPrefab))
        {
            healTargetPrefabs.Remove(targetPrefab);
        }
    }

    public void ClearHealTargetPrefabs()
    {
        healTargetPrefabs.Clear();
    }

    protected override void OnDisable()
    {
        VoodooSummonEvents.OnSummonDied -= HandleSummonDied;

        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        VoodooSummonEvents.OnSummonDied -= HandleSummonDied;

        base.OnDestroy();
    }
}