using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Normal,
    Elite,
    Shop,
    Rest,
    Chest,
    Boss
}

[Serializable]
public class MapNode
{
    public int Layer;
    public int Column;
    public RoomType Type;

    public List<MapNode> Parents = new List<MapNode>();
    public List<MapNode> Children = new List<MapNode>();

    public MapNode(int layer, int column)
    {
        Layer = layer;
        Column = column;
        Type = RoomType.Normal;
    }

    public string Id
    {
        get { return Layer + "_" + Column; }
    }
}

[Serializable]
public class MapEdge
{
    public MapNode From;
    public MapNode To;

    public MapEdge(MapNode from, MapNode to)
    {
        From = from;
        To = to;
    }
}

public class MapData
{
    public int Width;
    public int Height;

    public List<MapNode> Nodes = new List<MapNode>();
    public List<MapEdge> Edges = new List<MapEdge>();

    private Dictionary<string, MapNode> nodeLookup = new Dictionary<string, MapNode>();

    public MapData(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public MapNode GetOrCreateNode(int layer, int column)
    {
        string id = layer + "_" + column;

        if (nodeLookup.ContainsKey(id))
        {
            return nodeLookup[id];
        }

        MapNode node = new MapNode(layer, column);
        nodeLookup.Add(id, node);
        Nodes.Add(node);
        return node;
    }

    public void AddEdge(MapNode from, MapNode to)
    {
        for (int i = 0; i < Edges.Count; i++)
        {
            if (Edges[i].From == from && Edges[i].To == to)
            {
                return;
            }
        }

        MapEdge edge = new MapEdge(from, to);
        Edges.Add(edge);

        if (!from.Children.Contains(to))
        {
            from.Children.Add(to);
        }

        if (!to.Parents.Contains(from))
        {
            to.Parents.Add(from);
        }
    }
}

[Serializable]
public class MapGenerationConfig
{
    public int width = 7;
    public int height = 15;
    public int pathCount = 6;
    public int startNodeCount = 4;

    public bool useRandomSeed = true;
    public int seed = 0;

    public int actIndex = 1;

    public float shopWeight = 5f;
    public float restWeight = 12f;
    public float redistributedQuestionMarkWeight = 22f;
    public float eliteWeightAct1 = 8f;
    public float eliteWeightAfterAct1 = 12f;

    public int minimumShopCount = 2;
    public int minimumRestCount = 2;
    public int minimumChestCount = 2;
}

public static class SlayLikeMapGenerator
{
    private struct RoomWeight
    {
        public RoomType Type;
        public float Weight;

        public RoomWeight(RoomType type, float weight)
        {
            Type = type;
            Weight = weight;
        }
    }

    public static MapData Generate(MapGenerationConfig config)
    {
        int width = Mathf.Max(3, config.width);
        int height = Mathf.Max(3, config.height);
        int startNodeCount = Mathf.Clamp(config.startNodeCount, 1, width);
        int pathCount = Mathf.Max(startNodeCount, config.pathCount);

        System.Random rng;

        if (config.useRandomSeed)
        {
            rng = new System.Random(Guid.NewGuid().GetHashCode());
        }
        else
        {
            rng = new System.Random(config.seed);
        }

        MapData map = new MapData(width, height);
        List<int> startColumns = PickStartColumns(width, startNodeCount, rng);

        // Build Slay-the-Spire-like paths, but keep the bottom row limited to the chosen starts.
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            int currentColumn;

            if (pathIndex < startColumns.Count)
            {
                currentColumn = startColumns[pathIndex];
            }
            else
            {
                currentColumn = startColumns[rng.Next(0, startColumns.Count)];
            }

            MapNode currentNode = map.GetOrCreateNode(0, currentColumn);

            for (int layer = 0; layer < height - 1; layer++)
            {
                int nextColumn = PickNextColumnWithoutCrossing(
                    map,
                    rng,
                    layer,
                    currentColumn,
                    width
                );

                MapNode nextNode = map.GetOrCreateNode(layer + 1, nextColumn);
                map.AddEdge(currentNode, nextNode);

                currentColumn = nextColumn;
                currentNode = nextNode;
            }
        }

        int bossColumn = width / 2;
        MapNode bossNode = map.GetOrCreateNode(height, bossColumn);
        bossNode.Type = RoomType.Boss;

        List<MapNode> topLayerNodes = new List<MapNode>();

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            if (map.Nodes[i].Layer == height - 1)
            {
                topLayerNodes.Add(map.Nodes[i]);
            }
        }

        for (int i = 0; i < topLayerNodes.Count; i++)
        {
            map.AddEdge(topLayerNodes[i], bossNode);
        }

        AssignRoomTypes(map, config, rng);
        RemoveUnconnectedNodes(map);

        return map;
    }

    private static List<int> PickStartColumns(int width, int startNodeCount, System.Random rng)
    {
        List<int> columns = new List<int>();

        for (int column = 0; column < width; column++)
        {
            columns.Add(column);
        }

        Shuffle(columns, rng);

        List<int> starts = new List<int>();

        for (int i = 0; i < startNodeCount; i++)
        {
            starts.Add(columns[i]);
        }

        starts.Sort();
        return starts;
    }

    private static int PickNextColumnWithoutCrossing(
        MapData map,
        System.Random rng,
        int fromLayer,
        int fromColumn,
        int width
    )
    {
        List<int> candidates = new List<int>();

        for (int offset = -1; offset <= 1; offset++)
        {
            int candidate = fromColumn + offset;

            if (candidate >= 0 && candidate < width)
            {
                candidates.Add(candidate);
            }
        }

        Shuffle(candidates, rng);

        List<int> validCandidates = new List<int>();

        for (int i = 0; i < candidates.Count; i++)
        {
            int toColumn = candidates[i];

            if (!WouldCrossExistingEdge(map, fromLayer, fromColumn, toColumn))
            {
                validCandidates.Add(toColumn);
            }
        }

        if (validCandidates.Count == 0)
        {
            return fromColumn;
        }

        return validCandidates[rng.Next(0, validCandidates.Count)];
    }

    private static bool WouldCrossExistingEdge(
        MapData map,
        int fromLayer,
        int newFromColumn,
        int newToColumn
    )
    {
        for (int i = 0; i < map.Edges.Count; i++)
        {
            MapEdge edge = map.Edges[i];

            if (edge.From.Layer != fromLayer)
            {
                continue;
            }

            if (edge.To.Layer != fromLayer + 1)
            {
                continue;
            }

            int oldFromColumn = edge.From.Column;
            int oldToColumn = edge.To.Column;

            if (oldFromColumn == newFromColumn)
            {
                continue;
            }

            if (oldToColumn == newToColumn)
            {
                continue;
            }

            bool crossA = oldFromColumn < newFromColumn && oldToColumn > newToColumn;
            bool crossB = oldFromColumn > newFromColumn && oldToColumn < newToColumn;

            if (crossA || crossB)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssignRoomTypes(
        MapData map,
        MapGenerationConfig config,
        System.Random rng
    )
    {
        for (int layer = 0; layer <= map.Height; layer++)
        {
            List<MapNode> layerNodes = GetNodesInLayer(map, layer);

            for (int i = 0; i < layerNodes.Count; i++)
            {
                MapNode node = layerNodes[i];

                if (layer == map.Height)
                {
                    node.Type = RoomType.Boss;
                }
                else if (layer == map.Height - 1)
                {
                    node.Type = RoomType.Rest;
                }
                else if (layer == 0)
                {
                    node.Type = RoomType.Normal;
                }
                else
                {
                    node.Type = RoomType.Normal;
                }
            }
        }

        EnsureMinimumRoomCount(map, RoomType.Shop, config.minimumShopCount, rng);
        EnsureMinimumRoomCount(map, RoomType.Rest, config.minimumRestCount, rng);
        EnsureMinimumRoomCount(map, RoomType.Chest, config.minimumChestCount, rng);

        for (int layer = 1; layer < map.Height; layer++)
        {
            List<MapNode> layerNodes = GetNodesInLayer(map, layer);

            for (int i = 0; i < layerNodes.Count; i++)
            {
                MapNode node = layerNodes[i];

                if (node.Type != RoomType.Normal)
                {
                    continue;
                }

                node.Type = PickWeightedRoomType(node, config, rng);
            }
        }
    }

    private static void EnsureMinimumRoomCount(
        MapData map,
        RoomType roomType,
        int minimumCount,
        System.Random rng
    )
    {
        int targetCount = Mathf.Max(0, minimumCount);

        while (CountRoomsOfType(map, roomType) < targetCount)
        {
            List<MapNode> candidates = GetSpecialRoomCandidates(map);

            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    "Could not place enough "
                    + roomType
                    + " rooms without making special rooms consecutive."
                );
                return;
            }

            MapNode selectedNode = candidates[rng.Next(0, candidates.Count)];
            selectedNode.Type = roomType;
        }
    }

    private static int CountRoomsOfType(MapData map, RoomType roomType)
    {
        int count = 0;

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            if (map.Nodes[i].Type == roomType)
            {
                count++;
            }
        }

        return count;
    }

    private static List<MapNode> GetSpecialRoomCandidates(MapData map)
    {
        List<MapNode> candidates = new List<MapNode>();

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            MapNode node = map.Nodes[i];

            if (node.Layer == 0 || node.Layer == map.Height)
            {
                continue;
            }

            if (node.Type != RoomType.Normal)
            {
                continue;
            }

            if (HasAdjacentPriorityRoom(node))
            {
                continue;
            }

            candidates.Add(node);
        }

        return candidates;
    }

    private static List<MapNode> GetNodesInLayer(MapData map, int layer)
    {
        List<MapNode> result = new List<MapNode>();

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            if (map.Nodes[i].Layer == layer)
            {
                result.Add(map.Nodes[i]);
            }
        }

        return result;
    }

    private static RoomType PickWeightedRoomType(
        MapNode node,
        MapGenerationConfig config,
        System.Random rng
    )
    {
        bool canPlacePriorityRoom = !HasParentPriorityRoom(node) && !HasChildPriorityRoom(node);
        bool parentHasShop = HasParentOfType(node, RoomType.Shop);
        bool parentHasRest = HasParentOfType(node, RoomType.Rest);
        bool parentHasChest = HasParentOfType(node, RoomType.Chest);
        bool parentHasElite = HasParentOfType(node, RoomType.Elite);

        float eliteWeight;

        if (config.actIndex <= 1)
        {
            eliteWeight = config.eliteWeightAct1;
        }
        else
        {
            eliteWeight = config.eliteWeightAfterAct1;
        }

        float questionMarkWeight = Mathf.Max(0f, config.redistributedQuestionMarkWeight);
        float redistributedWeight = questionMarkWeight / 4f;
        float normalWeight = 100f
            - config.shopWeight
            - config.restWeight
            - questionMarkWeight
            - eliteWeight;

        normalWeight = Mathf.Max(1f, normalWeight) + redistributedWeight;
        float shopWeight = config.shopWeight + redistributedWeight;
        float restWeight = config.restWeight + redistributedWeight;
        float chestWeight = redistributedWeight;

        List<RoomWeight> weights = new List<RoomWeight>();

        weights.Add(new RoomWeight(RoomType.Normal, normalWeight));

        if (!parentHasElite)
        {
            weights.Add(new RoomWeight(RoomType.Elite, eliteWeight));
        }

        if (canPlacePriorityRoom && !parentHasShop)
        {
            weights.Add(new RoomWeight(RoomType.Shop, shopWeight));
        }

        if (canPlacePriorityRoom && !parentHasRest)
        {
            weights.Add(new RoomWeight(RoomType.Rest, restWeight));
        }

        if (canPlacePriorityRoom && !parentHasChest && chestWeight > 0f)
        {
            weights.Add(new RoomWeight(RoomType.Chest, chestWeight));
        }

        float totalWeight = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i].Weight;
        }

        float roll = (float)(rng.NextDouble() * totalWeight);

        float cumulative = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i].Weight;

            if (roll <= cumulative)
            {
                return weights[i].Type;
            }
        }

        return RoomType.Normal;
    }

    private static bool HasAdjacentPriorityRoom(MapNode node)
    {
        return HasParentPriorityRoom(node) || HasChildPriorityRoom(node);
    }

    private static bool HasParentPriorityRoom(MapNode node)
    {
        for (int i = 0; i < node.Parents.Count; i++)
        {
            if (IsPriorityRoom(node.Parents[i].Type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasChildPriorityRoom(MapNode node)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            if (IsPriorityRoom(node.Children[i].Type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPriorityRoom(RoomType roomType)
    {
        return roomType == RoomType.Shop || roomType == RoomType.Rest || roomType == RoomType.Chest;
    }

    private static bool HasParentOfType(MapNode node, RoomType type)
    {
        for (int i = 0; i < node.Parents.Count; i++)
        {
            if (node.Parents[i].Type == type)
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveUnconnectedNodes(MapData map)
    {
        for (int i = map.Nodes.Count - 1; i >= 0; i--)
        {
            MapNode node = map.Nodes[i];
            bool isStart = node.Layer == 0;
            bool isBoss = node.Layer == map.Height;
            bool hasConnection = node.Parents.Count > 0 || node.Children.Count > 0;

            if (!isStart && !isBoss && !hasConnection)
            {
                map.Nodes.RemoveAt(i);
            }
        }
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = rng.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
