using System.Collections.Generic;
using UnityEngine;

public static class GridReservationManager
{
    private static readonly Dictionary<Vector2Int, GameObject> cellOwners =
        new Dictionary<Vector2Int, GameObject>();

    private static readonly Dictionary<GameObject, Vector2Int> ownerCells =
        new Dictionary<GameObject, Vector2Int>();

    public static bool TryReserveCell(GameObject owner, Vector2Int targetCell)
    {
        if (owner == null) return false;

        CleanupDeadReservations();

        Vector2Int currentCell = WorldToGrid(owner.transform.position);

        if (targetCell == currentCell)
        {
            return false;
        }

        if (IsCellOccupiedByOther(owner, targetCell))
        {
            return false;
        }

        if (IsCellReservedByOther(owner, targetCell))
        {
            return false;
        }

        ReleaseReservation(owner);

        cellOwners[targetCell] = owner;
        ownerCells[owner] = targetCell;

        return true;
    }

    /// <summary>
    /// 強制預約格子（忽略 targetCell == currentCell 限制），並踢走原本預約者。
    /// 用於靜態障礙物（風牆、圖騰）放置在自身所在格時。
    /// </summary>
    public static void ForceReserveCell(GameObject owner, Vector2Int cell)
    {
        if (owner == null) return;
        CleanupDeadReservations();

        // 踢走原本預約此格的人，避免他們還以為可以走進來
        if (cellOwners.TryGetValue(cell, out GameObject previousOwner)
            && previousOwner != null && previousOwner != owner)
        {
            ownerCells.Remove(previousOwner);
        }

        ReleaseReservation(owner);
        cellOwners[cell] = owner;
        ownerCells[owner] = cell;
    }

    public static void ReleaseReservation(GameObject owner)
    {
        if (owner == null) return;

        Vector2Int cell;

        if (!ownerCells.TryGetValue(owner, out cell))
        {
            return;
        }

        GameObject currentOwner;

        if (cellOwners.TryGetValue(cell, out currentOwner))
        {
            if (currentOwner == owner || currentOwner == null)
            {
                cellOwners.Remove(cell);
            }
        }

        ownerCells.Remove(owner);
    }

    public static bool HasReservation(GameObject owner)
    {
        if (owner == null) return false;

        CleanupDeadReservations();

        return ownerCells.ContainsKey(owner);
    }

    public static bool CanEnterCell(GameObject owner, Vector2Int targetCell)
    {
        if (owner == null) return false;

        Vector2Int currentCell = WorldToGrid(owner.transform.position);

        if (targetCell == currentCell)
        {
            return false;
        }

        if (IsCellOccupiedByOther(owner, targetCell))
        {
            return false;
        }

        if (IsCellReservedByOther(owner, targetCell))
        {
            return false;
        }

        return true;
    }

    public static bool IsCellOccupiedByOther(GameObject owner, Vector2Int targetCell)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject player in players)
        {
            if (player == null) continue;
            if (!player.activeInHierarchy) continue;
            if (player == owner) continue;

            Vector2Int cell = WorldToGrid(player.transform.position);

            if (cell == targetCell)
            {
                return true;
            }
        }

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.activeInHierarchy) continue;
            if (enemy == owner) continue;

            Vector2Int cell = WorldToGrid(enemy.transform.position);

            if (cell == targetCell)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsCellReservedByOther(GameObject owner, Vector2Int targetCell)
    {
        CleanupDeadReservations();

        GameObject reservedOwner;

        if (!cellOwners.TryGetValue(targetCell, out reservedOwner))
        {
            return false;
        }

        if (reservedOwner == null || !reservedOwner.activeInHierarchy)
        {
            cellOwners.Remove(targetCell);
            return false;
        }

        return reservedOwner != owner;
    }

    public static Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return GridAStarPathfinder.WorldToGrid(worldPosition);
    }

    private static void CleanupDeadReservations()
    {
        List<GameObject> deadOwners = null;

        foreach (KeyValuePair<GameObject, Vector2Int> pair in ownerCells)
        {
            GameObject owner = pair.Key;

            if (owner == null || !owner.activeInHierarchy)
            {
                if (deadOwners == null)
                {
                    deadOwners = new List<GameObject>();
                }

                deadOwners.Add(owner);
            }
        }

        if (deadOwners == null) return;

        for (int i = 0; i < deadOwners.Count; i++)
        {
            ReleaseReservation(deadOwners[i]);
        }
    }
}