using System.Collections.Generic;
using UnityEngine;

public class BloodCurseTotem : TotemBase
{
    [Header("Blood Curse Totem")]
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float damagePerTick = 1f;


    private float tickTimer = 0f;
    private readonly List<GameObject> rangeOverlays = new List<GameObject>();

    protected override void Start()
    {
        base.Start();

        CreateRangeOverlay();
    }

    protected override void Update()
    {
        base.Update();

        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            DealAreaDamage();
            tickTimer = tickInterval;
        }
    }

    private void DealAreaDamage()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Vector2Int selfCell = GridAStarPathfinder.WorldToGrid(transform.position);

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;

            Vector2Int playerCell = GridAStarPathfinder.WorldToGrid(player.transform.position);

            int distance =
                Mathf.Abs(selfCell.x - playerCell.x) +
                Mathf.Abs(selfCell.y - playerCell.y);

            if (distance <= effectRange)
            {
                CharacterBase stats = player.GetComponent<CharacterBase>();

                if (stats != null)
                {
                    stats.TakeDamage(damagePerTick);
                    Debug.Log($"{unitName} 對 {stats.unitName} 造成血咒傷害 {damagePerTick}");
                }
            }
        }
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

                int distance = Mathf.Abs(x) + Mathf.Abs(y);

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

    protected override void OnDisable()
    {
        ClearRangeOverlay();

        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        ClearRangeOverlay();

        base.OnDestroy();
    }
}