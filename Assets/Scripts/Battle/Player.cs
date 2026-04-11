using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : CharacterBase
{
    [Header("戰鬥設定")]
    public float moveSpeed = 3f;
    public float attackRange = 1.5f;
    
    private Transform currentTarget;
    public float scaleOffset = 1.2f;
    void Update()
    {
        if (GameManager.Instance.currentState == GameState.Combat)
        {
            CombatLogic();
        }
    }
    public virtual void AdjustStats(float h, float a, float d)
    {
        health = Mathf.Max(health + h, 0);
        attack = Mathf.Max(attack + a, 0);
        defense = Mathf.Max(defense + d, 0);
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("遊戲結束，請重新開始");
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            transform.localScale *= scaleOffset;
        }
        else
        {
            transform.localScale /= scaleOffset;
        }
    }

    public void CombatLogic()
    {
        if (currentTarget == null)
        {
            FindTarget();
            return;
        }
        else
        {
            float distance = Vector2.Distance(transform.position, currentTarget.position);

            if (distance > attackRange)
            {
                MoveTowardsTarget();
                attackTimer = 0f;
            }
            else
            {
                HandleAttack();
            }
        }
    }
    public void HandleAttack()
    {
        float attackInterval = 1f / attackSpeed;

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }

    public void PerformAttack()
    {
        if (currentTarget != null)
        {
            CharacterBase targetStats = currentTarget.GetComponent<CharacterBase>();
            
            if (targetStats != null)
            {
                Debug.Log($"{unitName} 攻擊了 {targetStats.unitName}！");
                targetStats.TakeDamage(attack); 
                StartCoroutine(AttackAnimationEffect());
            }
        }
    }

    // 簡單的縮放效果，讓攻擊看起來有動感
    public IEnumerator AttackAnimationEffect()
    {
        transform.localScale = Vector3.one * 1.2f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }

    public void FindTarget()
    {
        GameObject enemyObj = GameObject.FindGameObjectWithTag("Enemy");
        
        if (enemyObj != null)
        {
            currentTarget = enemyObj.transform;
            Debug.Log($"{unitName} 鎖定了目標：{currentTarget.name}");
        }
        else
        {
            Debug.Log("場上沒有敵人了！");
        }
    }

    // 移動的邏輯
    public void MoveTowardsTarget()
    {
        transform.position = Vector2.MoveTowards(
            transform.position, 
            currentTarget.position, 
            moveSpeed * Time.deltaTime
        );
    }
}
