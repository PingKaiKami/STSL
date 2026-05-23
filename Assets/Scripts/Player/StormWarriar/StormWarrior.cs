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
    [Header("Animation")]
    public new Animator animator;

    [Header("狂風戰士屬性")]
    public float dodgeRate = 0f;
    public float critRate  = 0f;
    public float rangedDamageReduction = 0f;
    public float magicDamageMultiplier = 1f;

    [Header("技能設定")]
    public float windWallDuration  = 0.5f;    // 風牆持續秒數
    public float endureDodgeBonus  = 0.25f; // 忍耐時閃避加成
    public float endureCritBonus   = 0.30f; // 忍耐時必殺加成

    [Header("召喚物設定")]
    public GameObject windWallPrefab;

    // 被動
    private bool hasMovedThisTurn = false;

    // 忍耐狀態
    private bool isEnduring = false;

    private bool isWindWallActive = false;

    // ─── 初始化 ───────────────────────────────────────────────

    protected override void Start()
    {
        unitName    = "狂風戰士";
        health      = 1000f;
        attack      = 20f;
        defense     = 2f;
        attackRange = 1.5f;
        moveSpeed   = 4f;
        attackTime  = 1.0f;

        ApplyEquipment();
        base.Start();   // → Player.Start() → CharacterBase.Start()
    }

    private void ApplyEquipment()
    {
        moveSpeed             += 1f;
        dodgeRate             += 0.10f;
        rangedDamageReduction += 0.20f;
        magicDamageMultiplier  = 1.20f;
    }

    // ─── 被動：移動後閃避提升 ─────────────────────────────────

    protected override void OnMoveStart()
    {
        hasMovedThisTurn = false;
        if (animator != null) animator.SetBool("IsWalking", true);
    }

    protected override void OnMoveComplete()
    {
        hasMovedThisTurn = true;
        if (animator != null) animator.SetBool("IsWalking", false);
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
        if (isEnduring) return;

        if (animator != null) animator.SetTrigger("Attack");

        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

        bool isCrit = Random.value < critRate;
        float dmg   = isCrit ? attack * 2f : attack;
        if (isCrit) Debug.Log($"{unitName} 必殺！");

        enemy.TakeDamage(dmg);
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
        var walls = new List<GameObject>();
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
            walls.Add(wall);

            // 與 VoodooFaithful 相同：用 GridAStarPathfinder.WorldToGrid 取格子座標
            Vector2Int wallCell = GridAStarPathfinder.WorldToGrid(wallWorldPos);
            GridReservationManager.TryReserveCell(wall, wallCell);
        }

        Debug.Log($"{unitName} 生成風牆！封鎖三側，{windWallDuration} 秒後消散");
        yield return new WaitForSeconds(windWallDuration);

        foreach (GameObject wall in walls)
        {
            if (wall == null) continue;
            GridReservationManager.ReleaseReservation(wall);
            Destroy(wall);
        }
        isWindWallActive = false;
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
        Debug.Log($"{unitName} 進入忍耐！閃避+{endureDodgeBonus:P0} 必殺+{endureCritBonus:P0}，下回合觸發兩倍滴水穿石");
    }

    private void EndureAndStrike()
    {
        isEnduring  = false;
        dodgeRate  -= endureDodgeBonus;
        critRate   -= endureCritBonus;

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

    /// <summary>穿透防禦的真實傷害攻擊。multiplier = 1（正常）或 2（忍耐爆發）</summary>
    private void DrippingStoneStrike(GameObject target, float multiplier)
    {
        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

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
}
