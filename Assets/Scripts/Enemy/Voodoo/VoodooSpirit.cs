using UnityEngine;

public class VoodooSpirit : Enemy
{
    [Header("Summon Settings")]
    [SerializeField] private float lifeTime = 6f;

    [Header("Buff Settings")]
    [SerializeField] private float baseAttackTime;

    [Header("Totem Resonance")]
    [SerializeField] private float attackBonusPerTotem = 1f;

    private float buffTimer = 0f;
    private float lifeTimer;
    private float originalAttack;
    private bool deathNotified = false;

    private void Start()
    {
        lifeTimer = lifeTime;
        baseAttackTime = attackTime;
        originalAttack = attack;

        gameObject.tag = "Enemy";

        ApplyTotemResonance(VoodooTotemRegistry.ActiveTotemCount);
        VoodooTotemRegistry.OnTotemCountChanged += ApplyTotemResonance;

        FaceNearestPlayerOnSpawn();
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

        base.CombatLogic();
    }

    private void FaceNearestPlayerOnSpawn()
    {
        GameObject target = FindNearestPlayerByDistance();

        if (target != null)
        {
            FaceTarget(target);
        }
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

    private void ApplyTotemResonance(int totemCount)
    {
        attack = originalAttack + totemCount * attackBonusPerTotem;

        Debug.Log($"{unitName} 受到圖騰共鳴：目前 ATK = {attack}");
    }

    private void OnDisable()
    {
        VoodooTotemRegistry.OnTotemCountChanged -= ApplyTotemResonance;
    }

    private void OnDestroy()
    {
        VoodooTotemRegistry.OnTotemCountChanged -= ApplyTotemResonance;
    }

    protected override void Die()
    {
        if (!deathNotified)
        {
            deathNotified = true;
            VoodooSummonEvents.NotifySummonDied(this);
        }
        base.Die();
    }
}