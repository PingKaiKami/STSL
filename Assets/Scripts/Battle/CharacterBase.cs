using UnityEngine;
public class CharacterBase : MonoBehaviour
{
    [Header("戰鬥屬性")]
    public float attackSpeed = 1.0f; // 每秒攻擊幾次
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
}