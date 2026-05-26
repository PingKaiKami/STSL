using System.Collections.Generic;
using UnityEngine;

public static class GridReservationManager
{
    private static readonly Dictionary<Vector2Int, GameObject> cellOwners =
        new Dictionary<Vector2Int, GameObject>();

    private static readonly Dictionary<GameObject, Vector2Int> ownerCells =
        new Dictionary<GameObject, Vector2Int>();

    public static void ForceReserveCell(GameObject owner, Vector2Int targetCell)
    {
        if (owner == null) return;
        ReleaseReservation(owner);
        cellOwners[targetCell] = owner;
        ownerCells[owner] = targetCell;
    }

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