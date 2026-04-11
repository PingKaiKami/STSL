using UnityEngine;

public class Enemy : CharacterBase
{
    [Header("戰鬥設定")]
    public float moveSpeed = 2f;      // 史萊姆走慢一點
    public float attackRange = 1.2f;  // 手短一點
    private Transform currentTarget;

    void Start()
    {
        unitName = "邪惡史萊姆";
        health = 50f;
        attack = 5f;
        defense = 2f;
        attackSpeed = 0.8f; // 攻擊頻率稍微慢一點
    }

    void Update()
    {
        // 只有開戰時才動
        if (GameManager.Instance.currentState == GameState.Combat)
        {
            CombatLogic();
        }
    }

    void CombatLogic()
    {
        if (currentTarget == null)
        {
            // 尋找玩家 (注意這裡是找 Player 標籤)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) currentTarget = playerObj.transform;
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);

        if (distance > attackRange)
        {
            transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);
            attackTimer = 0f;
        }
        else
        {
            // 處理攻擊
            attackTimer += Time.deltaTime;
            if (attackTimer >= (1f / attackSpeed))
            {
                CharacterBase targetStats = currentTarget.GetComponent<CharacterBase>();
                if (targetStats != null)
                {
                    Debug.Log($"{unitName} 咬了 {targetStats.unitName}！");
                    targetStats.TakeDamage(attack);
                }
                attackTimer = 0f;
            }
        }
    }
}