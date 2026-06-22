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

    private static int _minX = int.MinValue;
    private static int _maxX = int.MaxValue;
    private static int _minY = int.MinValue;
    private static int _maxY = int.MaxValue;
    private static bool _hasBounds = false;

    public static bool debugDraw = false;

    public static void SetBounds(int minX, int maxX, int minY, int maxY)
    {
        _minX = minX; _maxX = maxX;
        _minY = minY; _maxY = maxY;
        _hasBounds = true;
    }

    public static bool IsWithinBounds(Vector2Int cell)
    {
        EnsureBounds();
        return cell.x >= _minX && cell.x <= _maxX
            && cell.y >= _minY && cell.y <= _maxY;
    }

    private static void EnsureBounds()
    {
        if (_hasBounds) return;
        _hasBounds = true;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        // 玩家區：PlaceableArea 的 GridSlot 子物件
        GameObject area = GameObject.FindGameObjectWithTag("PlaceableArea");
        if (area != null)
        {
            foreach (Transform child in area.transform)
            {
                if (child.GetComponent<GridSlot>() == null) continue;
                Vector2Int cell = WorldToGrid(child.position);
                minX = Mathf.Min(minX, cell.x); maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y); maxY = Mathf.Max(maxY, cell.y);
            }
        }

        // 敵人區：Summoner xMin/xMax/yMin/yMax
        Summoner summoner = Object.FindObjectOfType<Summoner>();
        if (summoner != null)
        {
            Vector2Int lo = WorldToGrid(new Vector3(summoner.xMin, summoner.yMin));
            Vector2Int hi = WorldToGrid(new Vector3(summoner.xMax, summoner.yMax));
            minX = Mathf.Min(minX, lo.x); maxX = Mathf.Max(maxX, hi.x);
            minY = Mathf.Min(minY, lo.y); maxY = Mathf.Max(maxY, hi.y);
        }

        if (minX <= maxX && minY <= maxY)
        {
            _minX = minX; _maxX = maxX; _minY = minY; _maxY = maxY;
            Debug.Log($"[GridAStarPathfinder] 邊界自動偵測：X[{minX}~{maxX}] Y[{minY}~{maxY}]");
        }
        else
        {
            // 找不到場景物件，回退到無限制
            _minX = int.MinValue; _maxX = int.MaxValue;
            _minY = int.MinValue; _maxY = int.MaxValue;
            Debug.LogWarning("[GridAStarPathfinder] 找不到 PlaceableArea/Summoner，邊界未啟用");
        }
    }

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

    public static bool TryFindNextStepToPosition(
        GameObject self,
        Vector2Int target,
        float attackRange,
        int maxSearchDepth,
        out Vector2Int nextStep,
        out int pathCost,
        bool drawDebug = false
    )
    {
        nextStep = Vector2Int.zero;
        pathCost = int.MaxValue;
        if (self == null) return false;

        Vector2Int start = WorldToGrid(self.transform.position);
        HashSet<Vector2Int> occupied = BuildOccupiedSet(self);

        return TryFindPathToTargetZone(start, target, attackRange, maxSearchDepth, occupied, out nextStep, out pathCost, drawDebug);
    }

    private static bool TryFindPathToTargetZone(
        Vector2Int start,
        Vector2Int target,
        float attackRange,
        int maxSearchDepth,
        HashSet<Vector2Int> occupied,
        out Vector2Int nextStep,
        out int pathCost,
        bool drawDebug = false
    )
    {
        nextStep = start;
        pathCost = int.MaxValue;

        int attackRangeCells = Mathf.Max(1, Mathf.FloorToInt(attackRange));

        // 已經在攻擊範圍內，不需要移動（出界時必須先走回來，不能在此提前結束）
        if (IsWithinBounds(start) && Heuristic(start, target) <= attackRangeCells)
        {
            nextStep = start;
            pathCost = 0;
            return true;
        }

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = drawDebug
            ? new Dictionary<Vector2Int, Vector2Int>()
            : null;

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
                if (drawDebug && cameFrom != null)
                    DrawDebugPath(current.position, start, cameFrom, target);
                return true;
            }

            if (current.gCost >= maxSearchDepth)
            {
                continue;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int neighbor = current.position + Directions[i];

                if (closedSet.Contains(neighbor)) continue;

                if (!IsWithinBounds(neighbor)) continue;

                // Player / Enemy / 圖騰 / 小靈都會在這裡被擋住
                if (occupied.Contains(neighbor)) continue;

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

                if (cameFrom != null && !cameFrom.ContainsKey(neighbor))
                    cameFrom[neighbor] = current.position;

                openList.Add(nextNode);
            }
        }

        return false;
    }

    private static void DrawDebugPath(
        Vector2Int goalCell,
        Vector2Int startCell,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int targetCell)
    {
        Debug.Log($"[DebugPath] start={startCell} goal={goalCell} target={targetCell}");

        // 起點：黃色十字
        Vector3 sv = GridToWorld(startCell);
        DrawThickLine(sv + Vector3.left * 0.4f, sv + Vector3.right * 0.4f, Color.yellow, 0.2f);
        DrawThickLine(sv + Vector3.down * 0.4f, sv + Vector3.up   * 0.4f, Color.yellow, 0.2f);

        // 路徑：青色
        Vector2Int cur = goalCell;
        while (cur != startCell && cameFrom.ContainsKey(cur))
        {
            Vector2Int prev = cameFrom[cur];
            DrawThickLine(GridToWorld(prev), GridToWorld(cur), Color.cyan, 0.2f);
            cur = prev;
        }

        // 目標：紅色十字
        Vector3 tv = GridToWorld(targetCell);
        DrawThickLine(tv + Vector3.left * 0.4f, tv + Vector3.right * 0.4f, Color.red, 0.2f);
        DrawThickLine(tv + Vector3.down * 0.4f, tv + Vector3.up   * 0.4f, Color.red, 0.2f);
    }

    private static void DrawThickLine(Vector3 from, Vector3 to, Color color, float duration, float thickness = 0.06f)
    {
        Debug.DrawLine(from, to, color, duration);
        Vector3 ox = new Vector3(thickness, 0, 0);
        Vector3 oy = new Vector3(0, thickness, 0);
        Debug.DrawLine(from + ox, to + ox, color, duration);
        Debug.DrawLine(from - ox, to - ox, color, duration);
        Debug.DrawLine(from + oy, to + oy, color, duration);
        Debug.DrawLine(from - oy, to - oy, color, duration);
    }

    private static Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
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