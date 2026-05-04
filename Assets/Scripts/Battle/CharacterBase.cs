using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
public class CharacterBase : MonoBehaviour
{
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    protected bool isMoving = false;

    [Header("戰鬥屬性")]
    public float moveSpeed = 3f;
    public float attackTime = 1.0f; // 攻擊一次幾秒
    public float attackRange = 1.5f;
    protected float attackTimer = 0f;

    [Header("基礎屬性")]
    public string unitName;
    public float health;
    public float attack;
    public float defense;
    public virtual void TakeDamage(float damage)
    {
        float actualDamage = Mathf.Max(damage - defense, 0);
        health -= actualDamage;

        Debug.Log($"{unitName} 受到了 {actualDamage} 點傷害，剩餘血量：{health}");

        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} 已死亡");
        gameObject.SetActive(false); 

        GameManager.Instance.OnCharacterDied(this);
    }

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

        StartCoroutine(MoveRoutine(target));
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
    }

}