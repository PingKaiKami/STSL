using System.Collections.Generic;
using UnityEngine;

public static class GridAStarPathfinder
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static bool TryFindNextStepToNearestPlayer(
        GameObject self,
        float attackRange,
        int maxSearchDepth,
        out Vector2Int nextStep,
        out GameObject selectedTarget
    )
    {
        nextStep = Vector2Int.zero;
        selectedTarget = null;

        if (self == null) return false;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players == null || players.Length == 0)
        {
            return false;
        }

        Vector2Int start = WorldToGrid(self.transform.position);
        HashSet<Vector2Int> occupied = BuildOccupiedSet(self);

        int bestCost = int.MaxValue;
        Vector2Int bestNextStep = Vector2Int.zero;
        GameObject bestTarget = null;

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            Vector2Int target = WorldToGrid(player.transform.position);

            Vector2Int candidateNextStep;
            int candidateCost;

            bool found = TryFindPathToTargetZone(
                start,
                target,
                attackRange,
                maxSearchDepth,
                occupied,
                out candidateNextStep,
                out candidateCost
            );

            if (!found) continue;

            if (candidateCost < bestCost)
            {
                bestCost = candidateCost;
                bestNextStep = candidateNextStep;
                bestTarget = player;
            }
        }

        if (bestTarget == null)
        {
            return false;
        }

        nextStep = bestNextStep;
        selectedTarget = bestTarget;
        return true;
    }

    private static bool TryFindPathToTargetZone(
        Vector2Int start,
        Vector2Int target,
        float attackRange,
        int maxSearchDepth,
        HashSet<Vector2Int> occupied,
        out Vector2Int nextStep,
        out int pathCost
    )
    {
        nextStep = start;
        pathCost = int.MaxValue;

        int attackRangeCells = Mathf.Max(1, Mathf.FloorToInt(attackRange));

        // 已經在攻擊範圍內，不需要移動
        if (Heuristic(start, target) <= attackRangeCells)
        {
            nextStep = start;
            pathCost = 0;
            return true;
        }

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(
            start,
            start,
            0,
            Heuristic(start, target),
            false
        );

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            int currentIndex = GetLowestFCostIndex(openList);
            Node current = openList[currentIndex];
            openList.RemoveAt(currentIndex);

            if (closedSet.Contains(current.position))
            {
                continue;
            }

            closedSet.Add(current.position);

            int distanceToTarget = Heuristic(current.position, target);

            // 找到可以攻擊玩家的位置
            if (current.hasFirstStep && distanceToTarget <= attackRangeCells)
            {
                nextStep = current.firstStep;
                pathCost = current.gCost;
                return true;
            }

            if (current.gCost >= maxSearchDepth)
            {
                continue;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int neighbor = current.position + Directions[i];

                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Player / Enemy / 圖騰 / 小靈都會在這裡被擋住
                if (occupied.Contains(neighbor))
                {
                    continue;
                }

                Vector2Int firstStep = current.hasFirstStep
                    ? current.firstStep
                    : neighbor;

                int nextGCost = current.gCost + 1;
                int nextHCost = Heuristic(neighbor, target);

                Node nextNode = new Node(
                    neighbor,
                    firstStep,
                    nextGCost,
                    nextHCost,
                    true
                );

                openList.Add(nextNode);
            }
        }

        return false;
    }

    private static HashSet<Vector2Int> BuildOccupiedSet(GameObject self)
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        AddUnitsWithTag("Enemy", self, occupied);
        AddUnitsWithTag("Player", self, occupied);

        return occupied;
    }

    private static void AddUnitsWithTag(
        string tag,
        GameObject self,
        HashSet<Vector2Int> occupied
    )
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject unit in units)
        {
            if (unit == null) continue;
            if (unit == self) continue;

            occupied.Add(WorldToGrid(unit.transform.position));
        }
    }

    // 重要：Enemy.cs 與 A* 都必須用這個方法
    // 如果你的格子中心是 0.5、1.5、2.5，使用 FloorToInt
    public static Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x + 0.001f),
            Mathf.FloorToInt(worldPosition.y + 0.001f)
        );
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static int GetLowestFCostIndex(List<Node> nodes)
    {
        int bestIndex = 0;
        int bestFCost = nodes[0].FCost;
        int bestHCost = nodes[0].hCost;

        for (int i = 1; i < nodes.Count; i++)
        {
            int currentFCost = nodes[i].FCost;
            int currentHCost = nodes[i].hCost;

            if (currentFCost < bestFCost)
            {
                bestIndex = i;
                bestFCost = currentFCost;
                bestHCost = currentHCost;
            }
            else if (currentFCost == bestFCost && currentHCost < bestHCost)
            {
                bestIndex = i;
                bestFCost = currentFCost;
                bestHCost = currentHCost;
            }
        }

        return bestIndex;
    }

    private struct Node
    {
        public Vector2Int position;
        public Vector2Int firstStep;
        public int gCost;
        public int hCost;
        public bool hasFirstStep;

        public int FCost
        {
            get { return gCost + hCost; }
        }

        public Node(
            Vector2Int position,
            Vector2Int firstStep,
            int gCost,
            int hCost,
            bool hasFirstStep
        )
        {
            this.position = position;
            this.firstStep = firstStep;
            this.gCost = gCost;
            this.hCost = hCost;
            this.hasFirstStep = hasFirstStep;
        }
    }
}