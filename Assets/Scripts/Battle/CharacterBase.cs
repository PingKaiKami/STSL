using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class CharacterBase : MonoBehaviour
{
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum DamageType
    {
        Physical,
        Ranged,
        Magic
    }

    public enum StatusEffect
    {
        Stun,       // 暈眩：無法行動
        Taunt,      // 被嘲諷：強制攻擊嘲諷來源
        KnockBack,  // 被擊退：位移效果
        Burn,       // 燒傷：每秒扣血（火焰傷害）
        Frostbite,  // 凍傷：減速 + 每秒扣血（冰霜傷害）
        Poison,     // 中毒：每秒扣血（毒素傷害）
    }

    [System.Serializable]
    public class StatusEffectInstance
    {
        public StatusEffect type;
        public float duration;          // 剩餘持續時間
        public float tickInterval;      // dot 傷害間隔（秒）
        public float tickDamage;        // 每次 dot 傷害量
        public float tickTimer;         // 累積計時器
        public CharacterBase source;    // 嘲諷來源（Taunt 使用）

        public StatusEffectInstance(StatusEffect type, float duration,
            float tickInterval = 0f, float tickDamage = 0f, CharacterBase source = null)
        {
            this.type = type;
            this.duration = duration;
            this.tickInterval = tickInterval;
            this.tickDamage = tickDamage;
            this.tickTimer = 0f;
            this.source = source;
        }
    }

    protected readonly List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();

    protected bool isMoving = false;

    private RectTransform healthFillRect;
    private RectTransform skillFillRect;
    protected float skillCharge = 0f;
    public float skillChargeInterval = 0.001f; // 每幾秒增加 1 點衝能
    private float skillChargeTimer = 0f;

    [Header("戰鬥屬性")]
    public float moveSpeed = 3f;
    public float attackTime = 1.0f; // 攻擊一次幾秒
    public float attackRange = 1.5f;
    protected float attackTimer = 0f;

    [Header("基礎屬性")]
    public string unitName;
    public float maxHealth;
    public float health;
    public float attack;
    public float defense;
    [HideInInspector] public float teamDamageReduction = 0f; // 深海庇護等外部減傷

    protected virtual void Start()
    {
        maxHealth = health;
        CreateHealthBar();
    }

    protected void Heal(float amount)
    {
        if (amount <= 0f || health <= 0f) return;
        health = Mathf.Min(health + amount, maxHealth);
        UpdateHealthBar();
        Debug.Log($"{unitName} 恢復 {amount:F1} HP，剩餘血量：{health}");
    }

    /// <summary>穿透防禦的真實傷害，不受 defense 影響。</summary>
    public void TakeTrueDamage(float damage)
    {
        float actual = Mathf.Max(damage, 0f);
        health -= actual;
        DamagePopup.Create(transform.position, actual);
        UpdateHealthBar();
        Debug.Log($"{unitName} 受到 {actual} 點真實傷害，剩餘血量：{health}");
        if (health <= 0f) Die();
    }

    public virtual void TakeDamage(float damage)
    {
        if (teamDamageReduction > 0f)
            damage *= (1f - teamDamageReduction);
        float actualDamage = damage <= 0f ? 0f : Mathf.Max(damage - defense, 1f);
        health -= actualDamage;

        DamagePopup.Create(transform.position, actualDamage);
        UpdateHealthBar();

        Debug.Log($"{unitName} 受到了 {actualDamage} 點傷害，剩餘血量：{health}");

        if (health <= 0)
        {
            Die();
        }
    }

    private void CreateHealthBar()
    {
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(80f, 18f);
        canvasRect.localScale = new Vector3(0.012f, 0.012f, 1f);

        // HP bar (top portion)
        healthFillRect = CreateBarRect(canvasObj.transform,
            new Color(0.1f, 0.1f, 0.1f), new Color(0.85f, 0.1f, 0.1f),
            new Vector2(0f, 10f / 18f), new Vector2(1f, 1f));

        // Skill bar (bottom portion, blue), starts empty
        skillFillRect = CreateBarRect(canvasObj.transform,
            new Color(0.1f, 0.1f, 0.1f), new Color(0.15f, 0.45f, 0.95f),
            new Vector2(0f, 0f), new Vector2(1f, 8f / 18f));
        skillFillRect.offsetMax = new Vector2(-80f, 0f);
    }

    private RectTransform CreateBarRect(Transform parent, Color bgColor, Color fillColor,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(parent, false);
        bg.AddComponent<Image>().color = bgColor;
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = anchorMin;
        bgRect.anchorMax = anchorMax;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(parent, false);
        fill.AddComponent<Image>().color = fillColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = anchorMin;
        fillRect.anchorMax = anchorMax;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        return fillRect;
    }

    private void UpdateHealthBar()
    {
        if (healthFillRect == null) return;
        healthFillRect.offsetMax = new Vector2(-(1f - Mathf.Clamp01(health / maxHealth)) * 80f, 0f);
    }

    private void UpdateSkillBar()
    {
        if (skillFillRect == null) return;
        skillFillRect.offsetMax = new Vector2(-(1f - Mathf.Clamp01(skillCharge / 100f)) * 80f, 0f);
    }

    protected void UpdateSkillCharge()
    {
        if (skillCharge >= 100f) return;
        skillChargeTimer += Time.deltaTime;
        if (skillChargeTimer >= skillChargeInterval)
        {
            skillChargeTimer -= skillChargeInterval;
            skillCharge += 1f;
            skillCharge = Mathf.Min(skillCharge, 100f);
            UpdateSkillBar();
            if (skillCharge >= 100f)
            {
                UseSkill();
                skillCharge = 0f;
                UpdateSkillBar();
            }
        }
    }

    protected virtual void UseSkill() { }

    public virtual void TakeDamage(float damage, DamageType type)
    {
        TakeDamage(damage);
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} 已死亡");
        gameObject.SetActive(false); 

        GameManager.Instance.OnCharacterDied(this);
    }

    private Coroutine moveCoroutine;

    protected void Move(MoveDirection dir)
    {
        if (isMoving)
        {
            Debug.LogError("角色還在移動中, 請檢察isMoving參數");
            return;
        }
        Vector2 target = transform.position;

        switch (dir)
        {
            case MoveDirection.Up:
                target += Vector2.up;
                break;
            case MoveDirection.Down:
                target += Vector2.down;
                break;
            case MoveDirection.Left:
                target += Vector2.left;
                break;
            case MoveDirection.Right:
                target += Vector2.right;
                break;
            default:
                Debug.LogError("呼叫 Move 函式錯誤: " + dir);
                return;
        }

        OnMoveStart();
        moveCoroutine = StartCoroutine(MoveRoutine(target));
    }

    protected virtual void OnMoveStart() { }

    protected virtual void OnMoveComplete() { }

    /// <summary>
    /// 若角色不在格子中心（誤差 > 0.05），立刻對齊。
    /// 在 CombatLogic 開頭呼叫，確保所有動作從正確格子出發。
    /// </summary>
    protected void SnapToGrid()
    {
        Vector2Int cell = GridReservationManager.WorldToGrid(transform.position);
        float cx = cell.x + 0.5f;
        float cy = cell.y + 0.5f;
        if (Mathf.Abs(transform.position.x - cx) > 0.0f ||
            Mathf.Abs(transform.position.y - cy) > 0.0f)
        {
            transform.position = new Vector3(cx, cy, transform.position.z);
        }
    }

    protected bool IsCellFree(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
        foreach (Collider2D hit in hits)
            if (hit.gameObject != gameObject) return false;
        return true;
    }

    protected static Vector2 DirToVector(MoveDirection dir)
    {
        switch (dir)
        {
            case MoveDirection.Up:    return Vector2.up;
            case MoveDirection.Down:  return Vector2.down;
            case MoveDirection.Left:  return Vector2.left;
            case MoveDirection.Right: return Vector2.right;
            default:                  return Vector2.zero;
        }
    }

    protected IEnumerator LungeAnim(Vector3 targetPos, System.Action onHit)
    {
        Vector3 origin = transform.position;
        Vector3 dir    = (targetPos - origin).normalized;
        Vector3 peak   = origin + dir * 0.35f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.1f;
            transform.position = Vector3.Lerp(origin, peak, t);
            yield return null;
        }

        onHit?.Invoke();

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.15f;
            transform.position = Vector3.Lerp(peak, origin, t);
            yield return null;
        }

        transform.position = origin;
    }

    private IEnumerator MoveRoutine(Vector2 targetPos)
    {
        isMoving = true;
        while (Vector2.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
        OnMoveComplete();
    }

    // ─── 狀態系統 ────────────────────────────────────────────────

    /// <summary>
    /// 施加狀態效果。若同類型已存在則刷新持續時間（取較長者）。
    /// KnockBack 直接觸發位移，不進入 activeEffects 列表。
    /// </summary>
    public void ApplyStatus(StatusEffect type, float duration,
        float tickInterval = 1f, float tickDamage = 0f,
        CharacterBase source = null, Vector2 knockDir = default, float knockDist = 1f)
    {
        if (type == StatusEffect.KnockBack)
        {
            StartCoroutine(KnockBackRoutine(knockDir.normalized, knockDist));
            return;
        }

        StatusEffectInstance existing = activeEffects.Find(e => e.type == type);
        if (existing != null)
        {
            existing.duration = Mathf.Max(existing.duration, duration);
            if (type == StatusEffect.Taunt && source != null) existing.source = source;
            return;
        }

        activeEffects.Add(new StatusEffectInstance(type, duration, tickInterval, tickDamage, source));
        OnStatusApplied(type);
    }

    /// <summary>
    /// 檢查目前是否有指定狀態。
    /// </summary>
    public bool HasStatus(StatusEffect type)
    {
        return activeEffects.Exists(e => e.type == type);
    }

    /// <summary>
    /// 手動移除指定狀態。
    /// </summary>
    public void RemoveStatus(StatusEffect type)
    {
        int removed = activeEffects.RemoveAll(e => e.type == type);
        if (removed > 0) OnStatusRemoved(type);
    }

    /// <summary>
    /// 取得嘲諷來源（若無嘲諷狀態則回傳 null）。
    /// </summary>
    public CharacterBase GetTauntSource()
    {
        StatusEffectInstance taunt = activeEffects.Find(e => e.type == StatusEffect.Taunt);
        return taunt?.source;
    }

    /// <summary>
    /// 在子類的 Update 中呼叫，處理所有狀態的倒數與 DoT 傷害。
    /// </summary>
    protected void UpdateStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectInstance effect = activeEffects[i];
            effect.duration -= Time.deltaTime;

            if (effect.tickDamage > 0f && effect.tickInterval > 0f)
            {
                effect.tickTimer += Time.deltaTime;
                if (effect.tickTimer >= effect.tickInterval)
                {
                    effect.tickTimer -= effect.tickInterval;
                    TakeDotDamage(effect.tickDamage, effect.type);
                }
            }

            if (effect.duration <= 0f)
            {
                StatusEffect expiredType = effect.type;
                activeEffects.RemoveAt(i);
                OnStatusRemoved(expiredType);
            }
        }
    }

    // 繞過 defense 的 DoT 直接扣血（燒傷/凍傷/中毒）
    private void TakeDotDamage(float damage, StatusEffect source)
    {
        health -= damage;
        DamagePopup.Create(transform.position, damage);
        UpdateHealthBar();
        Debug.Log($"{unitName} 受到 {source} DoT {damage} 點，剩餘血量：{health}");
        if (health <= 0f) Die();
    }

    private IEnumerator KnockBackRoutine(Vector2 dir, float dist)
    {
        // 中斷進行中的普通移動，對齊到最近格子中心（n + 0.5）
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        isMoving = false;
        Vector2Int snapCell = GridReservationManager.WorldToGrid(transform.position);
        transform.position = new Vector3(snapCell.x + 0.5f, snapCell.y + 0.5f, transform.position.z);

        GridReservationManager.ReleaseReservation(gameObject);

        Vector2 start = transform.position;
        Vector2Int startCell = GridReservationManager.WorldToGrid(start);
        Vector2Int endCell = new Vector2Int(
            startCell.x + Mathf.RoundToInt(dir.x * dist),
            startCell.y + Mathf.RoundToInt(dir.y * dist));
        Vector2 snappedEnd = new Vector2(endCell.x + 0.5f, endCell.y + 0.5f);

        // 目標格被佔用（牆、其他角色）時取消擊退
        if (!IsCellFree(snappedEnd))
        {
            OnStatusApplied(StatusEffect.KnockBack);
            yield return null;
            OnStatusRemoved(StatusEffect.KnockBack);
            yield break;
        }

        isMoving = true;
        float t = 0f;
        OnStatusApplied(StatusEffect.KnockBack);
        while (t < 1f)
        {
            t += Time.deltaTime / 0.2f;
            transform.position = Vector2.Lerp(start, snappedEnd, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.position = snappedEnd;
        isMoving = false;
        OnKnockBackComplete(snappedEnd);
        OnStatusRemoved(StatusEffect.KnockBack);
    }

    protected virtual void OnKnockBackComplete(Vector2 newPosition) { }

    /// <summary>狀態生效時的 hook，子類可 override 播動畫或特效。</summary>
    protected virtual void OnStatusApplied(StatusEffect type) { }

    /// <summary>狀態解除時的 hook，子類可 override 播動畫或特效。</summary>
    protected virtual void OnStatusRemoved(StatusEffect type) { }

}