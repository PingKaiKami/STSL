using System.Collections;
using UnityEngine;

public class Enemy : CharacterBase
{
    [Header("Pathfinding")]
    [SerializeField] private int maxSearchDepth = 50;

    [Header("Animation")]
    [SerializeField] protected Animator animator;

    [Header("Debug")]
    [SerializeField] private bool debugOccupancy = false;

    [Header("Death")]
    [SerializeField] private float deathAnimationDelay = 0.8f;

    [Header("Action Animation")]
    [SerializeField] protected float attackAnimationDuration = 0.45f;

    private int actionToken = 0;
    private bool isDying = false;
    private bool hasReservedCell = false;
    private Vector3 lastSafeWorldPosition;
    private bool initializedSafePosition = false;
    [Header("Projectile Attack")]
    [SerializeField] protected bool useProjectileAttack = false;
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float projectileSpeed = 6f;
    [SerializeField] protected float projectileLaunchDelay = 0.2f;
    [SerializeField] protected float projectileSpawnDistance = 0.45f;
    [SerializeField] protected float projectileHitRadius = 0.2f;

    protected MoveDirection currentFacingDirection = MoveDirection.Down;
    protected virtual void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    protected virtual void Update()
    {
        if (isDying) return;
        if (GameManager.Instance == null) return;

        EnsureSafePositionInitialized();

        if (GameManager.Instance.currentState == GameState.Combat)
        {
            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
            }

            UpdateReservationLifecycle();

            CombatLogic();

            UpdateReservationLifecycle();
            UpdateFacingToTargetWhenIdle();

            UpdateMoveAnimation();
        }
    }
    protected void UpdateFacingToTargetWhenIdle()
    {
        if (animator == null) return;
        if (isMoving) return;
        if (isDying) return;

        // 如果正在 Attack / Call / Death，不要中途改方向
        if (animator.GetBool("IsActing")) return;

        GameObject target = FindNearestPlayerByDistance();

        if (target == null) return;

        FaceTarget(target);
    }

    private void EnsureSafePositionInitialized()
    {
        if (initializedSafePosition) return;

        lastSafeWorldPosition = transform.position;
        initializedSafePosition = true;
    }

    protected override void Die()
    {
        if (isDying) return;

        isDying = true;
        isMoving = false;

        GridReservationManager.ReleaseReservation(gameObject);
        hasReservedCell = false;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Call");
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsActing", true);
            animator.SetTrigger("Death");
        }

        Debug.Log($"{unitName} 播放死亡動畫");

        StartCoroutine(DieAfterAnimation());
    }

    private IEnumerator DieAfterAnimation()
    {
        yield return new WaitForSeconds(deathAnimationDelay);

        base.Die();
    }

    private void OnDisable()
    {
        GridReservationManager.ReleaseReservation(gameObject);
    }

    private void OnDestroy()
    {
        GridReservationManager.ReleaseReservation(gameObject);
    }

    protected virtual void CombatLogic()
    {
        if (HasStatus(StatusEffect.Stun)) return;
        if (isMoving) return;

        Vector2Int currentCell = WorldToGrid(transform.position);

        Vector2Int nextStep;
        GameObject pathTarget;

        bool hasPath = GridAStarPathfinder.TryFindNextStepToNearestPlayer(
            gameObject,
            attackRange,
            maxSearchDepth,
            out nextStep,
            out pathTarget
        );

        GameObject targetPlayer = pathTarget;

        if (targetPlayer == null)
        {
            targetPlayer = FindNearestPlayerByDistance();
        }

        if (targetPlayer == null)
        {
            Debug.Log($"{unitName} 找不到玩家");
            return;
        }

        Vector2Int targetCell = WorldToGrid(targetPlayer.transform.position);
        int distance = GetGridDistance(currentCell, targetCell);

        if (distance <= GetAttackRangeInGrid())
        {
            FaceTarget(targetPlayer);
            TryAttack(targetPlayer);
            return;
        }

        if (!hasPath)
        {
            if (debugOccupancy)
            {
                Debug.Log($"{unitName} 找不到可行路徑 | current={currentCell}, target={targetCell}");
            }

            return;
        }

        MoveDirection moveDir;

        if (!TryConvertStepToMoveDirection(currentCell, nextStep, out moveDir))
        {
            Debug.LogWarning($"{unitName} A* 下一格不是相鄰格 | current={currentCell}, next={nextStep}");
            return;
        }

        if (!GridReservationManager.TryReserveCell(gameObject, nextStep))
        {
            if (debugOccupancy)
            {
                Debug.Log($"{unitName} 預約失敗或下一格被佔用 | current={currentCell}, next={nextStep}, target={targetCell}");
            }

            return;
        }

        hasReservedCell = true;
        lastSafeWorldPosition = transform.position;

        PlayMoveAnimation(moveDir);

        Move(moveDir);
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
            if (debugOccupancy)
            {
                Debug.LogWarning($"{unitName} 移動完成後發現重疊，退回安全位置");
            }

            transform.position = lastSafeWorldPosition;
            isMoving = false;

            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
            }

            return;
        }

        lastSafeWorldPosition = transform.position;
    }

    protected void TryAttack(GameObject target)
    {
        if (target == null) return;
        if (attackTimer > 0f) return;

        Attack(target);
        attackTimer = attackTime;
    }

    protected virtual void Attack(GameObject target)
    {
        if (target == null) return;

        FaceTarget(target);

        int token = BeginAction();

        if (animator != null)
        {
            animator.ResetTrigger("Call");
            animator.SetTrigger("Attack");
        }

        if (useProjectileAttack && projectilePrefab != null)
        {
            MoveDirection fireDirection = currentFacingDirection;
            StartCoroutine(LaunchProjectileAfterDelay(fireDirection, token));
            return;
        }

        Debug.Log($"{unitName} 正在攻擊 {target.name}");

        CharacterBase targetStats = target.GetComponent<CharacterBase>();

        if (targetStats != null)
        {
            targetStats.TakeDamage(attack);
            OnAttackHit(targetStats);
        }

        StartCoroutine(EndActionAfter(attackAnimationDuration, token));
    }
    protected IEnumerator LaunchProjectileAfterDelay(MoveDirection fireDirection, int token)
    {
        yield return new WaitForSeconds(projectileLaunchDelay);

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{unitName} 沒有設定 Projectile Prefab");
            EndAction(token);
            yield break;
        }

        Vector3 origin = transform.position;
        Vector3 spawnPos = GetProjectileSpawnPosition(fireDirection);

        GameObject projectileObj = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.identity
        );

        VoodooProjectile projectile = projectileObj.GetComponent<VoodooProjectile>();

        if (projectile != null)
        {
            projectile.Init(
                this,
                origin,
                fireDirection,
                attack,
                projectileSpeed,
                attackRange,
                projectileHitRadius
            );
        }

        float remainingActionTime = Mathf.Max(
            0f,
            attackAnimationDuration - projectileLaunchDelay
        );

        yield return new WaitForSeconds(remainingActionTime);

        EndAction(token);
    }

    protected Vector3 GetProjectileSpawnPosition(MoveDirection direction)
    {
        Vector3 dir = Vector3.down;

        switch (direction)
        {
            case MoveDirection.Up:
                dir = Vector3.up;
                break;

            case MoveDirection.Down:
                dir = Vector3.down;
                break;

            case MoveDirection.Left:
                dir = Vector3.left;
                break;

            case MoveDirection.Right:
                dir = Vector3.right;
                break;
        }

        return transform.position + dir * projectileSpawnDistance;
    }
    public virtual void ResolveProjectileHit(CharacterBase targetStats, float projectileDamage)
    {
        if (targetStats == null) return;

        targetStats.TakeDamage(projectileDamage);

        OnAttackHit(targetStats);
    }
    protected virtual void OnAttackHit(CharacterBase targetStats)
    {
        // 子類別可覆寫，例如巫毒信徒的微弱詛咒
    }

    protected bool TryConvertStepToMoveDirection(
        Vector2Int currentCell,
        Vector2Int nextCell,
        out MoveDirection moveDir
    )
    {
        moveDir = MoveDirection.Down;

        Vector2Int diff = nextCell - currentCell;

        if (diff == Vector2Int.up)
        {
            moveDir = MoveDirection.Up;
            return true;
        }

        if (diff == Vector2Int.down)
        {
            moveDir = MoveDirection.Down;
            return true;
        }

        if (diff == Vector2Int.left)
        {
            moveDir = MoveDirection.Left;
            return true;
        }

        if (diff == Vector2Int.right)
        {
            moveDir = MoveDirection.Right;
            return true;
        }

        return false;
    }

    protected GameObject FindNearestPlayerByDistance()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;

            float dist = Vector2.Distance(player.transform.position, currentPos);

            if (dist < minDistance)
            {
                nearest = player;
                minDistance = dist;
            }
        }

        return nearest;
    }

    protected void FaceTarget(GameObject target)
    {
        if (target == null) return;

        Vector2Int selfCell = WorldToGrid(transform.position);
        Vector2Int targetCell = WorldToGrid(target.transform.position);

        Vector2Int diff = targetCell - selfCell;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            if (diff.x > 0)
            {
                SetFacingDirection(MoveDirection.Right);
            }
            else if (diff.x < 0)
            {
                SetFacingDirection(MoveDirection.Left);
            }
        }
        else
        {
            if (diff.y > 0)
            {
                SetFacingDirection(MoveDirection.Up);
            }
            else if (diff.y < 0)
            {
                SetFacingDirection(MoveDirection.Down);
            }
        }
    }

    protected void PlayMoveAnimation(MoveDirection dir)
    {
        if (animator == null) return;

        SetFacingDirection(dir);
        animator.SetBool("IsMoving", true);
    }

    protected void UpdateMoveAnimation()
    {
        if (animator == null) return;
        if (animator.GetBool("IsActing")) return;

        animator.SetBool("IsMoving", isMoving);
    }

    protected void SetFacingDirection(MoveDirection dir)
    {
        currentFacingDirection = dir;

        if (animator == null) return;

        int directionValue = 0;

        switch (dir)
        {
            case MoveDirection.Down:
                directionValue = 0;
                break;

            case MoveDirection.Up:
                directionValue = 1;
                break;

            case MoveDirection.Left:
                directionValue = 2;
                break;

            case MoveDirection.Right:
                directionValue = 3;
                break;
        }

        animator.SetInteger("Direction", directionValue);
    }

    protected int GetAttackRangeInGrid()
    {
        return Mathf.Max(1, Mathf.FloorToInt(attackRange));
    }

    protected int GetGridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    protected Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return GridReservationManager.WorldToGrid(worldPosition);
    }
    protected int BeginAction()
    {
        actionToken++;

        if (animator != null)
        {
            animator.SetBool("IsActing", true);
            animator.SetBool("IsMoving", false);
        }

        return actionToken;
    }

    protected void EndAction(int token)
    {
        if (token != actionToken) return;

        if (animator != null)
        {
            animator.SetBool("IsActing", false);
        }
    }

    protected IEnumerator EndActionAfter(float duration, int token)
    {
        yield return new WaitForSeconds(duration);

        EndAction(token);
    }
}