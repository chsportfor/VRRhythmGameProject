using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState
{
    MainMenu,
    SongSelect,
    Playing
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;

    [Header("Songs")]
    public List<BeatmapData> availableSongs = new List<BeatmapData>();
    public BeatmapData selectedSong;

    [Header("References")]
    public NoteSpawner noteSpawner;
    public UIManager uiManager;
    public VRUIInteractor vrInteractor;
    public MainMenuUI mainMenuUI;
    public SongSelectUI songSelectUI;

    [Header("Loading UI")]
    public GameObject loadingPanel;
    private UnityEngine.UI.Image loadingBarFill;
    private TextMeshProUGUI loadingText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Populate available songs in editor
#if UNITY_EDITOR
        if (availableSongs == null || availableSongs.Count == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BeatmapData");
            availableSongs = new List<BeatmapData>();
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                BeatmapData asset = UnityEditor.AssetDatabase.LoadAssetAtPath<BeatmapData>(path);
                if (asset != null)
                {
                    availableSongs.Add(asset);
                }
            }
        }
#endif

        // Create and setup dynamic menus as DontDestroyOnLoad children
        CreateAndSetupMenus();
        
        // Resolve interactor
        ResolveVRInteractor();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Initial setup for the starting scene
        Scene activeScene = SceneManager.GetActiveScene();
        HandleSceneSetup(activeScene.name);
    }

    private void Update()
    {
        // Global Y button or Escape key to abort and return to menu when playing
        if (currentState == GameState.Playing)
        {
            bool triggerReturn = false;

            if (OVRInput.GetDown(OVRInput.Button.Four)) // Y button on left controller
            {
                triggerReturn = true;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                triggerReturn = true;
            }

            if (triggerReturn)
            {
                ReturnToMainMenu();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleSceneSetup(scene.name);
    }

    private void HandleSceneSetup(string sceneName)
    {
        // Re-resolve VR Interactor to find camera/controllers in the loaded scene
        ResolveVRInteractor();

        if (sceneName == "SampleScene")
        {
            // Connect gameplay elements
            noteSpawner = FindAnyObjectByType<NoteSpawner>();
            uiManager = FindAnyObjectByType<UIManager>();

            if (noteSpawner != null && selectedSong != null)
            {
                noteSpawner.PrepareBeatmap(selectedSong);
                if (selectedSong.musicClip != null && noteSpawner.audioSource != null)
                {
                    noteSpawner.audioSource.clip = selectedSong.musicClip;
                }
            }

            // Align menus with the HUD so they are in the exact track-aligned space (if they get shown)
            if (uiManager != null)
            {
                Vector3 hudPos = uiManager.transform.position;
                Quaternion hudRot = uiManager.transform.rotation;
                
                if (mainMenuUI != null)
                {
                    mainMenuUI.transform.position = hudPos;
                    mainMenuUI.transform.rotation = hudRot;
                }
                if (songSelectUI != null)
                {
                    songSelectUI.transform.position = hudPos;
                    songSelectUI.transform.rotation = hudRot;
                }
                if (loadingPanel != null)
                {
                    loadingPanel.transform.position = hudPos;
                    loadingPanel.transform.rotation = hudRot;
                }
            }

            ChangeState(GameState.Playing);
        }
        else if (sceneName == "MainMenuScene")
        {
            // Position menus cleanly in front of the active VR camera rig
            PositionMenusInFrontOfCamera();

            ChangeState(GameState.MainMenu);
        }
    }

    private void ResolveVRInteractor()
    {
        if (vrInteractor == null)
        {
            GameObject interactorObj = new GameObject("VRUIInteractor");
            interactorObj.transform.SetParent(transform); // Stay as child of GameManager
            vrInteractor = interactorObj.AddComponent<VRUIInteractor>();
        }

        // Locate OVR controllers or fallback camera in the new scene
        Transform rightHand = null;
        OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            rightHand = rig.rightControllerAnchor;
            if (rightHand == null) rightHand = rig.rightHandAnchor;
        }

        if (rightHand == null)
        {
            if (Camera.main != null) rightHand = Camera.main.transform;
        }

        if (rightHand != null)
        {
            vrInteractor.transform.SetParent(rightHand, false);
            vrInteractor.transform.localPosition = Vector3.zero;
            vrInteractor.transform.localRotation = Quaternion.identity;
            Debug.Log($"[GameManager] VRUIInteractor bound to hand anchor: {rightHand.name}");
        }
        else
        {
            Debug.LogWarning("[GameManager] Could not find right hand anchor or MainCamera to bind interactor.");
        }
    }

    private void CreateAndSetupMenus()
    {
        Vector3 menuPos = new Vector3(0f, 1.35f, 3.2f);
        Quaternion menuRot = Quaternion.identity;

        // Create Main Menu Canvas as GameManager child
        GameObject mainMenuObj = new GameObject("MainMenuUI");
        mainMenuObj.transform.position = menuPos;
        mainMenuObj.transform.rotation = menuRot;
        mainMenuUI = mainMenuObj.AddComponent<MainMenuUI>();
        mainMenuObj.transform.SetParent(transform);

        // Create Song Select Canvas as GameManager child
        GameObject songSelectObj = new GameObject("SongSelectUI");
        songSelectObj.transform.position = menuPos;
        songSelectObj.transform.rotation = menuRot;
        songSelectUI = songSelectObj.AddComponent<SongSelectUI>();
        songSelectObj.transform.SetParent(transform);

        // Create Loading UI Canvas as GameManager child
        GameObject loadingObj = new GameObject("LoadingUI", typeof(Canvas));
        loadingObj.transform.position = menuPos;
        loadingObj.transform.rotation = menuRot;
        loadingObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
        loadingObj.transform.SetParent(transform);
        
        Canvas loadCanvas = loadingObj.GetComponent<Canvas>();
        loadCanvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform loadCanvasRect = loadingObj.GetComponent<RectTransform>();
        loadCanvasRect.sizeDelta = new Vector2(800f, 600f);

        // Background (Deep night-blue sleek card)
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bgObj.transform.SetParent(loadingObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(800f, 600f);
        UnityEngine.UI.Image bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.14f, 0.95f);

        // Text: LOADING... 0%
        GameObject txtObj = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(bgObj.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(700f, 80f);
        txtRect.anchoredPosition = new Vector2(0f, 50f);
        loadingText = txtObj.GetComponent<TextMeshProUGUI>();
        loadingText.text = "LOADING... 0%";
        loadingText.fontSize = 48f;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = Color.white;
        loadingText.fontStyle = FontStyles.Bold;

        // Copy UIManager font if available
        if (UIManager.Instance != null)
        {
            TextMeshProUGUI sourceText = UIManager.Instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sourceText != null)
            {
                loadingText.font = sourceText.font;
            }
        }

        // Progress Bar Background
        GameObject barBgObj = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        barBgObj.transform.SetParent(bgObj.transform, false);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.sizeDelta = new Vector2(500f, 30f);
        barBgRect.anchoredPosition = new Vector2(0f, -50f);
        UnityEngine.UI.Image barBgImg = barBgObj.GetComponent<UnityEngine.UI.Image>();
        barBgImg.color = new Color(0.1f, 0.15f, 0.28f, 0.8f);

        // Progress Bar Fill
        GameObject barFillObj = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform barFillRect = barFillObj.GetComponent<RectTransform>();
        barFillRect.sizeDelta = new Vector2(500f, 30f);
        barFillRect.anchoredPosition = Vector2.zero;
        
        loadingBarFill = barFillObj.GetComponent<UnityEngine.UI.Image>();
        loadingBarFill.color = new Color(0f, 0.85f, 1f, 0.9f);
        loadingBarFill.type = UnityEngine.UI.Image.Type.Filled;
        loadingBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        loadingBarFill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        loadingBarFill.fillAmount = 0f;

        loadingPanel = loadingObj;
        loadingPanel.SetActive(false); // Hide initially
    }

    private void PositionMenusInFrontOfCamera()
    {
        Transform cam = null;
        if (Camera.main != null) cam = Camera.main.transform;

        Vector3 menuPos;
        Quaternion menuRot;

        if (cam != null)
        {
            Vector3 forward = cam.forward;
            forward.y = 0f;
            forward.Normalize();
            menuPos = cam.position + forward * 3.2f;
            menuPos.y = 1.35f; // eye level height

            menuRot = Quaternion.LookRotation(forward);
        }
        else
        {
            menuPos = new Vector3(0f, 1.35f, 3.2f);
            menuRot = Quaternion.identity;
        }

        if (mainMenuUI != null)
        {
            mainMenuUI.transform.position = menuPos;
            mainMenuUI.transform.rotation = menuRot;
        }
        if (songSelectUI != null)
        {
            songSelectUI.transform.position = menuPos;
            songSelectUI.transform.rotation = menuRot;
        }
        if (loadingPanel != null)
        {
            loadingPanel.transform.position = menuPos;
            loadingPanel.transform.rotation = menuRot;
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {currentState}");

        // Handle UI toggles
        if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(currentState == GameState.MainMenu);
        if (songSelectUI != null) songSelectUI.gameObject.SetActive(currentState == GameState.SongSelect);

        if (uiManager != null)
        {
            uiManager.SetHudVisible(currentState == GameState.Playing);
        }

        // Manage laser pointer visibility
        if (vrInteractor != null)
        {
            vrInteractor.SetPointerActive(currentState == GameState.MainMenu || currentState == GameState.SongSelect);
        }

        switch (currentState)
        {
            case GameState.MainMenu:
                if (noteSpawner != null) noteSpawner.ResetGame();
                break;

            case GameState.SongSelect:
                if (songSelectUI != null) songSelectUI.RefreshList(availableSongs);
                break;

            case GameState.Playing:
                if (uiManager != null)
                {
                    uiManager.ShowPressToStartPrompt();
                }
                break;
        }
    }

    public void SelectSong(BeatmapData song)
    {
        selectedSong = song;
        Debug.Log($"[GameManager] Song Selected: {song.songName}. Loading gameplay scene asynchronously with progress bar.");

        // Hide other menus
        if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);
        if (songSelectUI != null) songSelectUI.gameObject.SetActive(false);
        if (vrInteractor != null) vrInteractor.SetPointerActive(false);

        StartCoroutine(LoadSceneAsyncRoutine("SampleScene"));
    }

    public void OnSongFinished()
    {
        Debug.Log("[GameManager] Song finished! Returning to Main Menu.");
        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        if (noteSpawner != null)
        {
            noteSpawner.ResetGame();
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        // Load the MainMenu scene asynchronously with progress bar
        StartCoroutine(LoadSceneAsyncRoutine("MainMenuScene"));
    }

    private System.Collections.IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            UpdateLoadingProgress(0f);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Prevent activating until progress reaches 100%

        float displayedProgress = 0f;

        while (displayedProgress < 100f)
        {
            // asyncLoad.progress caps at 0.9. Map 0-0.9 to 0-100%
            float targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f) * 100f;
            
            // Smoothly Lerp the displayed progress bar
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.deltaTime * 120f);
            
            UpdateLoadingProgress(displayedProgress);
            yield return null;
        }

        // Complete 100% state
        UpdateLoadingProgress(100f);
        yield return new WaitForSeconds(0.4f); // Keep 100% visible briefly for satisfying feedback

        // Allow scene transition to complete
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Hide loading screen
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        Debug.Log($"[GameManager] Async scene load completed and activated: {sceneName}");
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (loadingBarFill != null)
        {
            loadingBarFill.fillAmount = progress / 100f;
        }
        if (loadingText != null)
        {
            loadingText.text = $"LOADING... {Mathf.RoundToInt(progress)}%";
        }
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
