using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class SongSelectUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform backgroundPanel;
    private RectTransform viewportTransform;
    private RectTransform contentTransform;

    [Header("Scroll Settings")]
    public float scrollSpeed = 400f;
    private float viewportHeight = 420f;
    private float contentHeight = 0f;

    private List<GameObject> activeItems = new List<GameObject>();

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900f, 700f);
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f); // Match MainMenuUI

        CreateUI();
    }

    private void CreateUI()
    {
        // 1. Create Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bgObj.transform.SetParent(transform, false);
        backgroundPanel = bgObj.GetComponent<RectTransform>();
        backgroundPanel.sizeDelta = new Vector2(900f, 700f);
        backgroundPanel.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.14f, 0.85f);

        // 2. Create Title
        CreateText("TitleText", "SELECT SONG", 48f, new Vector2(800f, 80f), new Vector2(0f, 270f), Color.white, backgroundPanel);

        // 3. Create Viewport (Masked scroll box) Y=20f, height=420f
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
        viewportObj.transform.SetParent(backgroundPanel, false);
        viewportTransform = viewportObj.GetComponent<RectTransform>();
        viewportTransform.sizeDelta = new Vector2(820f, viewportHeight);
        viewportTransform.anchoredPosition = new Vector2(0f, 20f);

        UnityEngine.UI.Image viewImg = viewportObj.GetComponent<UnityEngine.UI.Image>();
        // Mask image needs fully white color with alpha to mask children correctly
        viewImg.color = new Color(1f, 1f, 1f, 0.005f); 

        // 4. Create Content Transform inside Viewport
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportTransform, false);
        contentTransform = contentObj.GetComponent<RectTransform>();
        
        // Pivot at top-center, anchor at top-center
        contentTransform.anchorMin = new Vector2(0.5f, 1f);
        contentTransform.anchorMax = new Vector2(0.5f, 1f);
        contentTransform.pivot = new Vector2(0.5f, 1f);
        contentTransform.anchoredPosition = Vector2.zero;

        // 5. Create Return Back Button Y = -270f
        CreateButton("BackButton", "BACK TO MAIN", new Vector2(300f, 70f), new Vector2(0f, -270f), OnBackClicked, backgroundPanel);
    }

    public void RefreshList(List<BeatmapData> songs)
    {
        // Clean up previous items
        foreach (var item in activeItems)
        {
            Destroy(item);
        }
        activeItems.Clear();

        if (songs == null || songs.Count == 0)
        {
            // If no songs, show simple warning
            GameObject warning = CreateText("NoSongsWarning", "NO SONGS AVAILABLE", 32f, new Vector2(700f, 60f), new Vector2(0f, -180f), Color.gray, contentTransform).gameObject;
            activeItems.Add(warning);
            contentHeight = 200f;
            contentTransform.sizeDelta = new Vector2(800f, contentHeight);
            return;
        }

        float itemHeight = 120f;
        float spacing = 15f;
        float currentY = -15f;

        for (int i = 0; i < songs.Count; i++)
        {
            BeatmapData song = songs[i];
            
            // Container item panel
            GameObject itemObj = new GameObject($"SongItem_{i}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BoxCollider), typeof(VRButton));
            itemObj.transform.SetParent(contentTransform, false);
            activeItems.Add(itemObj);

            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0.5f, 1f);
            itemRect.anchorMax = new Vector2(0.5f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(780f, itemHeight);
            itemRect.anchoredPosition = new Vector2(0f, currentY);

            UnityEngine.UI.Image itemImg = itemObj.GetComponent<UnityEngine.UI.Image>();
            itemImg.color = new Color(0.1f, 0.15f, 0.28f, 0.7f);

            // Add Song Title Text (Left Aligned)
            // Trim extension if present in songName
            string cleanSongName = song.songName;
            if (cleanSongName.EndsWith(".wav") || cleanSongName.EndsWith(".mp3"))
            {
                cleanSongName = System.IO.Path.GetFileNameWithoutExtension(cleanSongName);
            }
            // Remove number prefix (like "02 ") for a clean display
            if (cleanSongName.StartsWith("02 ") || cleanSongName.StartsWith("05 "))
            {
                cleanSongName = cleanSongName.Substring(3);
            }

            GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleTextObj.transform.SetParent(itemObj.transform, false);
            RectTransform titleRect = titleTextObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0.8f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.sizeDelta = new Vector2(500f, 80f);
            titleRect.anchoredPosition = new Vector2(30f, 0f);

            TextMeshProUGUI titleText = titleTextObj.GetComponent<TextMeshProUGUI>();
            titleText.text = cleanSongName;
            titleText.fontSize = 32f;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;

            // Add BPM & Duration Text (Right Aligned)
            string durationText = "0:00";
            if (song.musicClip != null)
            {
                int minutes = Mathf.FloorToInt(song.musicClip.length / 60f);
                int seconds = Mathf.FloorToInt(song.musicClip.length % 60f);
                durationText = $"{minutes:0}:{seconds:00}";
            }

            GameObject infoTextObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoTextObj.transform.SetParent(itemObj.transform, false);
            RectTransform infoRect = infoTextObj.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.7f, 0.5f);
            infoRect.anchorMax = new Vector2(1f, 0.5f);
            infoRect.pivot = new Vector2(1f, 0.5f);
            infoRect.sizeDelta = new Vector2(200f, 80f);
            infoRect.anchoredPosition = new Vector2(-30f, 0f);

            TextMeshProUGUI infoText = infoTextObj.GetComponent<TextMeshProUGUI>();
            infoText.text = $"BPM {song.bpm}\n{durationText}";
            infoText.fontSize = 24f;
            infoText.alignment = TextAlignmentOptions.MidlineRight;
            infoText.color = new Color(0f, 0.85f, 1f, 0.9f);
            infoText.fontStyle = FontStyles.Normal;
            infoText.raycastTarget = false;

            // Copy UIManager fonts
            if (UIManager.Instance != null)
            {
                TextMeshProUGUI sourceText = UIManager.Instance.GetComponentInChildren<TextMeshProUGUI>(true);
                if (sourceText != null)
                {
                    titleText.font = sourceText.font;
                    infoText.font = sourceText.font;
                }
            }

            // Setup physics raycast collider bounds
            BoxCollider col = itemObj.GetComponent<BoxCollider>();
            col.size = new Vector3(780f, itemHeight, 10f);
            col.center = Vector3.zero;

            // Setup VRButton interaction click callback
            VRButton vrBtn = itemObj.GetComponent<VRButton>();
            BeatmapData currentSong = song; // Prevent capture issue in lambda
            vrBtn.Setup(
                new Color(0.1f, 0.15f, 0.28f, 0.7f),
                new Color(0.18f, 0.42f, 0.95f, 0.9f),
                () => { GameManager.Instance.SelectSong(currentSong); }
            );

            currentY -= (itemHeight + spacing);
        }

        // Calculate total content height for clamping scrolling bounds
        contentHeight = Mathf.Abs(currentY) + 15f;
        contentTransform.sizeDelta = new Vector2(800f, contentHeight);
        contentTransform.anchoredPosition = Vector2.zero; // Reset position
    }

    private void Update()
    {
        // Handle Scroll Wheel / Controller thumbstick scrolling
        float scrollInput = 0f;
        
        // VR thumbstick input
        scrollInput -= OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
        scrollInput -= OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

        // Editor fallback scrolling using Arrow keys
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed) scrollInput = -1f;
            if (Keyboard.current.downArrowKey.isPressed) scrollInput = 1f;
        }

        if (scrollInput != 0f)
        {
            Scroll(scrollInput * scrollSpeed * Time.deltaTime);
        }
    }

    private void Scroll(float amount)
    {
        if (contentTransform == null) return;

        float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);
        Vector2 pos = contentTransform.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y + amount, 0f, maxScrollY);
        contentTransform.anchoredPosition = pos;
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

        CreateText(objName + "_Text", text, 24f, size, Vector2.zero, Color.white, buttonObj.transform);

        BoxCollider col = buttonObj.GetComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 10f);
        col.center = Vector3.zero;

        VRButton vrBtn = buttonObj.GetComponent<VRButton>();
        vrBtn.Setup(new Color(0.12f, 0.18f, 0.32f, 0.8f), new Color(0.18f, 0.42f, 0.95f, 0.95f), onClick);

        return buttonObj;
    }

    private void OnBackClicked()
    {
        Debug.Log("[SongSelectUI] Back to Main clicked.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }
}
