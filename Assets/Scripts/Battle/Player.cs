using System.Collections.Generic;
using UnityEngine;

public class Player : CharacterBase
{
    private bool hasReservedCell = false;
    private Vector3 lastSafeWorldPosition;
    private float baseMoveSpeed;

    private const float StuckWindow = 0.025f;
    private int        _sampledPathCost = int.MaxValue;
    private float      _sampleTime      = -999f;
    private GameObject _trackedTarget;
    protected readonly HashSet<GameObject> _skippedTargets = new HashSet<GameObject>();

    [HideInInspector] public GameObject sourceCardPrefab;

    [Header("Animation")]
    [SerializeField] protected Animator animator;

    protected override void Start()
    {
        base.Start();
        baseMoveSpeed = moveSpeed;
        if (animator == null)
            animator = GetComponent<Animator>();
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
        Destroy(gameObject);
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

        // 最優先：出界時先回到界線內，不做任何戰鬥行為
        Vector2Int myCell = WorldToGrid(transform.position);
        if (!GridAStarPathfinder.IsWithinBounds(myCell))
        {
            ReturnToBounds(myCell);
            return;
        }

        CharacterBase tauntSource = GetTauntSource();
        bool isTaunted = tauntSource != null && tauntSource.gameObject.activeInHierarchy;

        GameObject targetEnemy = isTaunted ? tauntSource.gameObject : FindNearestEnemy();

        if (targetEnemy == null)
        {
            // 所有目標都被跳過 → 清空重試
            if (_skippedTargets.Count > 0)
            {
                _skippedTargets.Clear();
                targetEnemy = FindNearestEnemy();
            }
            if (targetEnemy == null)
            {
                Debug.Log("場上沒有敵人");
                return;
            }
        }

        // 目標換了就重置取樣
        if (targetEnemy != _trackedTarget)
        {
            _trackedTarget   = targetEnemy;
            _sampledPathCost = int.MaxValue;
            _sampleTime      = Time.time;
        }

        Vector2Int targetCell = WorldToGrid(targetEnemy.transform.position);

        Vector2Int pathNextStep;
        int pathCost;
        bool pathFound = GridAStarPathfinder.TryFindNextStepToPosition(
            gameObject, targetCell, attackRange, 20, out pathNextStep, out pathCost, false);
        if (!pathFound) pathCost = int.MaxValue;

        if (pathCost == 0)
        {
            TryAttack(targetEnemy);
            return;
        }

        // 找不到路徑：跳過卡路偵測，貪婪移動盡量靠近
        if (!pathFound)
        {
            MoveTowardEnemy(targetEnemy);
            return;
        }

        // 卡路偵測：每 StuckWindow 秒取樣一次，本次沒比上次更短就換目標
        if (!isTaunted)
        {
            if (Time.time - _sampleTime >= StuckWindow)
            {
                if (DebugPathDraw) Debug.Log($"[StuckCheck] {name} → {targetEnemy.name} 距離={pathCost} 步，最近步數={(_sampledPathCost == int.MaxValue ? "無" : _sampledPathCost.ToString())} 步");
                if (pathCost >= _sampledPathCost)
                {
                    _skippedTargets.Add(targetEnemy);
                    if (DebugPathDraw) Debug.Log($"[StuckCheck] {name} 沒進步，{targetEnemy.name} 加入黑名單（黑名單共 {_skippedTargets.Count} 個）");
                    _trackedTarget   = null;
                    _sampledPathCost = int.MaxValue;
                    return;
                }
                _sampledPathCost = pathCost;
                _sampleTime      = Time.time;
            }
        }

        MoveTowardEnemy(targetEnemy);
    }

    protected virtual bool DebugPathDraw => false;

    protected virtual void MoveTowardEnemy(GameObject target)
    {
        Vector2Int currentCell = GridAStarPathfinder.WorldToGrid(transform.position);
        Vector2Int targetCell  = GridAStarPathfinder.WorldToGrid(target.transform.position);

        // A* 優先
        Vector2Int nextStep;
        int cost;
        if (GridAStarPathfinder.TryFindNextStepToPosition(gameObject, targetCell, attackRange, 20, out nextStep, out cost, DebugPathDraw))
        {
            Vector2Int asDiff = nextStep - currentCell;
            MoveDirection aDir = Mathf.Abs(asDiff.x) >= Mathf.Abs(asDiff.y)
                ? (asDiff.x > 0 ? MoveDirection.Right : MoveDirection.Left)
                : (asDiff.y > 0 ? MoveDirection.Up    : MoveDirection.Down);
            if (TryMoveWithReservation(aDir)) return;
        }

        // 貪婪 fallback
        Vector2Int diff = targetCell - currentCell;
        MoveDirection primaryDir, secondaryDir;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            primaryDir   = diff.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            secondaryDir = diff.y > 0 ? MoveDirection.Up    : MoveDirection.Down;
        }
        else
        {
            primaryDir   = diff.y > 0 ? MoveDirection.Up    : MoveDirection.Down;
            secondaryDir = diff.x > 0 ? MoveDirection.Right : MoveDirection.Left;
        }

        if (TryMoveWithReservation(primaryDir)) return;
        if (TryMoveWithReservation(secondaryDir)) return;

        MoveDirection[] allDirs = { MoveDirection.Up, MoveDirection.Down, MoveDirection.Left, MoveDirection.Right };
        foreach (MoveDirection dir in allDirs)
        {
            if (dir == primaryDir || dir == secondaryDir) continue;
            if (TryMoveWithReservation(dir)) return;
        }
    }

    private void ReturnToBounds(Vector2Int outCell)
    {
        MoveDirection[] dirs    = { MoveDirection.Up, MoveDirection.Down, MoveDirection.Left, MoveDirection.Right };
        Vector2Int[]    offsets = { Vector2Int.up,    Vector2Int.down,    Vector2Int.left,    Vector2Int.right    };
        for (int i = 0; i < dirs.Length; i++)
        {
            if (GridAStarPathfinder.IsWithinBounds(outCell + offsets[i]))
            {
                TryMoveWithReservation(dirs[i]);
                return;
            }
        }
        // 所有鄰格也超界（深度出界），隨意嘗試任一方向
        foreach (MoveDirection dir in dirs)
            if (TryMoveWithReservation(dir)) return;
    }

    protected bool TryMoveWithReservation(MoveDirection dir)
    {
        Vector2Int currentCell = WorldToGrid(transform.position);
        Vector2Int targetCell = currentCell + DirectionToCellOffset(dir);

        if (!GridAStarPathfinder.IsWithinBounds(targetCell)) return false;

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

    protected virtual GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.activeInHierarchy) continue;
            if (_skippedTargets.Contains(enemy)) continue;

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
            float hpBefore = enemyStats.health;
            enemyStats.TakeDamage(attack);
            bool killed = hpBefore > 0f && enemyStats.health <= 0f;
            OnAttackLanded(enemyStats, killed);
        }
    }

    /// <summary>
    /// 攻擊命中後的 hook。子類 override 此方法來實作各自的充能邏輯。
    /// 例：直接傷害角色在此呼叫 GainSkillCharge()；
    ///     投射物角色改由 projectile callback 充能，可不 override。
    /// </summary>
    protected virtual void OnAttackLanded(CharacterBase victim, bool killed) { }

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

    private Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return GridReservationManager.WorldToGrid(worldPosition);
    }
}
