using UnityEngine;

public class Enemy : CharacterBase
{
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

    protected virtual void CombatLogic()
    {
        // 1. 尋找最近的玩家
        GameObject targetPlayer = FindNearestPlayer();

        if (targetPlayer == null)
        {
            Debug.Log("場上沒有玩家");
            return;
        }

        // 2. 計算與玩家的距離
        float distance = Vector2.Distance(transform.position, targetPlayer.transform.position);

        if (distance > attackRange)
        {
            // 3. 距離太遠：向玩家移動
            if (!isMoving)
            {
                Vector2 diff = (Vector2)targetPlayer.transform.position - (Vector2)transform.position;
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
                Attack(targetPlayer);
                attackTimer = attackTime;
            }
            else
            {
                attackTimer -= Time.deltaTime;
            }
        }
    }

    private GameObject FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        foreach (GameObject player in players)
        {
            float dist = Vector2.Distance(player.transform.position, currentPos);
            if (dist < minDistance)
            {
                nearest = player;
                minDistance = dist;
            }
        }
        return nearest;
    }

    private void Attack(GameObject target)
    {
        Debug.Log($"正在攻擊 {target.name}");
        CharacterBase playerStats = target.GetComponent<CharacterBase>();
        if (playerStats != null)
        {
            playerStats.TakeDamage(attack);
        }
    }
}