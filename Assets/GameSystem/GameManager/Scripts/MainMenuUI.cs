using UnityEngine;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class MainMenuUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform backgroundPanel;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Make sure Canvas is properly scaled for World Space
        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800f, 600f);
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f); // Perfect size in 3D Space

        CreateUI();
    }

    private void CreateUI()
    {
        // 1. Create Background Panel with rich glassmorphic night-blue styling
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bgObj.transform.SetParent(transform, false);
        backgroundPanel = bgObj.GetComponent<RectTransform>();
        backgroundPanel.sizeDelta = new Vector2(800f, 600f);
        backgroundPanel.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        // Deep sleek semi-transparent navy
        bgImg.color = new Color(0.04f, 0.06f, 0.14f, 0.85f);

        // 2. Create Title (Large Glowing White Text)
        CreateText("TitleText", "RotaBeat", 60f, new Vector2(750f, 90f), new Vector2(0f, 170f), Color.white, backgroundPanel);

        // 3. Create Subtitle (Vibrant Cyan Text)
        CreateText("SubtitleText", "Rotate, Punch, and Hold", 24f, new Vector2(750f, 50f), new Vector2(0f, 100f), new Color(0f, 0.85f, 1f, 0.9f), backgroundPanel);

        // 4. Create Start Button Y = -20f
        CreateButton("StartButton", "START GAME", new Vector2(380f, 90f), new Vector2(0f, -20f), OnStartClicked, backgroundPanel);

        // 5. Create Quit Button Y = -140f
        CreateButton("QuitButton", "QUIT GAME", new Vector2(380f, 90f), new Vector2(0f, -140f), OnQuitClicked, backgroundPanel);
    }

    private TextMeshProUGUI CreateText(string objName, string content, float fontSize, Vector2 size, Vector2 pos, Color color, Transform parent)
    {
        GameObject textObj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        // Try to bind default UIManager font if available for consistency
        if (UIManager.Instance != null)
        {
            TextMeshProUGUI sourceText = UIManager.Instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sourceText != null)
            {
                text.font = sourceText.font;
            }
        }

        return text;
    }

    private GameObject CreateButton(string objName, string text, Vector2 size, Vector2 pos, System.Action onClick, Transform parent)
    {
        GameObject buttonObj = new GameObject(objName, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BoxCollider), typeof(VRButton));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        UnityEngine.UI.Image img = buttonObj.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.12f, 0.18f, 0.32f, 0.8f);

        // Add visual text child centered
        CreateText(objName + "_Text", text, 32f, size, Vector2.zero, Color.white, buttonObj.transform);

        // Setup collider for physics raycasting
        BoxCollider col = buttonObj.GetComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 10f);
        col.center = Vector3.zero;

        // Setup button component
        VRButton vrBtn = buttonObj.GetComponent<VRButton>();
        vrBtn.Setup(new Color(0.12f, 0.18f, 0.32f, 0.8f), new Color(0.18f, 0.42f, 0.95f, 0.95f), onClick);

        return buttonObj;
    }

    private void OnStartClicked()
    {
        Debug.Log("[MainMenuUI] Start Clicked!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.SongSelect);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quit Clicked!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
}
