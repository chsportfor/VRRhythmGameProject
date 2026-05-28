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
        VRMenuHelper.CreateText("TitleText", "RotaBeat", 60f, new Vector2(750f, 90f), new Vector2(0f, 170f), Color.white, backgroundPanel);

        // 3. Create Subtitle (Vibrant Cyan Text)
        VRMenuHelper.CreateText("SubtitleText", "Rotate, Punch, and Hold", 24f, new Vector2(750f, 50f), new Vector2(0f, 100f), new Color(0f, 0.85f, 1f, 0.9f), backgroundPanel);

        // 4. Create Start Button Y = -20f
        VRMenuHelper.CreateButton("StartButton", "START GAME", 32f, new Vector2(380f, 90f), new Vector2(0f, -20f), OnStartClicked, backgroundPanel);

        // 5. Create Quit Button Y = -140f
        VRMenuHelper.CreateButton("QuitButton", "QUIT GAME", 32f, new Vector2(380f, 90f), new Vector2(0f, -140f), OnQuitClicked, backgroundPanel);
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
