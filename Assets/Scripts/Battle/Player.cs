using UnityEngine;

public class Player : CharacterBase
{
    private bool hasReservedCell = false;
    private Vector3 lastSafeWorldPosition;
    private float baseMoveSpeed;

    [Header("Animation")]
    [SerializeField] protected Animator animator;

    protected override void Start()
    {
        base.Start();
        baseMoveSpeed = moveSpeed;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        UpdateStatusEffects();  // 計時所有狀態並觸發 DoT

        if (GameManager.Instance.currentState == GameState.Combat)
        {
            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;

            EnsureSafePositionInitialized();
            UpdateReservationLifecycle();

            // 暈眩：完全跳過行動與技能蓄力
            if (!HasStatus(StatusEffect.Stun))
            {
                UpdateSkillCharge();
                CombatLogic();
            }

            UpdateReservationLifecycle();
        }
    }

    protected override void OnStatusApplied(StatusEffect type)
    {
        if (type == StatusEffect.Frostbite)
            moveSpeed = baseMoveSpeed * 0.5f;
    }

    protected override void OnStatusRemoved(StatusEffect type)
    {
        if (type == StatusEffect.Frostbite)
            moveSpeed = baseMoveSpeed;
    }

    protected override void OnKnockBackComplete(Vector2 newPosition)
    {
        lastSafeWorldPosition = newPosition;
        hasReservedCell = false;
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
        SnapToGrid();

        // 被嘲諷：強制以嘲諷來源為目標，否則尋找最近敵人
        CharacterBase tauntSource = GetTauntSource();
        GameObject targetEnemy = (tauntSource != null && tauntSource.gameObject.activeInHierarchy)
            ? tauntSource.gameObject
            : FindNearestEnemy();

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

    protected virtual void Attack(GameObject target)
    {
        Debug.Log($"正在攻擊 {target.name}");

        if (animator != null)
        {
            FaceTarget(target);
            animator.SetTrigger("Attack");
        }

        CharacterBase enemyStats = target.GetComponent<CharacterBase>();

        if (enemyStats != null)
        {
            enemyStats.TakeDamage(attack);
        }
    }

    protected void FaceTarget(GameObject target)
    {
        if (target == null || animator == null) return;

        Vector2 diff = (Vector2)target.transform.position - (Vector2)transform.position;

        int dirVal;
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
            dirVal = diff.x > 0 ? 3 : 2; // Right=3, Left=2
        else
            dirVal = diff.y > 0 ? 1 : 0; // Up=1, Down=0

        animator.SetInteger("Direction", dirVal);
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