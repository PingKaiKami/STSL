using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 狂風戰士 — 靈活防守，以速度與閃避化解攻擊
///
/// 面板:  HP 55 | ATK 3 | DEF 2 | RANGE 1 | SPEED 4 | ATK SPEED 1/s
/// 被動:  移動後本回合獲得 30% 閃避率
/// 技能:  風牆 / 忍耐 / 滴水穿石
/// 裝備:  輕風靴 / 風刃護腕 / 風衣
/// </summary>
public class StormWarrior : Player
{
    // [Header("Animation")]
    // public new Animator animator;

    [Header("狂風戰士屬性")]
    public float dodgeRate = 0f;
    public float critRate  = 0f;
    public float rangedDamageReduction = 0f;
    public float magicDamageMultiplier = 1f;

    [Header("技能設定")]
    public float windWallDuration      = 2f;    // 風牆持續秒數
    public float windWallKnockbackRange = 3f;   // 擊退有效範圍（格）
    public float endureDodgeBonus  = 0.25f; // 忍耐時閃避加成
    public float endureCritBonus   = 0.30f; // 忍耐時必殺加成

    [Header("召喚物設定")]
    public GameObject windWallPrefab;

    [Header("技能特效")]
    public GameObject windWallCastEffectPrefab;
    public GameObject endureEffectPrefab;

    // 動畫基準速度
    private float baseWalkSpeed;
    private float baseAttackTime;

    // 走路方向追蹤
    private Vector2 prevPosition;

    // 被動
    private bool hasMovedThisTurn = false;

    // 忍耐狀態
    private bool isEnduring = false;

    private bool isWindWallActive = false;
    private readonly List<GameObject> activeWalls = new List<GameObject>();
    private GameObject activeWindWallCastEffect = null;
    private GameObject activeEndureEffect = null;

    // 目標鎖定
    [Header("目標選定")]
    public float defenseCheckRadius = 5f; // 防守型：友軍被圍攻的判定半徑

    private GameObject lockedTarget = null; // 鎖定目標，擊敗前不重選

    // ─── 初始化 ───────────────────────────────────────────────

    protected override void Start()
    {
        unitName             = "StormWarrior";
        health               = 300f;
        attack               = 20f;
        defense              = 2f;
        attackRange          = 1.5f;
        moveSpeed            = 3f;
        attackTime           = 1.0f;
        skillChargeInterval  = 0.05f;   // 每 1 秒 +1 衝能，改這裡即可

        ApplyEquipment();
        baseWalkSpeed  = moveSpeed;
        baseAttackTime = attackTime;
        base.Start();   // → Player.Start() → CharacterBase.Start()

        prevPosition = transform.position;
    }

    private void ApplyEquipment()
    {
        moveSpeed             += 1f;
        dodgeRate             += 0.10f;
        rangedDamageReduction += 0.20f;
        magicDamageMultiplier  = 1.20f;
    }

    // ─── 被動：移動後閃避提升 ─────────────────────────────────

    private void LateUpdate()
    {
        if (animator == null) return;

        Vector2 delta = (Vector2)transform.position - prevPosition;
        if (isMoving && delta.sqrMagnitude > 0.0001f)
        {
            int dirVal;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                dirVal = delta.x > 0 ? 3 : 2; // Right=3, Left=2
            else
                dirVal = delta.y > 0 ? 1 : 0; // Up=1, Down=0

            animator.SetInteger("Direction", dirVal);
        }
        prevPosition = transform.position;
    }

    protected override void OnMoveStart()
    {
        hasMovedThisTurn = false;
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            if (baseWalkSpeed > 0f)
                animator.speed = moveSpeed / baseWalkSpeed;
        }
    }

    protected override void OnMoveComplete()
    {
        hasMovedThisTurn = true;
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.speed = 1f;
        }
    }

    // ─── 傷害系統 ─────────────────────────────────────────────

    public override void TakeDamage(float damage)
    {
        float activeDodge = dodgeRate + (hasMovedThisTurn ? 0.30f : 0f);
        if (Random.value < activeDodge)
        {
            DamagePopup.Create(transform.position, 0, isMiss: true);
            return;
        }
        base.TakeDamage(damage);
    }

    public override void TakeDamage(float damage, DamageType type)
    {
        switch (type)
        {
            case DamageType.Ranged:
                damage *= (1f - rangedDamageReduction);
                TakeDamage(damage);
                break;
            case DamageType.Magic:
                damage *= magicDamageMultiplier;
                base.TakeDamage(damage);
                break;
            default:
                TakeDamage(damage);
                break;
        }
    }

    // ─── 普通攻擊（覆寫）─────────────────────────────────────


    protected override void Attack(GameObject target)
    {
        // 忍耐中：封鎖普通攻擊
        // if (isEnduring) return;

        if (animator != null)
        {
            FaceTarget(target);
            if (baseAttackTime > 0f)
                animator.speed = baseAttackTime / attackTime;
            Debug.Log($"Set Trigger Attack, animation speed: {animator.speed}");
            animator.SetTrigger("Attack");
        }

        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

        bool isCrit = Random.value < critRate;
        float dmg   = isCrit ? attack * 2f : attack;
        if (isCrit) Debug.Log($"{unitName} 必殺！");

        Debug.Log($"{unitName} 攻擊 {enemy.unitName}，造成 {dmg} 傷害");
        enemy.TakeDamage(dmg);
        StartCoroutine(ResetAnimatorSpeed(attackTime));
    }

    private IEnumerator ResetAnimatorSpeed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null) animator.speed = 1f;
    }

    // ─── 技能決策 ─────────────────────────────────────────────

    protected override void UseSkill()
    {
        // 忍耐第二回合：優先觸發兩倍滴水穿石
        if (isEnduring)
        {
            EndureAndStrike();
            return;
        }

        int enemyCount = CountActiveEnemies();

        // 技能1：風牆（場上敵人超過 2 個）
        if (enemyCount >= 2)
        {
            if (!isWindWallActive)
                StartCoroutine(WindWallRoutine());
            return;
        }

        // 技能2：忍耐（只有一個敵人，且血量 >= 50%）
        if (enemyCount == 1 && health >= maxHealth * 0.5f)
        {
            TriggerEndurance();
            return;
        }

        // 技能3：滴水穿石（預設）
        GameObject target = FindNearestEnemyObj();
        if (target != null)
            DrippingStoneStrike(target, 1f);
    }

    // ─── 技能1：風牆 ─────────────────────────────────────────

    private IEnumerator WindWallRoutine()
    {
        SpawnWindWallCastEffect();

        // 與 VoodooFaithful 相同：以世界座標方向陣列對齊 Grid 方向陣列
        Vector3[]   worldDirs = { Vector3.up,       Vector3.down,      Vector3.left,      Vector3.right      };
        Vector2Int[] gridDirs = { Vector2Int.up,    Vector2Int.down,   Vector2Int.left,   Vector2Int.right   };

        // 用點積找「最接近最近敵人」的方向 → 保持開放
        int openIdx = -1;
        GameObject nearest = FindNearestEnemyObj();
        if (nearest != null)
        {
            Vector2 toEnemy = (Vector2)nearest.transform.position - (Vector2)transform.position;
            float   bestDot = -1f;
            for (int i = 0; i < 4; i++)
            {
                float d = Vector2.Dot(toEnemy.normalized, new Vector2(gridDirs[i].x, gridDirs[i].y));
                if (d > bestDot) { bestDot = d; openIdx = i; }
            }
        }

        // Step 1：先對三側執行擊退
        for (int i = 0; i < 4; i++)
        {
            if (i == openIdx) continue;
            KnockBackEnemiesInDir(gridDirs[i]);
        }

        // 短暫等待，讓擊退 Coroutine 啟動、敵人開始位移
        yield return new WaitForSeconds(0.15f);

        // Step 2：放置風牆（與 VoodooFaithful.TryGetSpawnPosition 相同：transform.position + dir）
        isWindWallActive = true;
        activeWalls.Clear();
        for (int i = 0; i < 4; i++)
        {
            if (i == openIdx) continue;

            Vector3 wallWorldPos = transform.position + worldDirs[i];
            GameObject wall = windWallPrefab != null
                ? Instantiate(windWallPrefab, wallWorldPos, Quaternion.identity)
                : new GameObject("WindWall");
            wall.name = "WindWall";
            wall.transform.position = wallWorldPos;
            wall.tag = "Player";
            // 視為「玩家」障礙物：A* 與 GridReservation 都會避開此格
            wall.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
            activeWalls.Add(wall);

            Vector2Int wallCell = GridAStarPathfinder.WorldToGrid(wallWorldPos);
            GridReservationManager.ForceReserveCell(wall, wallCell);
        }

        Debug.Log($"{unitName} 生成風牆！封鎖三側，{windWallDuration} 秒後消散");
        yield return new WaitForSeconds(windWallDuration);

        ClearWindWalls();
        Debug.Log($"{unitName} 風牆消散");
    }

    /// <summary>
    /// 對 dir 方向這一側（60° 錐角內）的所有敵人施加擊退。
    /// </summary>
    private void KnockBackEnemiesInDir(Vector2Int dir)
    {
        Vector2 pushDir = new Vector2(dir.x, dir.y).normalized;
        Vector2 myPos   = transform.position;

        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (e == null || !e.activeInHierarchy) continue;

            Vector2 toEnemy = (Vector2)e.transform.position - myPos;
            if (toEnemy.magnitude > windWallKnockbackRange) continue;
            if (Vector2.Dot(toEnemy.normalized, pushDir) <= 0.5f) continue;

            CharacterBase enemy = e.GetComponent<CharacterBase>();
            if (enemy == null) continue;

            enemy.ApplyStatus(StatusEffect.KnockBack, 0f,
                knockDir: pushDir, knockDist: 1f);
        }
    }

    // ─── 技能2：忍耐 ─────────────────────────────────────────

    private void TriggerEndurance()
    {
        isEnduring  = true;
        dodgeRate  += endureDodgeBonus;
        critRate   += endureCritBonus;
        animator?.SetBool(endureAnimBool, true);
        SpawnEndureEffect();
        Debug.Log($"{unitName} 進入忍耐！閃避+{endureDodgeBonus:P0} 必殺+{endureCritBonus:P0}，下回合觸發兩倍滴水穿石");
    }

    private void EndureAndStrike()
    {
        isEnduring  = false;
        dodgeRate  -= endureDodgeBonus;
        critRate   -= endureCritBonus;
        animator?.SetBool(endureAnimBool, false);

        if (activeEndureEffect != null)
        {
            Destroy(activeEndureEffect);
            activeEndureEffect = null;
        }

        GameObject target = FindNearestEnemyObj();
        if (target != null)
        {
            Debug.Log($"{unitName} 忍耐爆發！兩倍滴水穿石！");
            DrippingStoneStrike(target, 2f);
        }
        else
        {
            Debug.Log($"{unitName} 忍耐結束，場上已無敵人");
        }
    }

    // ─── 技能3：滴水穿石 ──────────────────────────────────────

    [Header("動畫參數")]
    public string drippingStoneAnimTrigger = "DrippingStone"; // Trigger
    public string endureAnimBool           = "IsEnduring";    // Bool
    public string windWallAnimBool         = "IsWindWall";    // Bool

    /// <summary>穿透防禦的真實傷害攻擊。multiplier = 1（正常）或 2（忍耐爆發）</summary>
    private void DrippingStoneStrike(GameObject target, float multiplier)
    {
        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

        if (animator != null)
        {
            FaceTarget(target);
            animator.SetTrigger(drippingStoneAnimTrigger);
        }

        float damage = attack * multiplier;
        Debug.Log($"{unitName} 滴水穿石！穿透真實傷害 {damage}（x{multiplier}）");
        enemy.TakeTrueDamage(damage);
    }

    // ─── 輔助方法 ──────────────────────────────────────────────

    private int CountActiveEnemies()
    {
        int count = 0;
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
            if (e != null && e.activeInHierarchy) count++;
        return count;
    }

    private GameObject FindNearestEnemyObj()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   nearest = null;
        float        minDist = Mathf.Infinity;
        Vector2      pos     = transform.position;

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;
            float d = Vector2.Distance(e.transform.position, pos);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    // ─── 移動目標選取（鎖定機制）─────────────────────────────────

    protected override GameObject FindNearestEnemy()
    {
        // 鎖定目標仍存活 → 繼續追擊，不重選
        if (lockedTarget != null && lockedTarget.activeInHierarchy)
            return lockedTarget;

        // 目標已擊敗或尚無目標 → 50/50 重新選定
        lockedTarget = Random.value < 0.5f ? FindAggressiveTarget() : FindDefensiveTarget();
        return lockedTarget ?? FindNearestEnemyObj();
    }

    /// <summary>進攻型：可擊殺 → 血量最低 → 離自己最遠</summary>
    private GameObject FindAggressiveTarget()
    {
        GameObject[] enemies   = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   best      = null;
        float        bestScore = float.NegativeInfinity;
        Vector2      myPos     = transform.position;

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;
            CharacterBase cb = e.GetComponent<CharacterBase>();
            if (cb == null) continue;

            float score = 0f;
            if (cb.health <= attack) score += 10000f;
            score += 5000f / (cb.health + 1f);
            score += Vector2.Distance(e.transform.position, myPos);
            if (score > bestScore) { bestScore = score; best = e; }
        }
        return best;
    }

    /// <summary>防守型：威脅被圍攻友軍的敵人 → 威脅最近隊友的敵人 → 最近敵人</summary>
    private GameObject FindDefensiveTarget()
    {
        GameObject besiegedAlly = FindMostBesiegedAlly();
        if (besiegedAlly != null)
        {
            GameObject threat = FindNearestEnemyTo(besiegedAlly.transform.position);
            if (threat != null) return threat;
        }

        GameObject nearestAlly = FindNearestAllyExcludeSelf();
        if (nearestAlly != null)
        {
            GameObject threat = FindNearestEnemyTo(nearestAlly.transform.position);
            if (threat != null) return threat;
        }

        return FindNearestEnemyObj();
    }

    private GameObject FindMostBesiegedAlly()
    {
        GameObject[] allies  = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   bestAlly = null;
        int          maxCount = 1; // 至少 2 名敵人才算圍攻

        foreach (GameObject ally in allies)
        {
            if (ally == null || !ally.activeInHierarchy) continue;
            Vector2 allyPos = ally.transform.position;
            int count = 0;
            foreach (GameObject e in enemies)
            {
                if (e == null || !e.activeInHierarchy) continue;
                if (Vector2.Distance(e.transform.position, allyPos) <= defenseCheckRadius)
                    count++;
            }
            if (count > maxCount) { maxCount = count; bestAlly = ally; }
        }
        return bestAlly;
    }

    private GameObject FindNearestAllyExcludeSelf()
    {
        GameObject[] allies  = GameObject.FindGameObjectsWithTag("Player");
        GameObject   nearest = null;
        float        minDist = Mathf.Infinity;
        Vector2      myPos   = transform.position;

        foreach (GameObject p in allies)
        {
            if (p == null || !p.activeInHierarchy || p == gameObject) continue;
            float d = Vector2.Distance(p.transform.position, myPos);
            if (d < minDist) { minDist = d; nearest = p; }
        }
        return nearest;
    }

    private GameObject FindNearestEnemyTo(Vector2 pos)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   nearest = null;
        float        minDist = Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;
            float d = Vector2.Distance(e.transform.position, pos);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    private void ClearWindWalls()
    {
        foreach (GameObject wall in activeWalls)
        {
            if (wall == null) continue;
            GridReservationManager.ReleaseReservation(wall);
            Destroy(wall);
        }
        activeWalls.Clear();
        isWindWallActive = false;
        animator?.SetBool(windWallAnimBool, false);

        if (activeWindWallCastEffect != null)
        {
            Destroy(activeWindWallCastEffect);
            activeWindWallCastEffect = null;
        }
    }

    private void SpawnWindWallCastEffect()
    {
        animator?.SetBool(windWallAnimBool, true);
        if (windWallCastEffectPrefab == null) return;
        if (activeWindWallCastEffect != null) Destroy(activeWindWallCastEffect);
        activeWindWallCastEffect = Instantiate(windWallCastEffectPrefab, transform.position, Quaternion.identity);
        activeWindWallCastEffect.GetComponent<WindWallCastEffect>()?.Init(transform);
    }

    private void SpawnEndureEffect()
    {
        if (endureEffectPrefab == null) return;
        if (activeEndureEffect != null) Destroy(activeEndureEffect);
        activeEndureEffect = Instantiate(endureEffectPrefab, transform.position, Quaternion.identity);
        activeEndureEffect.GetComponent<EndureEffect>()?.Init(transform);
    }

    protected override void Die()
    {
        ClearWindWalls();
        if (activeEndureEffect != null) { Destroy(activeEndureEffect); activeEndureEffect = null; }
        base.Die();
    }
}
