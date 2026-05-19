using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class VoodooFaithful : Enemy
{
    [Header("Voodoo Summon")]
    [SerializeField] private GameObject spiritPrefab;
    [SerializeField] private int maxOwnSummons = 3;
    [SerializeField] private float summonCooldown = 4f;

    [Header("Passive - Weak Curse")]
    [SerializeField] private float weakCurseChance = 0.2f;
    [SerializeField] private float weakCurseAttackPenalty = 1f;
    [SerializeField] private float weakCurseDuration = 3f;

    private readonly Dictionary<CharacterBase, Coroutine> curseCoroutines = new Dictionary<CharacterBase, Coroutine>();

    private float summonTimer = 1f;
    private readonly List<GameObject> ownSummons = new List<GameObject>();

    protected override void CombatLogic()
    {
        CleanupSummons();

        // 玩家死光 / 場上沒有 Player 時，不召喚、不移動、不攻擊
        if (!HasAnyPlayerAlive())
        {
            Debug.Log($"{unitName} 找不到玩家，停止召喚");
            return;
        }

        summonTimer -= Time.deltaTime;

        if (CanSummon())
        {
            SummonSpirit();
            summonTimer = summonCooldown;
            return;
        }

        // 沒有召喚時，使用 Enemy 原本的 A* 追擊與攻擊
        base.CombatLogic();
    }

    private bool HasAnyPlayerAlive()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players == null || players.Length == 0)
        {
            return false;
        }

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;

            CharacterBase character = player.GetComponent<CharacterBase>();

            if (character != null)
            {
                return true;
            }
        }

        return false;
    }
    protected override void OnAttackHit(CharacterBase targetStats)
    {
        base.OnAttackHit(targetStats);

        TryApplyWeakCurse(targetStats);
    }
    private void TryApplyWeakCurse(CharacterBase targetStats)
    {
        if (targetStats == null) return;

        if (Random.value > weakCurseChance)
        {
            return;
        }

        // 已經被詛咒時，刷新持續時間，不重複扣 ATK
        if (curseCoroutines.ContainsKey(targetStats))
        {
            StopCoroutine(curseCoroutines[targetStats]);
            curseCoroutines[targetStats] = StartCoroutine(WeakCurseRoutine(targetStats, false));
        }
        else
        {
            curseCoroutines[targetStats] = StartCoroutine(WeakCurseRoutine(targetStats, true));
        }

        Debug.Log($"{unitName} 觸發微弱詛咒：{targetStats.unitName} ATK -{weakCurseAttackPenalty}，持續 {weakCurseDuration} 秒");
    }
    private IEnumerator WeakCurseRoutine(CharacterBase targetStats, bool applyPenalty)
    {
        if (targetStats == null)
        {
            yield break;
        }

        if (applyPenalty)
        {
            targetStats.attack = Mathf.Max(0f, targetStats.attack - weakCurseAttackPenalty);
        }

        yield return new WaitForSeconds(weakCurseDuration);

        if (targetStats != null && targetStats.gameObject.activeInHierarchy)
        {
            targetStats.attack += weakCurseAttackPenalty;
        }

        if (curseCoroutines.ContainsKey(targetStats))
        {
            curseCoroutines.Remove(targetStats);
        }
    }
    private bool CanSummon()
    {
        if (spiritPrefab == null) return false;
        if (summonTimer > 0f) return false;
        if (ownSummons.Count >= maxOwnSummons) return false;

        return true;
    }

    private void SummonSpirit()
    {
        if (animator != null)
        {
            animator.SetTrigger("Cast");
        }

        Vector3 spawnPos = GetSpawnPosition();

        GameObject spirit = Instantiate(spiritPrefab, spawnPos, Quaternion.identity);
        spirit.tag = "Enemy";

        ownSummons.Add(spirit);

        Debug.Log($"{unitName} 召喚小靈");
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3[] directions =
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down
        };

        foreach (Vector3 dir in directions)
        {
            Vector3 candidate = transform.position + dir;

            if (!IsCellOccupied(candidate))
            {
                return candidate;
            }
        }

        // 四周都被擋住時，不應該硬召喚到右邊，否則可能疊格
        return transform.position;
    }

    private bool IsCellOccupied(Vector3 pos)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Vector2Int targetCell = GridAStarPathfinder.WorldToGrid(pos);

        foreach (GameObject obj in enemies)
        {
            if (obj == null) continue;
            if (obj == gameObject) continue;

            Vector2Int objCell = GridAStarPathfinder.WorldToGrid(obj.transform.position);

            if (objCell == targetCell)
            {
                return true;
            }
        }

        foreach (GameObject obj in players)
        {
            if (obj == null) continue;

            Vector2Int objCell = GridAStarPathfinder.WorldToGrid(obj.transform.position);

            if (objCell == targetCell)
            {
                return true;
            }
        }

        return false;
    }

    private void CleanupSummons()
    {
        for (int i = ownSummons.Count - 1; i >= 0; i--)
        {
            if (ownSummons[i] == null || !ownSummons[i].activeInHierarchy)
            {
                ownSummons.RemoveAt(i);
            }
        }
    }
}