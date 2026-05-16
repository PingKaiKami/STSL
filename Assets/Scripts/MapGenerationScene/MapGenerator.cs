using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Normal,
    Elite,
    Shop,
    Rest,
    Event,
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
    public float eventWeight = 22f;
    public float eliteWeightAct1 = 8f;
    public float eliteWeightAfterAct1 = 12f;
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
        int chestLayer = Mathf.Clamp(8, 0, map.Height - 1);
        int restLayerBeforeBoss = map.Height - 1;

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
                else if (layer == 0)
                {
                    node.Type = RoomType.Normal;
                }
                else if (layer == chestLayer)
                {
                    node.Type = RoomType.Chest;
                }
                else if (layer == restLayerBeforeBoss)
                {
                    node.Type = RoomType.Rest;
                }
                else
                {
                    node.Type = PickWeightedRoomType(node, config, rng);
                }
            }
        }
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
        bool parentHasShop = HasParentOfType(node, RoomType.Shop);
        bool parentHasRest = HasParentOfType(node, RoomType.Rest);
        bool parentHasEvent = HasParentOfType(node, RoomType.Event);
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

        float normalWeight = 100f
            - config.shopWeight
            - config.restWeight
            - config.eventWeight
            - eliteWeight;

        normalWeight = Mathf.Max(1f, normalWeight);

        List<RoomWeight> weights = new List<RoomWeight>();

        weights.Add(new RoomWeight(RoomType.Normal, normalWeight));

        if (!parentHasElite)
        {
            weights.Add(new RoomWeight(RoomType.Elite, eliteWeight));
        }

        if (!parentHasEvent)
        {
            weights.Add(new RoomWeight(RoomType.Event, config.eventWeight));
        }

        if (!parentHasShop)
        {
            weights.Add(new RoomWeight(RoomType.Shop, config.shopWeight));
        }

        if (!parentHasRest)
        {
            weights.Add(new RoomWeight(RoomType.Rest, config.restWeight));
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
