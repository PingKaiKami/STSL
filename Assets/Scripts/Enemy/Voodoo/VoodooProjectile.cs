using UnityEngine;

public class VoodooProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float defaultSpeed = 6f;
    [SerializeField] private float defaultHitRadius = 0.2f;

    [Header("Directional Sprites")]
    [SerializeField] private Sprite spriteDown;
    [SerializeField] private Sprite spriteUp;
    [SerializeField] private Sprite spriteLeft;
    [SerializeField] private Sprite spriteRight;

    private Enemy owner;
    private Vector3 origin;
    private Vector3 moveDir;
    private float damage;
    private float speed;
    private float maxDistance;
    private float hitRadius;
    private bool initialized = false;

    private SpriteRenderer spriteRenderer;

    public void Init(
        Enemy owner,
        Vector3 origin,
        CharacterBase.MoveDirection direction,
        float damage,
        float speed,
        float maxDistance,
        float hitRadius
    )
    {
        this.owner = owner;
        this.origin = origin;
        this.damage = damage;
        this.speed = speed > 0f ? speed : defaultSpeed;
        this.maxDistance = maxDistance;
        this.hitRadius = hitRadius > 0f ? hitRadius : defaultHitRadius;
        this.moveDir = DirectionToVector(direction);

        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyDirectionSprite(direction);

        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += moveDir * speed * Time.deltaTime;

        CheckHitPlayer();

        float distanceFromOrigin = Vector2.Distance(transform.position, origin);

        if (distanceFromOrigin >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void CheckHitPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= hitRadius)
            {
                CharacterBase targetStats = player.GetComponent<CharacterBase>();

                if (targetStats != null)
                {
                    if (owner != null)
                    {
                        owner.ResolveProjectileHit(targetStats, damage);
                    }
                    else
                    {
                        targetStats.TakeDamage(damage);
                    }
                }

                Destroy(gameObject);
                return;
            }
        }
    }

    private Vector3 DirectionToVector(CharacterBase.MoveDirection direction)
    {
        switch (direction)
        {
            case CharacterBase.MoveDirection.Up:
                return Vector3.up;

            case CharacterBase.MoveDirection.Down:
                return Vector3.down;

            case CharacterBase.MoveDirection.Left:
                return Vector3.left;

            case CharacterBase.MoveDirection.Right:
                return Vector3.right;
        }

        return Vector3.down;
    }

    private void ApplyDirectionSprite(CharacterBase.MoveDirection direction)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null) return;

        switch (direction)
        {
            case CharacterBase.MoveDirection.Down:
                if (spriteDown != null) spriteRenderer.sprite = spriteDown;
                break;

            case CharacterBase.MoveDirection.Up:
                if (spriteUp != null) spriteRenderer.sprite = spriteUp;
                break;

            case CharacterBase.MoveDirection.Left:
                if (spriteLeft != null) spriteRenderer.sprite = spriteLeft;
                break;

            case CharacterBase.MoveDirection.Right:
                if (spriteRight != null) spriteRenderer.sprite = spriteRight;
                break;
        }
    }
}