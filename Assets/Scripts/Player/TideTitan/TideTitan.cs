using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 潮汐泰坦 — 堅守防線，以鋼鐵意志保護隊友
///
/// 技能優先序：
///   1. 潮汐反擊  — 最優先，持續 3 次技能施放週期；受傷時回復 5% HP 並立刻反攻
///              結束後冷卻 1 次技能施放；不阻擋其他技能同時施放
///   2. 深海庇護  — 範圍內敵人 ≥ 範圍內友方 → 隊友全體獲得護盾
///   3. 堅守      — 其餘情況：減傷 50%、攻擊力 -20%、不封鎖普攻、加速技能蓄力
///
/// 充能條件：
///   受傷充能（damage × skillChargeOnDamageRatio）＋時間自然蓄力
///
/// 移動邏輯：
///   1. 優先朝向正在圍攻友軍的敵人（威脅被包圍隊友的敵人）
///   2. 次之找離最近隊友最近的敵人
/// </summary>
public class TideTitan : Player
{
    [Header("技能設定")]
    public float sanctuaryRadius      = 5f;
    public float sanctuaryShieldRatio = 0.20f;
    public float sanctuaryDuration    = 5f;

    [Header("技能充能")]
    public float skillChargeOnDamageRatio = 0.2f; // 受傷充能：charge += damage × ratio

    [Header("目標選定")]
    [Range(0f, 1f)] public float aggressiveChance = 0f; // 0% 進攻 / 100% 防守

    [Header("堅守設定")]
    public float standfastAttackMultiplier     = 0.8f;  // 攻擊力倍率（-20%）
    public float standfastExtraChargePerSecond = 30f;   // 堅守期間每秒額外充能

    [Header("盾牌特效")]
    public GameObject shieldEffectPrefab;

    // 動畫
    private float   baseWalkSpeed;
    private float   baseAttackTime;
    private Vector2 prevPosition;

    // 鎖定目標
    private GameObject lockedTarget = null;

    // 技能狀態
    private bool isStandfast       = false;
    private bool isTidalCounter    = false;
    private bool isSanctuaryActive = false;

    // 潮汐反擊狀態機
    private int tidalCounterUsesLeft = 0; // 剩餘生效次數
    private int tidalCounterCooldown = 0; // 冷卻剩餘次數（以技能施放次數計）

    // 深海庇護
    private Coroutine                 sanctuaryCoroutine;
    private readonly List<GameObject> shieldEffects = new List<GameObject>();

    // 堅守充能累積（分數部分）
    private float standfastChargeAccum = 0f;

    // ─── 初始化 ────────────────────────────────────────────────────

    protected override void Start()
    {
        unitName            = "TideTitan";
        health              = 500f;
        attack              = 10f;
        defense             = 4f;
        attackRange         = 1.5f;
        moveSpeed           = 3f;
        attackTime          = 1.5f;
        skillChargeInterval = 0.1f;

        ApplyEquipment();
        baseWalkSpeed  = moveSpeed;
        baseAttackTime = attackTime;
        base.Start();

        prevPosition = transform.position;
    }

    private void ApplyEquipment()
    {
        defense += 3f;
    }

    // ─── 動畫同步 ──────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (animator != null)
        {
            Vector2 delta = (Vector2)transform.position - prevPosition;
            if (isMoving && delta.sqrMagnitude > 0.0001f)
            {
                int dirVal;
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                    dirVal = delta.x > 0 ? 3 : 2;
                else
                    dirVal = delta.y > 0 ? 1 : 0;
                animator.SetInteger("Direction", dirVal);
            }
        }
        prevPosition = transform.position;

        // 堅守：每幀累積額外充能
        if (isStandfast)
        {
            standfastChargeAccum += standfastExtraChargePerSecond * Time.deltaTime;
            if (standfastChargeAccum >= 1f)
            {
                int give = Mathf.FloorToInt(standfastChargeAccum);
                GainSkillCharge(give);
                standfastChargeAccum -= give;
            }
        }
        else
        {
            standfastChargeAccum = 0f;
        }
    }

    protected override void OnMoveStart()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            if (baseWalkSpeed > 0f)
                animator.speed = moveSpeed / baseWalkSpeed;
        }
    }

    protected override void OnMoveComplete()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.speed = 1f;
        }
    }

    // ─── 傷害系統 ──────────────────────────────────────────────────

    public override void TakeDamage(float damage)
    {
        if (isStandfast) damage *= 0.5f;
        base.TakeDamage(damage);

        // 快照反擊狀態（GainSkillCharge 可能觸發 UseSkill 改變 isTidalCounter）
        bool shouldCounter = isTidalCounter && health > 0f;

        if (health > 0f)
            GainSkillCharge(damage * skillChargeOnDamageRatio);

        if (shouldCounter)
            TidalCounterReact();
    }

    // ─── 普通攻擊 ──────────────────────────────────────────────────

    protected override void Attack(GameObject target)
    {
        // 堅守不封鎖普攻，但攻擊力 -20%
        float effectiveAttack = isStandfast ? attack * standfastAttackMultiplier : attack;

        if (animator != null)
        {
            FaceTarget(target);
            if (baseAttackTime > 0f)
                animator.speed = baseAttackTime / attackTime;
            animator.SetTrigger("Attack");
        }

        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

        enemy.TakeDamage(effectiveAttack);
        StartCoroutine(ResetAnimatorSpeed(attackTime));
    }

    private IEnumerator ResetAnimatorSpeed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null) animator.speed = 1f;
    }

    // ─── 技能決策 ──────────────────────────────────────────────────

    protected override void UseSkill()
    {
        // 技能1：潮汐反擊（最優先，每次 UseSkill 都推進狀態機，不阻擋其他技能）
        AdvanceTidalCounter();

        // 技能2：深海庇護（第二優先）
        if (CountEnemiesInRange() >= CountAlliesInRange())
        {
            DeactivateStandfast();
            ActivateSanctuary();
            return;
        }

        // 技能3：堅守（預設 fallback）
        ExitSanctuary();
        ActivateStandfast();
    }

    // ─── 技能1：潮汐反擊 ─────────────────────────────────────────

    private void AdvanceTidalCounter()
    {
        // 冷卻中：消耗一次冷卻機會
        if (tidalCounterCooldown > 0)
        {
            tidalCounterCooldown--;
            if (tidalCounterCooldown <= 0)
                Debug.Log($"{unitName} 潮汐反擊冷卻結束，可再次發動");
            return;
        }

        // 啟動（若尚未啟動）
        if (!isTidalCounter)
        {
            isTidalCounter       = true;
            tidalCounterUsesLeft = 3;
            Debug.Log($"{unitName} 潮汐反擊發動！持續 3 次技能施放週期");
        }

        // 消耗一次並立即反攻
        tidalCounterUsesLeft--;
        Debug.Log($"{unitName} 潮汐反擊剩餘 {tidalCounterUsesLeft} 次");
        TidalCounterReact();

        if (tidalCounterUsesLeft <= 0)
        {
            isTidalCounter       = false;
            tidalCounterCooldown = 1;
            Debug.Log($"{unitName} 潮汐反擊結束，冷卻 1 次技能施放");
        }
    }

    private void TidalCounterReact()
    {
        Heal(maxHealth * 0.01f);
        GameObject targetObj = (lockedTarget != null && lockedTarget.activeInHierarchy)
            ? lockedTarget : FindNearestEnemyObj();
        if (targetObj == null) return;

        CharacterBase enemy = targetObj.GetComponent<CharacterBase>();
        if (enemy != null)
        {
            if (animator != null)
            {
                FaceTarget(targetObj);
                animator.SetTrigger("Attack");
            }
            enemy.TakeDamage(attack * 0.75f);
            Debug.Log($"{unitName} 潮汐反擊！回復 5% HP 並反攻 {enemy.unitName}");
        }
        attackTimer = attackTime;
    }

    // ─── 技能2：深海庇護 ──────────────────────────────────────────

    private void ActivateSanctuary()
    {
        if (sanctuaryCoroutine != null)
        {
            StopCoroutine(sanctuaryCoroutine);
            sanctuaryCoroutine = null;
        }

        isSanctuaryActive = true;
        shieldEffects.Clear();

        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (!p.activeInHierarchy) continue;
            CharacterBase ally = p.GetComponent<CharacterBase>();
            ally?.AddShield(ally.maxHealth * sanctuaryShieldRatio);
            SpawnShieldEffect(p.transform);
        }
        Debug.Log($"{unitName} 深海庇護！隊友獲得最大生命值 {sanctuaryShieldRatio:P0} 護盾，持續 {sanctuaryDuration} 秒");

        sanctuaryCoroutine = StartCoroutine(SanctuaryTimer());
    }

    private IEnumerator SanctuaryTimer()
    {
        yield return new WaitForSeconds(sanctuaryDuration);
        ExitSanctuary();
    }

    private void ExitSanctuary()
    {
        if (!isSanctuaryActive) return;
        isSanctuaryActive = false;

        if (sanctuaryCoroutine != null)
        {
            StopCoroutine(sanctuaryCoroutine);
            sanctuaryCoroutine = null;
        }

        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (!p.activeInHierarchy) continue;
            CharacterBase ally = p.GetComponent<CharacterBase>();
            ally?.ClearShield();
        }

        foreach (GameObject fx in shieldEffects)
            if (fx != null) Destroy(fx);
        shieldEffects.Clear();

        Debug.Log($"{unitName} 深海庇護解除");
    }

    private void SpawnShieldEffect(Transform allyTransform)
    {
        if (shieldEffectPrefab == null) return;
        GameObject fx = Instantiate(shieldEffectPrefab, allyTransform.position, Quaternion.identity);
        fx.GetComponent<ShieldEffect>()?.Init(allyTransform, sanctuaryDuration);
        shieldEffects.Add(fx);
    }

    // ─── 技能3：堅守 ──────────────────────────────────────────────

    private void ActivateStandfast()
    {
        if (isStandfast) return;
        isStandfast          = true;
        standfastChargeAccum = 0f;
        Debug.Log($"{unitName} 堅守！減傷 50%、攻擊力 -20%、加速充能");
    }

    private void DeactivateStandfast()
    {
        if (!isStandfast) return;
        isStandfast          = false;
        standfastChargeAccum = 0f;
    }

    // ─── 輔助：計數 ────────────────────────────────────────────────

    private int CountEnemiesInRange()
    {
        int count = 0;
        Vector2 pos = transform.position;
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (e == null || !e.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, pos) <= sanctuaryRadius)
                count++;
        }
        return count;
    }

    private int CountAlliesInRange()
    {
        int count = 0;
        Vector2 pos = transform.position;
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == null || !p.activeInHierarchy) continue;
            if (Vector2.Distance(p.transform.position, pos) <= sanctuaryRadius)
                count++;
        }
        return count;
    }

    // ─── 移動目標選取 ──────────────────────────────────────────────

    protected override GameObject FindNearestEnemy()
    {
        if (lockedTarget != null && (!lockedTarget.activeInHierarchy || _skippedTargets.Contains(lockedTarget)))
            lockedTarget = null;

        if (lockedTarget != null)
            return lockedTarget;

        GameObject candidate = Random.value < aggressiveChance ? FindAggressiveTarget() : FindDefensiveTarget();

        if (candidate != null && _skippedTargets.Contains(candidate))
            candidate = base.FindNearestEnemy();

        lockedTarget = candidate;
        return lockedTarget;
    }

    /// <summary>
    /// 進攻型：可擊殺 → 血量最低 → 離自己最遠（大評分差確保嚴格優先序）
    /// </summary>
    private GameObject FindAggressiveTarget()
    {
        GameObject[] enemies  = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   best     = null;
        float        bestScore = float.NegativeInfinity;
        Vector2      myPos    = transform.position;

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;
            CharacterBase cb = e.GetComponent<CharacterBase>();
            if (cb == null) continue;

            float score = 0f;
            if (cb.health <= attack) score += 10000f;          // 可擊殺
            score += 5000f / (cb.health + 1f);                 // 血量越低越高分
            score += Vector2.Distance(e.transform.position, myPos); // 離自己越遠越高分
            if (score > bestScore) { bestScore = score; best = e; }
        }
        return best ?? FindNearestEnemyObj();
    }

    /// <summary>
    /// 防守型：威脅被圍攻友軍的敵人 → 威脅最近隊友的敵人 → 最近敵人
    /// </summary>
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

    /// <summary>找被最多敵人包圍的友軍（至少 2 名敵人才算圍攻）。</summary>
    private GameObject FindMostBesiegedAlly()
    {
        GameObject[] allies  = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject bestAlly = null;
        int        maxCount = 1; // 門檻：超過 1 才算「被圍攻」

        foreach (GameObject ally in allies)
        {
            if (ally == null || !ally.activeInHierarchy) continue;
            Vector2 allyPos = ally.transform.position;
            int count = 0;
            foreach (GameObject e in enemies)
            {
                if (e == null || !e.activeInHierarchy) continue;
                if (Vector2.Distance(e.transform.position, allyPos) <= sanctuaryRadius)
                    count++;
            }
            if (count > maxCount) { maxCount = count; bestAlly = ally; }
        }
        return bestAlly;
    }

    /// <summary>找距離 TideTitan 最近的隊友（排除自身）。</summary>
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

    /// <summary>找距離指定位置最近的存活敵人。</summary>
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

    /// <summary>找距離 TideTitan 自身最近的存活敵人。</summary>
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
