using UnityEngine;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class PauseUI : MonoBehaviour
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
        // Deep sleek semi-transparent navy (90% opacity for better occlusion in gameplay)
        bgImg.color = new Color(0.04f, 0.06f, 0.14f, 0.90f);

        // 2. Create Title (Large Glowing White Text)
        VRMenuHelper.CreateText("TitleText", "GAME PAUSED", 60f, new Vector2(750f, 90f), new Vector2(0f, 170f), Color.white, backgroundPanel);

        // 3. Create Subtitle (Vibrant Cyan Text)
        VRMenuHelper.CreateText("SubtitleText", "Select an option to proceed", 24f, new Vector2(750f, 50f), new Vector2(0f, 100f), new Color(0f, 0.85f, 1f, 0.9f), backgroundPanel);

        // 4. Create Resume Button Y = 10f
        VRMenuHelper.CreateButton("ResumeButton", "RESUME GAME", 32f, new Vector2(400f, 80f), new Vector2(0f, 10f), OnResumeClicked, backgroundPanel);

        // 5. Create Song Select Button Y = -90f
        VRMenuHelper.CreateButton("SongSelectButton", "SONG SELECT", 32f, new Vector2(400f, 80f), new Vector2(0f, -90f), OnSongSelectClicked, backgroundPanel);

        // 6. Create Main Menu Button Y = -190f
        VRMenuHelper.CreateButton("MainMenuButton", "MAIN MENU", 32f, new Vector2(400f, 80f), new Vector2(0f, -190f), OnMainMenuClicked, backgroundPanel);
    }

    private void OnResumeClicked()
    {
        Debug.Log("[PauseUI] Resume Clicked!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    private void OnSongSelectClicked()
    {
        Debug.Log("[PauseUI] Song Select Clicked!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToSongSelect();
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[PauseUI] Main Menu Clicked!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenuFromPause();
        }
    }
}
