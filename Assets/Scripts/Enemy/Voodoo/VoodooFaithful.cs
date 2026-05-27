using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoodooFaithful : Enemy
{
    [Header("Voodoo Summon")]
    [SerializeField] private GameObject spiritPrefab;
    [SerializeField] private int maxOwnSummons = 3;
    [SerializeField] private float summonCooldown = 4f;

    [Header("Summon Animation")]
    [SerializeField] private float summonSpawnDelay = 0.35f;

    [Header("Passive - Weak Curse")]
    [SerializeField] private float weakCurseChance = 0.2f;
    [SerializeField] private float weakCurseAttackPenalty = 1f;
    [SerializeField] private float weakCurseDuration = 3f;

    private readonly Dictionary<CharacterBase, Coroutine> curseCoroutines =
        new Dictionary<CharacterBase, Coroutine>();

    private float summonTimer = 1f;
    private bool summonInProgress = false;

    private readonly List<GameObject> ownSummons = new List<GameObject>();

    protected override void CombatLogic()
    {
        CleanupSummons();

        if (summonInProgress)
        {
            return;
        }

        // 玩家死光 / 場上沒有 Player 時，不召喚、不移動、不攻擊
        if (!HasAnyPlayerAlive())
        {
            Debug.Log($"{unitName} 找不到玩家，停止召喚");
            return;
        }

        summonTimer -= Time.deltaTime;

        if (CanSummon() && !isMoving)
        {
            summonTimer = summonCooldown;
            StartCoroutine(SummonSpiritRoutine());
            return;
        }

        // 沒有召喚時，使用 Enemy 原本的 A* 追擊與攻擊
        base.CombatLogic();
    }

    private IEnumerator SummonSpiritRoutine()
    {
        summonInProgress = true;

        int token = BeginAction();
        
        GameObject target = FindNearestPlayerByDistance();

        if (target != null)
        {
            FaceTarget(target);
        }


        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Call");
        }

        yield return new WaitForSeconds(summonSpawnDelay);

        if (!gameObject.activeInHierarchy)
        {
            summonInProgress = false;
            EndAction(token);
            yield break;
        }

        if (!HasAnyPlayerAlive())
        {
            summonInProgress = false;
            EndAction(token);
            yield break;
        }

        Vector3 spawnPos;

        if (!TryGetSpawnPosition(out spawnPos))
        {
            Debug.Log($"{unitName} 周圍沒有空格，取消召喚");
            summonInProgress = false;
            EndAction(token);
            yield break;
        }

        GameObject spirit = Instantiate(spiritPrefab, spawnPos, Quaternion.identity);
        spirit.tag = "Enemy";
        CharacterBase summonCharacter = spirit.GetComponent<CharacterBase>();

        if (summonCharacter != null && GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemyFromPrefab(summonCharacter, spiritPrefab);
        }

        ownSummons.Add(spirit);

        Debug.Log($"{unitName} 召喚小靈");

        yield return new WaitForSeconds(0.15f);

        summonInProgress = false;

        EndAction(token);
    }

    private bool CanSummon()
    {
        if (summonInProgress) return false;
        if (spiritPrefab == null) return false;
        if (summonTimer > 0f) return false;
        if (ownSummons.Count >= maxOwnSummons) return false;

        return true;
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPos)
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
                spawnPos = candidate;
                return true;
            }
        }

        spawnPos = transform.position;
        return false;
    }

    private bool IsCellOccupied(Vector3 pos)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Vector2Int targetCell = GridAStarPathfinder.WorldToGrid(pos);

        foreach (GameObject obj in enemies)
        {
            if (obj == null) continue;
            if (!obj.activeInHierarchy) continue;
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
            if (!obj.activeInHierarchy) continue;

            Vector2Int objCell = GridAStarPathfinder.WorldToGrid(obj.transform.position);

            if (objCell == targetCell)
            {
                return true;
            }
        }

        return false;
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