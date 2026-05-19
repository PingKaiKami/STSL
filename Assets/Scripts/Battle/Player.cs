using UnityEngine;

public class Player : CharacterBase
{
    private bool hasReservedCell = false;
    private Vector3 lastSafeWorldPosition;

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentState == GameState.Combat)
        {
            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
            }

            EnsureSafePositionInitialized();

            UpdateReservationLifecycle();

            CombatLogic();

            UpdateReservationLifecycle();
        }
    }

    private void EnsureSafePositionInitialized()
    {
        if (lastSafeWorldPosition == Vector3.zero)
        {
            lastSafeWorldPosition = transform.position;
        }
    }

    protected override void Die()
    {
        GridReservationManager.ReleaseReservation(gameObject);

        base.Die();
        Debug.Log("遊戲結束，請重新開始");
    }

    private void OnDisable()
    {
        GridReservationManager.ReleaseReservation(gameObject);
    }

    private void OnDestroy()
    {
        GridReservationManager.ReleaseReservation(gameObject);
    }

    public void CombatLogic()
    {
        if (isMoving) return;

        GameObject targetEnemy = FindNearestEnemy();

        if (targetEnemy == null)
        {
            Debug.Log("場上沒有敵人");
            return;
        }

        Vector2Int currentCell = WorldToGrid(transform.position);
        Vector2Int targetCell = WorldToGrid(targetEnemy.transform.position);

        int distance = GetGridDistance(currentCell, targetCell);

        if (distance <= GetAttackRangeInGrid())
        {
            TryAttack(targetEnemy);
            return;
        }

        Vector2Int diff = targetCell - currentCell;

        MoveDirection primaryDir;
        MoveDirection secondaryDir;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            primaryDir = diff.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            secondaryDir = diff.y > 0 ? MoveDirection.Up : MoveDirection.Down;
        }
        else
        {
            primaryDir = diff.y > 0 ? MoveDirection.Up : MoveDirection.Down;
            secondaryDir = diff.x > 0 ? MoveDirection.Right : MoveDirection.Left;
        }

        if (TryMoveWithReservation(primaryDir))
        {
            return;
        }

        TryMoveWithReservation(secondaryDir);
    }

    private bool TryMoveWithReservation(MoveDirection dir)
    {
        Vector2Int currentCell = WorldToGrid(transform.position);
        Vector2Int targetCell = currentCell + DirectionToCellOffset(dir);

        if (!GridReservationManager.TryReserveCell(gameObject, targetCell))
        {
            return false;
        }

        hasReservedCell = true;
        lastSafeWorldPosition = transform.position;

        Move(dir);
        return true;
    }

    private void UpdateReservationLifecycle()
    {
        if (isMoving) return;

        if (hasReservedCell || GridReservationManager.HasReservation(gameObject))
        {
            GridReservationManager.ReleaseReservation(gameObject);
            hasReservedCell = false;
        }

        Vector2Int currentCell = WorldToGrid(transform.position);

        if (GridReservationManager.IsCellOccupiedByOther(gameObject, currentCell))
        {
            transform.position = lastSafeWorldPosition;
            return;
        }

        lastSafeWorldPosition = transform.position;
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.activeInHierarchy) continue;

            float dist = Vector2.Distance(enemy.transform.position, currentPos);

            if (dist < minDistance)
            {
                nearest = enemy;
                minDistance = dist;
            }
        }

        return nearest;
    }

    private void TryAttack(GameObject target)
    {
        if (target == null) return;
        if (attackTimer > 0f) return;

        Attack(target);
        attackTimer = attackTime;
    }

    private void Attack(GameObject target)
    {
        Debug.Log($"正在攻擊 {target.name}");

        CharacterBase enemyStats = target.GetComponent<CharacterBase>();

        if (enemyStats != null)
        {
            enemyStats.TakeDamage(attack);
        }
    }

    private Vector2Int DirectionToCellOffset(MoveDirection dir)
    {
        switch (dir)
        {
            case MoveDirection.Up:
                return Vector2Int.up;

            case MoveDirection.Down:
                return Vector2Int.down;

            case MoveDirection.Left:
                return Vector2Int.left;

            case MoveDirection.Right:
                return Vector2Int.right;
        }

        return Vector2Int.zero;
    }

    private int GetAttackRangeInGrid()
    {
        return Mathf.Max(1, Mathf.FloorToInt(attackRange));
    }

    private int GetGridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return GridReservationManager.WorldToGrid(worldPosition);
    }
}