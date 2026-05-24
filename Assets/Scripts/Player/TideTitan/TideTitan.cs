using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 潮汐泰坦 — 堅守防線，以鋼鐵意志保護隊友
///
/// 技能優先序：
///   1. 堅守      — 本輪衝能週期掉血 ≥ 30% maxHP → 減傷 50%，封鎖普攻
///   2. 深海庇護  — 範圍內敵人數 ≥ 範圍內友方數 → 隊友全體減傷 10%
///   3. 潮汐反擊  — 其餘情況 → 受傷時回復 5% HP 並立刻反攻
/// </summary>
public class TideTitan : Player
{
    [Header("Animation")]
    public new Animator animator;

    [Header("技能設定")]
    public float sanctuaryRadius    = 5f;    // 深海庇護：判定半徑（格）
    public float sanctuaryReduction = 0.10f; // 深海庇護：隊友減傷比例

    // 動畫基準速度
    private float baseWalkSpeed;
    private float baseAttackTime;

    // 技能狀態
    private bool isStandfast       = false;
    private bool isTidalCounter    = false;
    private bool isSanctuaryActive = false;

    // 堅守用：上一次技能觸發時的血量快照
    private float healthSnapshot;

    // ─── 初始化 ────────────────────────────────────────────────────

    protected override void Start()
    {
        unitName            = "潮汐泰坦";
        health              = 1000f;
        attack              = 15f;
        defense             = 8f;
        attackRange         = 1.5f;
        moveSpeed           = 2f;
        attackTime          = 1.5f;
        skillChargeInterval = 0.1f;

        ApplyEquipment();
        baseWalkSpeed  = moveSpeed;
        baseAttackTime = attackTime;
        base.Start();

        healthSnapshot = health;
    }

    private void ApplyEquipment()
    {
        defense += 3f;
    }

    // ─── 動畫同步 ──────────────────────────────────────────────────

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

        if (isTidalCounter && health > 0f)
            TidalCounterReact();
    }

    // ─── 普通攻擊 ──────────────────────────────────────────────────

    protected override void Attack(GameObject target)
    {
        if (isStandfast) return; // 堅守中封鎖普攻

        if (animator != null)
        {
            if (baseAttackTime > 0f)
                animator.speed = baseAttackTime / attackTime;
            animator.SetTrigger("Attack");
        }

        CharacterBase enemy = target.GetComponent<CharacterBase>();
        if (enemy == null) return;

        enemy.TakeDamage(attack);
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
        ExitCurrentSkills();

        // 技能1：堅守 — 本週期掉血 ≥ 30% maxHP
        if (healthSnapshot - health >= maxHealth * 0.30f)
        {
            ActivateStandfast();
            healthSnapshot = health;
            return;
        }

        // 技能2：深海庇護 — 範圍內敵人 ≥ 範圍內友方
        if (CountEnemiesInRange() >= CountAlliesInRange())
        {
            ActivateSanctuary();
            healthSnapshot = health;
            return;
        }

        // 技能3：潮汐反擊（預設）
        ActivateTidalCounter();
        healthSnapshot = health;
    }

    private void ExitCurrentSkills()
    {
        isStandfast    = false;
        isTidalCounter = false;
        ExitSanctuary();
    }

    // ─── 技能1：堅守 ──────────────────────────────────────────────

    private void ActivateStandfast()
    {
        isStandfast = true;
        Debug.Log($"{unitName} 堅守！減傷 50%，封鎖普攻");
    }

    // ─── 技能2：深海庇護 ──────────────────────────────────────────

    private void ActivateSanctuary()
    {
        isSanctuaryActive = true;
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == gameObject || !p.activeInHierarchy) continue;
            CharacterBase ally = p.GetComponent<CharacterBase>();
            if (ally != null)
                ally.teamDamageReduction = Mathf.Clamp01(ally.teamDamageReduction + sanctuaryReduction);
        }
        Debug.Log($"{unitName} 深海庇護！隊友全體減傷 {sanctuaryReduction:P0}");
    }

    private void ExitSanctuary()
    {
        if (!isSanctuaryActive) return;
        isSanctuaryActive = false;
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (p == gameObject || !p.activeInHierarchy) continue;
            CharacterBase ally = p.GetComponent<CharacterBase>();
            if (ally != null)
                ally.teamDamageReduction = Mathf.Max(ally.teamDamageReduction - sanctuaryReduction, 0f);
        }
        Debug.Log($"{unitName} 深海庇護解除");
    }

    // ─── 技能3：潮汐反擊 ─────────────────────────────────────────

    private void ActivateTidalCounter()
    {
        isTidalCounter = true;
        Debug.Log($"{unitName} 潮汐反擊就緒！受傷時回血 5% 並立刻反攻");
    }

    private void TidalCounterReact()
    {
        Heal(maxHealth * 0.05f);
        GameObject target = FindNearestEnemyObj();
        if (target == null) return;

        Attack(target);
        attackTimer = attackTime;
    }

    // ─── 輔助方法 ─────────────────────────────────────────────────

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
