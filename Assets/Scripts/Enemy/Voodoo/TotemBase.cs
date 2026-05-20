using System.Collections.Generic;
using UnityEngine;

public enum TotemRangeMode
{
    Manhattan,   // 上下左右菱形
    Chebyshev   // 周圍八格方形
}

public class TotemBase : CharacterBase
{
    [Header("Totem Settings")]
    [SerializeField] protected float lifeTime = 10f;

    [Header("Totem Owner")]
    protected CharacterBase owner;

    [Header("Totem Range")]
    [SerializeField] protected int effectRange = 1;
    [SerializeField] protected TotemRangeMode rangeMode = TotemRangeMode.Manhattan;

    [Header("Range Overlay")]
    [SerializeField] protected GameObject rangeOverlayPrefab;
    [SerializeField] protected bool showRangeOverlay = true;
    [SerializeField] protected bool includeCenterCell = false;

    [Header("Target Effect")]
    [SerializeField] protected GameObject targetEffectPrefab;
    [SerializeField] protected Vector3 targetEffectOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] protected bool attachTargetEffectToTarget = true;

    [Header("Self Effect")]
    [SerializeField] protected GameObject selfEffectPrefab;
    [SerializeField] protected Vector3 selfEffectOffset = Vector3.zero;
    [SerializeField] protected bool attachSelfEffectToSelf = true;

    private float lifeTimer;
    private bool isDead = false;

    private readonly List<GameObject> rangeOverlays = new List<GameObject>();

    public virtual void InitOwner(CharacterBase ownerCharacter)
    {
        owner = ownerCharacter;
    }

    protected virtual void Start()
    {
        lifeTimer = lifeTime;
        gameObject.tag = "Enemy";

        VoodooTotemRegistry.Register(this);

        CreateRangeOverlay();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Die();
        }
    }

    protected override void Die()
    {
        if (isDead) return;

        isDead = true;

        ClearRangeOverlay();

        VoodooTotemRegistry.Unregister(this);

        base.Die();
    }

    protected virtual void OnDisable()
    {
        ClearRangeOverlay();

        VoodooTotemRegistry.Unregister(this);
    }

    protected virtual void OnDestroy()
    {
        ClearRangeOverlay();

        VoodooTotemRegistry.Unregister(this);
    }

    protected bool IsCellInRange(Vector2Int targetCell)
    {
        Vector2Int selfCell = GridAStarPathfinder.WorldToGrid(transform.position);

        int dx = Mathf.Abs(selfCell.x - targetCell.x);
        int dy = Mathf.Abs(selfCell.y - targetCell.y);

        int distance;

        if (rangeMode == TotemRangeMode.Manhattan)
        {
            distance = dx + dy;
        }
        else
        {
            distance = Mathf.Max(dx, dy);
        }

        return distance <= effectRange;
    }

    protected bool IsWorldPositionInRange(Vector3 worldPosition)
    {
        Vector2Int targetCell = GridAStarPathfinder.WorldToGrid(worldPosition);
        return IsCellInRange(targetCell);
    }

    protected List<GameObject> GetPlayersInRange()
    {
        List<GameObject> result = new List<GameObject>();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;

            if (IsWorldPositionInRange(player.transform.position))
            {
                result.Add(player);
            }
        }

        return result;
    }

    protected List<VoodooSpirit> GetSummonsInRange()
    {
        List<VoodooSpirit> result = new List<VoodooSpirit>();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.activeInHierarchy) continue;

            VoodooSpirit spirit = enemy.GetComponent<VoodooSpirit>();

            if (spirit == null) continue;

            if (IsWorldPositionInRange(spirit.transform.position))
            {
                result.Add(spirit);
            }
        }

        return result;
    }

    protected CharacterBase GetOwner()
    {
        return owner;
    }

    protected void PlayTargetEffect(CharacterBase target)
    {
        if (target == null) return;
        if (targetEffectPrefab == null) return;
        if (!target.gameObject.activeInHierarchy) return;

        Vector3 spawnPos = target.transform.position + targetEffectOffset;

        GameObject effect = Instantiate(
            targetEffectPrefab,
            spawnPos,
            Quaternion.identity
        );

        if (attachTargetEffectToTarget)
        {
            effect.transform.SetParent(target.transform);
            effect.transform.localPosition = targetEffectOffset;
        }
    }

    protected void PlaySelfEffect()
    {
        if (selfEffectPrefab == null) return;

        Vector3 spawnPos = transform.position + selfEffectOffset;

        GameObject effect = Instantiate(
            selfEffectPrefab,
            spawnPos,
            Quaternion.identity
        );

        if (attachSelfEffectToSelf)
        {
            effect.transform.SetParent(transform);
            effect.transform.localPosition = selfEffectOffset;
        }
    }

    protected void PlayEffectAt(GameObject effectPrefab, Vector3 worldPosition)
    {
        if (effectPrefab == null) return;

        Instantiate(
            effectPrefab,
            worldPosition,
            Quaternion.identity
        );
    }

    private void CreateRangeOverlay()
    {
        if (!showRangeOverlay) return;
        if (rangeOverlayPrefab == null) return;

        ClearRangeOverlay();

        for (int x = -effectRange; x <= effectRange; x++)
        {
            for (int y = -effectRange; y <= effectRange; y++)
            {
                if (!includeCenterCell && x == 0 && y == 0)
                {
                    continue;
                }

                int distance;

                if (rangeMode == TotemRangeMode.Manhattan)
                {
                    distance = Mathf.Abs(x) + Mathf.Abs(y);
                }
                else
                {
                    distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                }

                if (distance > effectRange)
                {
                    continue;
                }

                Vector3 overlayPos = transform.position + new Vector3(x, y, 0f);

                GameObject overlay = Instantiate(
                    rangeOverlayPrefab,
                    overlayPos,
                    Quaternion.identity
                );

                rangeOverlays.Add(overlay);
            }
        }
    }

    private void ClearRangeOverlay()
    {
        for (int i = rangeOverlays.Count - 1; i >= 0; i--)
        {
            if (rangeOverlays[i] != null)
            {
                Destroy(rangeOverlays[i]);
            }
        }

        rangeOverlays.Clear();
    }
}