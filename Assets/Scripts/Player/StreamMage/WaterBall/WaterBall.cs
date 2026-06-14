using UnityEngine;

public class WaterBall : MonoBehaviour
{
    [SerializeField] private float defaultSpeed       = 8f;
    [SerializeField] private float defaultHitRadius   = 0.3f;
    [SerializeField] private float defaultMaxDistance = 20f;

    private CharacterBase  target;
    private Vector3        targetPos;
    private float          damage;
    private float          speed;
    private float          hitRadius;
    private float          maxDistance;
    private Vector3        origin;
    private bool           initialized;

    private System.Action onHit;
    private System.Action onKill;

    /// <param name="onHit">每次命中時呼叫（無論是否擊殺）</param>
    /// <param name="onKill">命中且造成擊殺時額外呼叫</param>
    public void Init(CharacterBase target, float damage,
                     float speed = 0f, float hitRadius = 0f,
                     System.Action onHit = null, System.Action onKill = null)
    {
        this.target      = target;
        this.targetPos   = target.transform.position;
        this.damage      = damage;
        this.speed       = speed > 0f ? speed : defaultSpeed;
        this.hitRadius   = hitRadius > 0f ? hitRadius : defaultHitRadius;
        this.maxDistance = defaultMaxDistance;
        this.origin      = transform.position;
        this.initialized = true;
        this.onHit       = onHit;
        this.onKill      = onKill;

        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.up = dir;
    }

    private void Update()
    {
        if (!initialized) return;

        Vector3 dir = (targetPos - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector2.Distance(transform.position, targetPos) <= hitRadius)
        {
            OnHit();
            return;
        }

        if (Vector2.Distance(transform.position, origin) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnHit()
    {
        bool killed = false;
        if (target != null && target.gameObject.activeInHierarchy)
        {
            float hpBefore = target.health;
            target.TakeDamage(damage);
            killed = hpBefore > 0f && target.health <= 0f;
        }

        onHit?.Invoke();
        if (killed) onKill?.Invoke();

        Destroy(gameObject);
    }
}
