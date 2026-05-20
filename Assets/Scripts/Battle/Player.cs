using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : CharacterBase
{
    public GameObject sourceCardPrefab;
    void Update()
    {
        if (GameManager.Instance.currentState == GameState.Combat)
        {
            CombatLogic();
        }
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("遊戲結束，請重新開始");
    }

    public void CombatLogic()
    {
        // 1. 尋找最近的敵人
        GameObject targetEnemy = FindNearestEnemy();

        if (targetEnemy == null)
        {
            Debug.Log("場上沒有敵人");
            return;
        }

        // 2. 計算與敵人的距離
        float distance = Vector2.Distance(transform.position, targetEnemy.transform.position);

        if (distance > attackRange)
        {
            // 3. 距離太遠：向敵人移動
            if (!isMoving)
            {
                Vector2 diff = (Vector2)targetEnemy.transform.position - (Vector2)transform.position;
                MoveDirection dir;

                // 比較 X 軸與 Y 軸的絕對值，看哪邊距離比較遠
                if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                {
                    // 左右距離較遠，優先走左右
                    dir = (diff.x > 0) ? MoveDirection.Right : MoveDirection.Left;
                }
                else
                {
                    // 上下距離較遠，優先走上下
                    dir = (diff.y > 0) ? MoveDirection.Up : MoveDirection.Down;
                }

                Move(dir);
            }
        }
        else
        {
            // 4. 距離夠近：執行攻擊
            if (attackTimer < 0f)
            {
                Attack(targetEnemy);
                attackTimer = attackTime;
            }
            else
            {
                attackTimer -= Time.deltaTime;
            }
        }
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(enemy.transform.position, currentPos);
            if (dist < minDistance)
            {
                nearest = enemy;
                minDistance = dist;
            }
        }
        return nearest;
    }

    private void Attack(GameObject target)
    {
        Debug.Log($"正在攻擊 {target.name}");
        CharacterBase enemyStats = target.GetComponent<CharacterBase>();
        if (enemyStats != null)
        {
            enemyStats.TakeDamage(attack);
        }
    }
}
