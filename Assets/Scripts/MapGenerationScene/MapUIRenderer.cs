using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Attach this to a UI GameObject in MapScene, such as a Panel under Canvas.
// Assign mapRoot to the RectTransform where map nodes and lines should be spawned.
public class MapUIRenderer : MonoBehaviour
{
    [Header("Map Generation Settings")]
    public int width = 7;
    public int height = 15;
    public int pathCount = 6;
    public int startNodeCount = 4;

    public bool useRandomSeed = true;
    public int seed = 0;
    public int actIndex = 1;

    [Header("Room Weights")]
    public float shopWeight = 5f;
    public float restWeight = 12f;
    public float redistributedQuestionMarkWeight = 22f;
    public float eliteWeightAct1 = 8f;
    public float eliteWeightAfterAct1 = 12f;

    [Header("Required Room Counts")]
    public int minimumShopCount = 2;
    public int minimumRestCount = 2;
    public int minimumChestCount = 2;

    [Header("UI References")]
    public RectTransform mapRoot;
    public RectTransform viewport;
    public Button generateButton;
    public Button nodeButtonPrefab;
    public Text statusText;
    public Text playerInfoText;

    [Header("UI Layout")]
    public Vector2 cellSpacing = new Vector2(120f, 58f);
    public Vector2 nodeSize = new Vector2(82f, 44f);
    public float lineThickness = 5f;
    public int fontSize = 14;
    public Font uiFont;

    [Header("Generate Behavior")]
    public bool generateOnStart = true;

    [Header("Map Scroll")]
    public bool enableMouseWheelScroll = true;
    public float mouseWheelScrollSpeed = 180f;
    public float scrollPadding = 70f;
    public float currentLayerViewportY = -190f;

    [Header("Scene Loading")]
    public string battleSceneName = "BattleScene";
    public string shopSceneName = "ShopScene";
    public string restSceneName = "RestScene";
    public string chestSceneName = "ChestScene";

    [Header("Progress Colors")]
    public Color completedColor = new Color(0.45f, 0.85f, 0.45f, 1f);
    public Color lockedColor = new Color(0.28f, 0.28f, 0.28f, 0.65f);

    [Header("Room Colors")]
    public Color normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color eliteColor = new Color(0.95f, 0.55f, 0.55f, 1f);
    public Color shopColor = new Color(0.95f, 0.82f, 0.45f, 1f);
    public Color restColor = new Color(0.55f, 0.85f, 0.95f, 1f);
    public Color chestColor = new Color(0.85f, 0.65f, 0.35f, 1f);
    public Color bossColor = new Color(0.9f, 0.25f, 0.25f, 1f);
    public Color lineColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color textColor = Color.black;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private MapData currentMap;
    private Vector2 mapRootBasePosition;
    private float scrollOffsetY;
    private float minScrollOffsetY;
    private float maxScrollOffsetY;

    private void Awake()
    {
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(GenerateAndRenderMap);
        }
    }

    private void Start()
    {
        if (mapRoot == null)
        {
            mapRoot = transform as RectTransform;
        }

        if (viewport == null && mapRoot != null)
        {
            viewport = mapRoot.parent as RectTransform;
        }

        if (mapRoot != null)
        {
            mapRootBasePosition = mapRoot.anchoredPosition;
        }

        if (generateOnStart)
        {
            RenderExistingRunOrCreateNewRun();
        }
    }

    private void Update()
    {
        RefreshPlayerInfo();

        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateAndRenderMap();
        }

        HandleMapScroll();
    }

    public void GenerateAndRenderMap()
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.StartNewRun(CreateConfig());
        RenderCurrentMap("New run generated.");
    }

    public void RenderExistingRunOrCreateNewRun()
    {
        RunStateManager runState = RunStateManager.EnsureExists();

        if (!runState.HasActiveRun())
        {
            runState.StartNewRun(CreateConfig());
        }

        RenderCurrentMap("Map ready.");
    }

    private MapGenerationConfig CreateConfig()
    {
        MapGenerationConfig config = new MapGenerationConfig();
        config.width = width;
        config.height = height;
        config.pathCount = pathCount;
        config.startNodeCount = startNodeCount;
        config.useRandomSeed = useRandomSeed;
        config.seed = seed;
        config.actIndex = actIndex;
        config.shopWeight = shopWeight;
        config.restWeight = restWeight;
        config.redistributedQuestionMarkWeight = redistributedQuestionMarkWeight;
        config.eliteWeightAct1 = eliteWeightAct1;
        config.eliteWeightAfterAct1 = eliteWeightAfterAct1;
        config.minimumShopCount = minimumShopCount;
        config.minimumRestCount = minimumRestCount;
        config.minimumChestCount = minimumChestCount;
        return config;
    }

    private void RenderCurrentMap(string statusMessage)
    {
        if (mapRoot == null)
        {
            Debug.LogError("Map Root is missing. Please assign a RectTransform to mapRoot.");
            return;
        }

        RunStateManager runState = RunStateManager.EnsureExists();

        if (runState.currentMap == null)
        {
            Debug.LogError("No map exists in RunStateManager.");
            return;
        }

        ClearMap();
        currentMap = runState.currentMap;
        RenderMap(currentMap);
        RefreshScrollBounds();
        FocusCurrentLayer();

        if (statusText != null)
        {
            statusText.text = statusMessage
                + " Nodes: "
                + currentMap.Nodes.Count
                + ", Edges: "
                + currentMap.Edges.Count;
        }

        RefreshPlayerInfo();
    }

    private void ClearMap()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
    }

    private void HandleMapScroll()
    {
        if (!enableMouseWheelScroll || currentMap == null || mapRoot == null)
        {
            return;
        }

        float wheelDelta = Input.mouseScrollDelta.y;

        if (Mathf.Approximately(wheelDelta, 0f))
        {
            return;
        }

        scrollOffsetY -= wheelDelta * mouseWheelScrollSpeed;
        ApplyScrollOffset();
    }

    private void RefreshScrollBounds()
    {
        if (currentMap == null || mapRoot == null)
        {
            minScrollOffsetY = 0f;
            maxScrollOffsetY = 0f;
            return;
        }

        RectTransform effectiveViewport = GetViewport();
        float viewportHeight = effectiveViewport != null ? effectiveViewport.rect.height : mapRoot.rect.height;
        float topLayerY = GetLayerPositionY(currentMap.Height);
        float bottomLayerY = GetLayerPositionY(0);

        float topViewportY = viewportHeight * 0.5f - scrollPadding;
        float bottomViewportY = -viewportHeight * 0.5f + scrollPadding;
        minScrollOffsetY = topViewportY - topLayerY;
        maxScrollOffsetY = bottomViewportY - bottomLayerY;

        if (minScrollOffsetY > maxScrollOffsetY)
        {
            float midpoint = (minScrollOffsetY + maxScrollOffsetY) * 0.5f;
            minScrollOffsetY = midpoint;
            maxScrollOffsetY = midpoint;
        }
    }

    private void FocusCurrentLayer()
    {
        if (currentMap == null)
        {
            return;
        }

        scrollOffsetY = currentLayerViewportY - GetLayerPositionY(GetCurrentVisibleLayer());
        ApplyScrollOffset();
    }

    private void ApplyScrollOffset()
    {
        if (mapRoot == null)
        {
            return;
        }

        scrollOffsetY = Mathf.Clamp(scrollOffsetY, minScrollOffsetY, maxScrollOffsetY);
        mapRoot.anchoredPosition = mapRootBasePosition + new Vector2(0f, scrollOffsetY);
    }

    private RectTransform GetViewport()
    {
        if (viewport != null)
        {
            return viewport;
        }

        if (mapRoot != null)
        {
            return mapRoot.parent as RectTransform;
        }

        return null;
    }

    private void RenderMap(MapData map)
    {
        for (int i = 0; i < map.Edges.Count; i++)
        {
            DrawEdge(map.Edges[i], map);
        }

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            DrawNode(map.Nodes[i], map);
        }
    }

    private void DrawEdge(MapEdge edge, MapData map)
    {
        Vector2 startPosition = GetNodePosition(edge.From, map);
        Vector2 endPosition = GetNodePosition(edge.To, map);

        Vector2 direction = endPosition - startPosition;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject lineObject = new GameObject(
            "Line_" + edge.From.Id + "_to_" + edge.To.Id,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        lineObject.transform.SetParent(mapRoot, false);
        spawnedObjects.Add(lineObject);

        Image image = lineObject.GetComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;

        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = (startPosition + endPosition) * 0.5f;
        rect.sizeDelta = new Vector2(length, lineThickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void DrawNode(MapNode node, MapData map)
    {
        Button button;

        if (nodeButtonPrefab != null)
        {
            button = Instantiate(nodeButtonPrefab, mapRoot);
        }
        else
        {
            button = CreateDefaultButton(mapRoot);
        }

        button.name = "Room_" + node.Layer + "_" + node.Column + "_" + node.Type;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = nodeSize;
        rect.anchoredPosition = GetNodePosition(node, map);

        RunStateManager runState = RunStateManager.EnsureExists();
        bool isCleared = runState.IsNodeCleared(node.Id);
        bool isAvailable = runState.IsNodeAvailable(node.Id);
        Color nodeColor = GetNodeStateColor(node, isCleared, isAvailable);

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.color = nodeColor;
        }

        ColorBlock colorBlock = button.colors;
        colorBlock.normalColor = nodeColor;
        colorBlock.highlightedColor = Color.Lerp(nodeColor, Color.white, 0.25f);
        colorBlock.pressedColor = Color.Lerp(nodeColor, Color.black, 0.2f);
        colorBlock.selectedColor = nodeColor;
        colorBlock.disabledColor = nodeColor;
        button.colors = colorBlock;
        button.interactable = isAvailable;

        Text text = button.GetComponentInChildren<Text>(true);

        if (text != null)
        {
            text.text = GetRoomProgressLabel(node, isCleared, isAvailable);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.color = textColor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = fontSize;

            Font font = GetFont();

            if (font != null)
            {
                text.font = font;
            }
        }

        MapNode capturedNode = node;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate
        {
            OnRoomButtonClicked(capturedNode);
        });

        spawnedObjects.Add(button.gameObject);
    }

    private Button CreateDefaultButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "RoomButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)
        );

        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = textColor;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = fontSize;

        Font font = GetFont();

        if (font != null)
        {
            text.font = font;
        }

        return button;
    }

    private Font GetFont()
    {
        if (uiFont != null)
        {
            return uiFont;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private Vector2 GetNodePosition(MapNode node, MapData map)
    {
        float x = (node.Column - (map.Width - 1) / 2f) * cellSpacing.x;
        float y = GetLayerPositionY(node.Layer);

        return new Vector2(x, y);
    }

    private float GetLayerPositionY(int layer)
    {
        int mapHeight = currentMap != null ? currentMap.Height : height;
        return (layer - mapHeight / 2f) * cellSpacing.y;
    }

    private int GetCurrentVisibleLayer()
    {
        RunStateManager runState = RunStateManager.EnsureExists();

        if (runState.pendingSelectedNode != null)
        {
            return runState.pendingSelectedNode.Layer;
        }

        if (runState.currentSelectedNode != null)
        {
            return runState.currentSelectedNode.Layer;
        }

        IReadOnlyList<string> availableIds = runState.AvailableNextNodeIds;
        int lowestAvailableLayer = int.MaxValue;

        for (int i = 0; i < availableIds.Count; i++)
        {
            string[] idParts = availableIds[i].Split('_');

            if (idParts.Length == 0)
            {
                continue;
            }

            int layer;

            if (int.TryParse(idParts[0], out layer) && layer < lowestAvailableLayer)
            {
                lowestAvailableLayer = layer;
            }
        }

        if (lowestAvailableLayer != int.MaxValue)
        {
            return lowestAvailableLayer;
        }

        return 0;
    }

    private Color GetNodeStateColor(MapNode node, bool isCleared, bool isAvailable)
    {
        Color roomColor = GetRoomColor(node.Type);

        if (isCleared)
        {
            return Color.Lerp(roomColor, completedColor, 0.45f);
        }

        if (!isAvailable)
        {
            return Color.Lerp(roomColor, lockedColor, 0.45f);
        }

        return roomColor;
    }

    private string GetRoomProgressLabel(MapNode node, bool isCleared, bool isAvailable)
    {
        string roomLabel = GetRoomLabel(node.Type);

        if (isCleared)
        {
            return "Done\n" + roomLabel;
        }

        if (!isAvailable)
        {
            return "Locked\n" + roomLabel;
        }

        return roomLabel;
    }

    private Color GetRoomColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Normal:
                return normalColor;

            case RoomType.Elite:
                return eliteColor;

            case RoomType.Shop:
                return shopColor;

            case RoomType.Rest:
                return restColor;

            case RoomType.Chest:
                return chestColor;

            case RoomType.Boss:
                return bossColor;

            default:
                return normalColor;
        }
    }

    private string GetRoomLabel(RoomType type)
    {
        switch (type)
        {
            case RoomType.Normal:
                return "Enemy";

            case RoomType.Elite:
                return "Elite";

            case RoomType.Shop:
                return "Shop";

            case RoomType.Rest:
                return "Rest";

            case RoomType.Chest:
                return "Chest";

            case RoomType.Boss:
                return "Boss";

            default:
                return "Room";
        }
    }

    private void OnRoomButtonClicked(MapNode node)
    {
        RunStateManager runState = RunStateManager.EnsureExists();

        if (!runState.TrySelectNode(node))
        {
            if (statusText != null)
            {
                statusText.text = "This room is locked: " + node.Id;
            }

            return;
        }

        string message = "Selected Room | Floor: "
            + (node.Layer + 1)
            + " | Column: "
            + node.Column
            + " | Type: "
            + node.Type;

        Debug.Log(message);

        if (statusText != null)
        {
            statusText.text = message;
        }

        switch (node.Type)
        {
            case RoomType.Normal:
            case RoomType.Elite:
            case RoomType.Boss:
                SceneManager.LoadScene(battleSceneName);
                GameManager.Instance.currentState = GameState.Preparation;
                break;

            case RoomType.Shop:
                SceneManager.LoadScene(shopSceneName);
                GameManager.Instance.currentState = GameState.Shopping;
                break;

            case RoomType.Rest:
                SceneManager.LoadScene(restSceneName);
                GameManager.Instance.currentState = GameState.Rest;
                break;

            case RoomType.Chest:
                SceneManager.LoadScene(chestSceneName);
                break;
        }
    }

    private void CompleteSimpleRoomAndStayOnMap(MapNode node)
    {
        RunStateManager runState = RunStateManager.EnsureExists();
        runState.CompletePendingRoom();
        RenderCurrentMap(node.Type + " room completed.");
    }

    private void RefreshPlayerInfo()
    {
        if (playerInfoText == null)
        {
            return;
        }

        PlayerManager playerManager = PlayerManager.EnsureExists();
        int cardCount = HandManager.Instance != null ? HandManager.Instance.CardCount : 0;

        playerInfoText.text = "Lives: "
            + playerManager.health
            + " / "
            + PlayerManager.DefaultLives
            + "\nGold: "
            + playerManager.money
            + "\nCards: "
            + cardCount;
    }
}
