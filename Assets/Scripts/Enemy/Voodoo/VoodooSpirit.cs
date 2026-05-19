using UnityEngine;

public class VoodooSpirit : Enemy
{
    [Header("Summon Settings")]
    [SerializeField] private float lifeTime = 6f;

    [Header("Buff Settings")]
    [SerializeField] private float baseAttackTime;
    private float buffTimer = 0f;

    private float lifeTimer;

    private void Start()
    {
        lifeTimer = lifeTime;
        baseAttackTime = attackTime;

        gameObject.tag = "Enemy";
    }

    protected override void CombatLogic()
    {
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Die();
            return;
        }

        UpdateBuffTimer();

        // 使用 Enemy 原本的追擊與攻擊邏輯
        base.CombatLogic();
    }

    public void ApplyAttackSpeedBuff(float multiplier, float duration)
    {
        attackTime = Mathf.Max(0.1f, baseAttackTime / multiplier);
        buffTimer = duration;
    }

    private void UpdateBuffTimer()
    {
        if (buffTimer <= 0f) return;

        buffTimer -= Time.deltaTime;

        if (buffTimer <= 0f)
        {
            attackTime = baseAttackTime;
        }
    }

    protected override void Die()
    {
        base.Die();
        Destroy(gameObject, 0.1f);
    }
}