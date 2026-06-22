using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 溪流法師 — 水系魔法師，以水力牽制與封鎖控制戰場節奏
///
/// 技能優先序：
///   1. 水影連殺  — 範圍內有可擊殺 / 低血量（< 50% HP）敵人 → 瞬移至最大攻擊距離，射水球；擊殺後鏈接下一目標
///   2. 水牢      — 近身敵人 ≥ 3 → 對最近 2 名敵人各造成 ATK×0.75 傷害 + 暈眩 1 秒
///   3. 水彈      — 其餘情況 → 連發三顆水球
/// </summary>
public class StreamMage : Player
{

    [Header("水球設定")]
    public GameObject waterBallPrefab;
    public float      waterBallSpeed         = 8f;
    public float      waterBallHitRadius     = 0.3f;
    public float      waterBallBurstInterval = 0.25f; // 技能3連發間隔

    [Header("水牢特效")]
    public GameObject waterPrisonEffectPrefab;

    [Header("技能條 — 命中充能")]
    public float skillChargeOnHit      = 15f; // 每次水球命中
    public float skillChargeOnKillBonus = 25f; // 擊殺額外（疊加，total = 15+25=40）

    [Header("目標選定")]
    [Range(0f, 1f)] public float aggressiveChance = 1f; // 100% 進攻
    public float defenseCheckRadius = 5f;

    [Header("Debug — 技能開關")]
    public bool debugEnableWaterPrison      = true;
    public bool debugEnableWaterCurrentPull = true;
    public bool debugEnableWaterBullet      = true;

    // 動畫基準速度
    private float baseWalkSpeed;
    private float baseAttackTime;

    // 走路方向追蹤
    private Vector2 prevPosition;

    // 鎖定目標
    private GameObject lockedTarget = null;

    // ─── 初始化 ────────────────────────────────────────────────────

    protected override void Start()
    {
        unitName            = "StreamMage";
        health              = 200f;
        attack              = 25f;
        defense             = 3f;
        attackRange         = 3f;
        moveSpeed           = 3f;
        attackTime          = 1.2f;
        skillChargeInterval = 99999f; // 停用時間蓄力，改由命中/擊殺驅動

        ApplyEquipment();
        baseWalkSpeed  = moveSpeed;
        baseAttackTime = attackTime;
        base.Start();

        prevPosition = transform.position;
    }

    private void ApplyEquipment()
    {
        attack += 2f;
    }

    // ─── 動畫同步 ──────────────────────────────────────────────────

    // 每幀結束後用位移 delta 更新走路方向
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

    // ─── 普通攻擊 ──────────────────────────────────────────────────

    protected override void Attack(GameObject target)
    {
        if (animator != null)
        {
            FaceTarget(target);
            if (baseAttackTime > 0f)
                animator.speed = baseAttackTime / attackTime;
            animator.SetTrigger("Attack");
        }

        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

        SpawnWaterBall(enemy, attack);
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
        // 技能1：水影連殺 — 範圍內有可擊殺 or 低血量（< 50% HP）敵人
        if (debugEnableWaterCurrentPull)
        {
            GameObject weak = FindWeakOrKillableEnemy(attackRange * 1.5f);
            if (weak != null) { WaterCurrentPull(weak); return; }
        }

        // 技能2：水牢 — 近身敵人 ≥ 3（受到多方圍攻）
        if (debugEnableWaterPrison)
        {
            if (CountNearbyEnemies(attackRange) >= 3)
            {
                var prisonTargets = FindTopNEnemyTargets(2, attackRange);
                if (prisonTargets.Count > 0) { WaterPrison(prisonTargets); return; }
            }
        }

        // 技能3：水彈（預設 fallback）
        if (debugEnableWaterBullet)
        {
            GameObject best = FindBestEnemyTarget(float.MaxValue);
            if (best != null) WaterBullet(best);
        }
    }

    // ─── 技能2：水牢 ──────────────────────────────────────────────

    private void WaterPrison(System.Collections.Generic.List<GameObject> targets)
    {
        if (targets == null || targets.Count == 0) return;

        if (animator != null)
        {
            FaceTarget(targets[0]);
            animator.SetTrigger("Attack");
        }

        foreach (GameObject target in targets)
        {
            CharacterBase enemy = target.GetComponent<CharacterBase>();
            if (enemy == null) continue;

            float hpBefore = enemy.health;
            enemy.TakeDamage(attack * 0.75f);
            GainSkillCharge(skillChargeOnHit);
            if (hpBefore > 0f && enemy.health <= 0f)
                GainSkillCharge(skillChargeOnKillBonus);

            enemy.ApplyStatus(StatusEffect.Stun, 1f);
            SpawnWaterPrisonEffect(target.transform, 1f);

            Debug.Log($"{unitName} 水牢！對 {enemy.unitName} 造成 ATK×0.75 傷害並暈眩 1 秒");
        }
    }

    private void TeleportToRangeEdge(Transform enemyTransform)
    {
        Vector2 dirAway = ((Vector2)transform.position - (Vector2)enemyTransform.position).normalized;
        if (dirAway == Vector2.zero) dirAway = Vector2.right;
        Vector2 edgePos = (Vector2)enemyTransform.position + dirAway * attackRange;
        transform.position = new Vector3(edgePos.x, edgePos.y, transform.position.z);
    }

    // ─── 技能2：水流牽引 ─────────────────────────────────────────

    private void WaterCurrentPull(GameObject firstTarget)
    {
        StartCoroutine(WaterCurrentPullChain(firstTarget));
    }

    private IEnumerator WaterCurrentPullChain(GameObject firstTarget)
    {
        GameObject targetObj = firstTarget;

        while (targetObj != null && targetObj.activeInHierarchy)
        {
            CharacterBase enemy = targetObj.GetComponent<CharacterBase>();
            if (enemy == null) yield break;

            // 瞬移至與目標的最大攻擊距離
            TeleportToRangeEdge(targetObj.transform);

            if (animator != null)
            {
                FaceTarget(targetObj);
                animator.SetTrigger("Attack");
            }

            SpawnWaterBall(enemy, attack);
            Debug.Log($"{unitName} 水流牽引！瞄準 {enemy.unitName}（血量最低），射出水球");

            // 等待水球飛行時間
            float dist        = Vector2.Distance(transform.position, targetObj.transform.position);
            float travelTime  = Mathf.Max(dist / waterBallSpeed, 0.1f);
            yield return new WaitForSeconds(travelTime + 0.1f);

            // 目標仍存活 → 停止鏈接
            if (targetObj != null && targetObj.activeInHierarchy)
                yield break;

            // 目標死亡 → 尋找下一個範圍內最佳目標
            targetObj = FindBestEnemyTarget(attackRange * 1.5f);
        }
    }

    // ─── 技能3：水彈 ─────────────────────────────────────────────

    private void WaterBullet(GameObject target)
    {
        StartCoroutine(WaterBulletBurst(target));
    }

    private IEnumerator WaterBulletBurst(GameObject target)
    {
        for (int i = 0; i < 3; i++)
        {
            // 若目標已死，改找最佳敵人
            if (target == null || !target.activeInHierarchy)
                target = FindBestEnemyTarget(float.MaxValue);

            if (target == null) yield break;

            CharacterBase enemy = target.GetComponent<CharacterBase>();
            if (enemy == null) yield break;

            if (animator != null)
            {
                FaceTarget(target);
                animator.SetTrigger("Attack");
            }

            SpawnWaterBall(enemy, attack);
            Debug.Log($"{unitName} 水彈（{i + 1}/3）！對 {enemy.unitName} 射出水球");

            if (i < 2)
                yield return new WaitForSeconds(waterBallBurstInterval);
        }
    }

    // ─── 水牢特效 ─────────────────────────────────────────────────

    private void SpawnWaterPrisonEffect(Transform enemyTransform, float duration)
    {
        if (waterPrisonEffectPrefab == null) return;
        GameObject fx = Instantiate(waterPrisonEffectPrefab, enemyTransform.position, Quaternion.identity);
        fx.GetComponent<WaterPrisonEffect>()?.Init(enemyTransform, duration);
    }

    // ─── 水球發射 ─────────────────────────────────────────────────

    private void SpawnWaterBall(CharacterBase enemy, float damage)
    {
        if (waterBallPrefab == null)
        {
            Debug.LogWarning($"{unitName}：waterBallPrefab 未指定，改為直接傷害");
            float hpBefore = enemy.health;
            enemy.TakeDamage(damage);
            GainSkillCharge(skillChargeOnHit);
            if (hpBefore > 0f && enemy.health <= 0f)
                GainSkillCharge(skillChargeOnKillBonus);
            return;
        }

        System.Action hitCb  = () => GainSkillCharge(skillChargeOnHit);
        System.Action killCb = () => GainSkillCharge(skillChargeOnKillBonus);

        GameObject ball = Instantiate(waterBallPrefab, transform.position, Quaternion.identity);
        ball.GetComponent<WaterBall>()?.Init(enemy, damage, waterBallSpeed, waterBallHitRadius, hitCb, killCb);
    }

    // ─── 輔助方法 ─────────────────────────────────────────────────

    /// <summary>
    /// 技能1觸發條件：找範圍內「可擊殺（HP ≤ attack）」或「低血量（HP &lt; maxHP×0.5）」的敵人。
    /// 可擊殺優先，其次血量越低越優先。
    /// </summary>
    private GameObject FindWeakOrKillableEnemy(float range)
    {
        GameObject[] enemies  = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   best     = null;
        float        bestScore = float.NegativeInfinity;
        Vector2      myPos    = transform.position;

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, myPos) > range) continue;
            CharacterBase cb = e.GetComponent<CharacterBase>();
            if (cb == null) continue;

            bool isKillable = cb.health <= attack;
            bool isLowHP    = cb.maxHealth > 0f && cb.health < cb.maxHealth * 0.5f;
            if (!isKillable && !isLowHP) continue;

            float score = isKillable ? 10000f : 0f;
            score += 5000f / (cb.health + 1f);
            if (score > bestScore) { bestScore = score; best = e; }
        }
        return best;
    }

    /// <summary>統計範圍內的存活敵人數。</summary>
    private int CountNearbyEnemies(float range)
    {
        int count = 0;
        Vector2 myPos = transform.position;
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (e == null || !e.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, myPos) <= range)
                count++;
        }
        return count;
    }

    /// <summary>依評分取範圍內前 n 名敵人（與 FindBestEnemyTarget 相同評分邏輯）。</summary>
    private System.Collections.Generic.List<GameObject> FindTopNEnemyTargets(int n, float range)
    {
        var scored = new System.Collections.Generic.List<(float score, GameObject obj)>();
        Vector2 myPos = transform.position;
        Vector2 allyCenter = GetAlliesCenterPosition();

        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (e == null || !e.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, myPos) > range) continue;
            CharacterBase cb = e.GetComponent<CharacterBase>();
            if (cb == null) continue;

            float score  = cb.health <= attack ? 10000f : 0f;
            score       += 5000f / (cb.health + 1f);
            score       += Vector2.Distance(e.transform.position, allyCenter);
            scored.Add((score, e));
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));

        var result = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < Mathf.Min(n, scored.Count); i++)
            result.Add(scored[i].obj);
        return result;
    }

    /// <summary>
    /// 以三段優先序選出最佳目標：
    ///   1. 可擊殺（HP ≤ attack）— 加分 10000
    ///   2. 低血量（HP 越低加分越高，最高 5000）
    ///   3. 後排（距友方中心越遠加分，作為同級的決勝分）
    /// </summary>
    private GameObject FindBestEnemyTarget(float range)
    {
        GameObject[] enemies   = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject   best      = null;
        float        bestScore = float.NegativeInfinity;
        Vector2      myPos     = transform.position;
        Vector2      allyCenter = GetAlliesCenterPosition();

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, myPos) > range) continue;
            CharacterBase cb = e.GetComponent<CharacterBase>();
            if (cb == null) continue;

            float score = 0f;

            // 優先1：可擊殺
            if (cb.health <= attack)
                score += 10000f;

            // 優先2：血量越低越高分
            score += 5000f / (cb.health + 1f);

            // 優先3：後排（離友方中心越遠）
            score += Vector2.Distance(e.transform.position, allyCenter);

            if (score > bestScore) { bestScore = score; best = e; }
        }
        return best;
    }

    private Vector2 GetAlliesCenterPosition()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Vector2 sum   = Vector2.zero;
        int     count = 0;
        foreach (GameObject p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;
            sum += (Vector2)p.transform.position;
            count++;
        }
        return count > 0 ? sum / count : (Vector2)transform.position;
    }

    // ─── 移動目標選取 ──────────────────────────────────────────────

    protected override GameObject FindNearestEnemy()
    {
        if (lockedTarget != null && lockedTarget.activeInHierarchy)
            return lockedTarget;

        lockedTarget = Random.value < aggressiveChance ? FindAggressiveTarget() : FindDefensiveTarget();
        return lockedTarget ?? FindNearestEnemyObj();
    }

    private GameObject FindAggressiveTarget()
    {
        return FindBestEnemyTarget(float.MaxValue);
    }

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
        int          maxCount = 1;

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
