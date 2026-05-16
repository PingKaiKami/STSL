using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach this to a GameObject in MapGenerationScene.
// It creates a simple runtime UI so this feature scene can be tested without editing classmates' scenes.
public class MapGenerationSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        EnsureEventSystemExists();
        BuildMapUi();
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void BuildMapUi()
    {
        GameObject canvasObject = new GameObject("MapGenerationCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        Text statusText = CreateText(canvasRect, "StatusText", "Map Generation", new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(640f, 40f), 20);
        Button newRunButton = CreateButton(canvasRect, "NewRunButton", "New Run", new Vector2(1f, 1f), new Vector2(-90f, -32f), new Vector2(140f, 44f));

        GameObject mapRootObject = new GameObject("MapRoot", typeof(RectTransform));
        mapRootObject.transform.SetParent(canvasObject.transform, false);

        RectTransform mapRoot = mapRootObject.GetComponent<RectTransform>();
        mapRoot.anchorMin = new Vector2(0.5f, 0.5f);
        mapRoot.anchorMax = new Vector2(0.5f, 0.5f);
        mapRoot.pivot = new Vector2(0.5f, 0.5f);
        mapRoot.anchoredPosition = new Vector2(0f, -20f);
        mapRoot.sizeDelta = new Vector2(1100f, 620f);

        MapUIRenderer renderer = mapRootObject.AddComponent<MapUIRenderer>();
        renderer.mapRoot = mapRoot;
        renderer.statusText = statusText;
        renderer.generateButton = newRunButton;
        renderer.battleSceneName = "BattleScene";
        renderer.shopSceneName = "ShopScene";
        renderer.restSceneName = "RestScene";
        renderer.chestSceneName = "ChestScene";
        renderer.generateOnStart = true;

        newRunButton.onClick.AddListener(renderer.GenerateAndRenderMap);
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        string content,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.font = GetDefaultFont();

        return text;
    }

    private Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        CreateText(buttonObject.transform, "Text", label, new Vector2(0.5f, 0.5f), Vector2.zero, size, 18).color = Color.black;

        return button;
    }

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
