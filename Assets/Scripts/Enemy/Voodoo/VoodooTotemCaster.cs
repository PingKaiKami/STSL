using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoodooTotemCaster : Enemy
{
    [Header("Totem Placement")]
    [SerializeField] private GameObject[] totemPrefabs;
    [SerializeField] private int maxOwnTotems = 2;
    [SerializeField] private float placeTotemCooldown = 5f;
    [SerializeField] private float placeTotemDelay = 0.35f;

    [Header("Totem Placement Direction")]
    [SerializeField] private float forwardPlaceChance = 0.7f;

    private float placeTotemTimer = 1f;
    private bool placingTotem = false;

    private readonly List<GameObject> ownTotems = new List<GameObject>();

    protected override void CombatLogic()
    {
        CleanupTotems();

        if (placingTotem)
        {
            return;
        }

        if (!HasAnyPlayerAlive())
        {
            Debug.Log($"{unitName} 找不到玩家，停止放置圖騰");
            return;
        }

        placeTotemTimer -= Time.deltaTime;

        if (CanPlaceTotem())
        {
            placeTotemTimer = placeTotemCooldown;
            StartCoroutine(PlaceTotemRoutine());
            return;
        }

        // 沒有放置圖騰時，使用 Enemy 原本的追擊 / 攻擊邏輯
        base.CombatLogic();
    }

    private bool CanPlaceTotem()
    {
        if (placingTotem) return false;
        if (totemPrefabs == null || totemPrefabs.Length == 0) return false;
        if (placeTotemTimer > 0f) return false;
        if (ownTotems.Count >= maxOwnTotems) return false;

        return true;
    }

    private IEnumerator PlaceTotemRoutine()
    {
        placingTotem = true;

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

        yield return new WaitForSeconds(placeTotemDelay);

        if (!gameObject.activeInHierarchy)
        {
            placingTotem = false;
            EndAction(token);
            yield break;
        }

        if (!HasAnyPlayerAlive())
        {
            placingTotem = false;
            EndAction(token);
            yield break;
        }

        Vector3 spawnPos;

        if (!TryGetTotemSpawnPosition(out spawnPos))
        {
            Debug.Log($"{unitName} 周圍沒有空格，取消放置圖騰");
            placingTotem = false;
            EndAction(token);
            yield break;
        }

        GameObject prefab = PickRandomTotemPrefab();

        if (prefab == null)
        {
            placingTotem = false;
            EndAction(token);
            yield break;
        }

        GameObject totem = Instantiate(prefab, spawnPos, Quaternion.identity);
        totem.tag = "Enemy";

        // 關鍵：把圖騰擁有者指定為這個圖騰師
        TotemBase totemBase = totem.GetComponent<TotemBase>();

        if (totemBase != null)
        {
            totemBase.InitOwner(this);
        }
        else
        {
            Debug.LogWarning($"{totem.name} 沒有 TotemBase，無法指定 owner");
        }

        ownTotems.Add(totem);

        Debug.Log($"{unitName} 放置圖騰：{totem.name}");

        yield return new WaitForSeconds(0.15f);

        placingTotem = false;

        EndAction(token);
    }

    private GameObject PickRandomTotemPrefab()
    {
        if (totemPrefabs == null || totemPrefabs.Length == 0)
        {
            return null;
        }

        int index = Random.Range(0, totemPrefabs.Length);
        return totemPrefabs[index];
    }

    private bool TryGetTotemSpawnPosition(out Vector3 spawnPos)
    {
        GameObject target = FindNearestPlayerByDistance();

        if (target != null && Random.value <= forwardPlaceChance)
        {
            Vector3 forwardDir = GetDirectionTowardTarget(target);
            Vector3 forwardCandidate = transform.position + forwardDir;

            if (!IsCellOccupied(forwardCandidate))
            {
                spawnPos = forwardCandidate;
                return true;
            }
        }

        Vector3[] directions =
        {
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down
        };

        ShuffleDirections(directions);

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

    private Vector3 GetDirectionTowardTarget(GameObject target)
    {
        Vector2Int selfCell = GridAStarPathfinder.WorldToGrid(transform.position);
        Vector2Int targetCell = GridAStarPathfinder.WorldToGrid(target.transform.position);

        Vector2Int diff = targetCell - selfCell;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return diff.x > 0 ? Vector3.right : Vector3.left;
        }

        return diff.y > 0 ? Vector3.up : Vector3.down;
    }

    private void ShuffleDirections(Vector3[] directions)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            int randomIndex = Random.Range(i, directions.Length);

            Vector3 temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }
    }

    private bool IsCellOccupied(Vector3 pos)
    {
        Vector2Int targetCell = GridAStarPathfinder.WorldToGrid(pos);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.activeInHierarchy) continue;
            if (enemy == gameObject) continue;

            Vector2Int enemyCell = GridAStarPathfinder.WorldToGrid(enemy.transform.position);

            if (enemyCell == targetCell)
            {
                return true;
            }
        }

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;

            Vector2Int playerCell = GridAStarPathfinder.WorldToGrid(player.transform.position);

            if (playerCell == targetCell)
            {
                return true;
            }
        }

        if (GridReservationManager.IsCellReservedByOther(gameObject, targetCell))
        {
            return true;
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

    private void CleanupTotems()
    {
        for (int i = ownTotems.Count - 1; i >= 0; i--)
        {
            if (ownTotems[i] == null || !ownTotems[i].activeInHierarchy)
            {
                ownTotems.RemoveAt(i);
            }
        }
    }

}