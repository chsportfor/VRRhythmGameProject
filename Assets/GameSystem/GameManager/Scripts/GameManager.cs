using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState
{
    MainMenu,
    SongSelect,
    Playing,
    Result
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    public GameState CurrentState => currentState;

    [Header("Songs")]
    public List<BeatmapData> availableSongs = new List<BeatmapData>();
    [HideInInspector] public BeatmapData selectedSong;

    [HideInInspector] public NoteSpawner noteSpawner;
    [HideInInspector] public UIManager uiManager;
    [HideInInspector] public VRUIInteractor vrInteractor;
    [HideInInspector] public MainMenuUI mainMenuUI;
    [HideInInspector] public SongSelectUI songSelectUI;
    [HideInInspector] public PauseUI pauseUI;
    [HideInInspector] public ResultUI resultUI;

    private bool isPaused = false;
    private GameState targetStateAfterLoad = GameState.MainMenu;

    [HideInInspector] public GameObject loadingPanel;
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
        // Left Controller Y Button (Button.Four) or Escape key to toggle pause when playing
        if (currentState == GameState.Playing)
        {
            if (OVRInput.GetDown(OVRInput.Button.Four) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                TogglePause();
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
                
                SetAllMenuTransforms(hudPos, hudRot);
            }

            ChangeState(GameState.Playing);
        }
        else if (sceneName == "MainMenuScene")
        {
            // Position menus cleanly in front of the active VR camera rig
            PositionMenusInFrontOfCamera();

            ChangeState(targetStateAfterLoad);
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

        // Create Pause UI Canvas as GameManager child
        GameObject pauseObj = new GameObject("PauseUI");
        pauseObj.transform.position = menuPos;
        pauseObj.transform.rotation = menuRot;
        pauseUI = pauseObj.AddComponent<PauseUI>();
        pauseObj.transform.SetParent(transform);
        pauseUI.gameObject.SetActive(false); // Hide initially

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

        SetAllMenuTransforms(menuPos, menuRot);
    }

    private void SetAllMenuTransforms(Vector3 pos, Quaternion rot)
    {
        if (mainMenuUI != null)
        {
            mainMenuUI.transform.position = pos;
            mainMenuUI.transform.rotation = rot;
        }
        if (songSelectUI != null)
        {
            songSelectUI.transform.position = pos;
            songSelectUI.transform.rotation = rot;
        }
        if (pauseUI != null)
        {
            pauseUI.transform.position = pos;
            pauseUI.transform.rotation = rot;
        }
        if (loadingPanel != null)
        {
            loadingPanel.transform.position = pos;
            loadingPanel.transform.rotation = rot;
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {currentState}");

        // Handle UI toggles
        if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(currentState == GameState.MainMenu);
        if (songSelectUI != null) songSelectUI.gameObject.SetActive(currentState == GameState.SongSelect);
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);
        if (resultUI != null) resultUI.gameObject.SetActive(currentState == GameState.Result);

        if (uiManager != null)
        {
            uiManager.SetHudVisible(currentState == GameState.Playing);
        }

        // Manage laser pointer visibility
        if (vrInteractor != null)
        {
            vrInteractor.SetPointerActive(currentState == GameState.MainMenu || currentState == GameState.SongSelect || currentState == GameState.Result);
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
        Debug.Log("[GameManager] Song finished! Calculating results and showing Result Screen.");

        // 🚀 1. 씬에 남아있는 모든 잔여 노트들 강제 파괴 정리! (판정 도중 꼬임 예방)
        BaseNote[] activeNotes = FindObjectsByType<BaseNote>(FindObjectsSortMode.None);
        foreach (BaseNote note in activeNotes)
        {
            if (note != null && note.gameObject != null)
            {
                Destroy(note.gameObject);
            }
        }

        // 🚀 2. 인게임 라인(TrackManager)의 자식 메쉬들만 안전하게 일괄 오프하여 NullReferenceException 완벽 차단!
        if (TrackManager.Instance != null)
        {
            TrackManager.Instance.SetTrackVisualsActive(false);
            Debug.Log("[GameManager] Safely hid all track visual children lanes and judgment rings.");
        }

        // 🚀 3. UIManager HUD 비활성화
        if (uiManager != null)
        {
            uiManager.SetHudVisible(false);
        }

        // 🚀 4. 게임 상태를 Result로 '먼저' 변경하여 캔버스(Awake)를 먼저 활성화합니다! (텍스트 필드 바인딩 null 방지)
        ChangeState(GameState.Result);

        // 🚀 5. ScoreManager 데이터 수집 및 ResultUI 정보 바인딩
        string songTitle = selectedSong != null ? selectedSong.songName : "UNKNOWN";
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        float finalAccuracy = ScoreManager.Instance != null ? ScoreManager.Instance.Accuracy : 100f;
        int totalMisses = ScoreManager.Instance != null ? ScoreManager.Instance.MissCount : 0;

        if (resultUI != null)
        {
            resultUI.DisplayResult(songTitle, finalScore, finalAccuracy, totalMisses);
        }
    }

    /// <summary>
    /// 현재 재생 중이던 곡을 처음부터 다시 깔끔하게 재실행(재도전) 합니다.
    /// </summary>
    public void RestartCurrentSong()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);
        if (resultUI != null) resultUI.gameObject.SetActive(false);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        if (noteSpawner != null)
        {
            noteSpawner.ResetGame();
        }

        Debug.Log($"[GameManager] Restarting active song: {selectedSong?.songName}. Reloading gameplay scene.");
        StartCoroutine(LoadSceneAsyncRoutine("SampleScene", GameState.Playing));
    }

    public void TogglePause()
    {
        if (currentState != GameState.Playing) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (noteSpawner != null)
        {
            if (isPaused) noteSpawner.PauseGame();
            else noteSpawner.ResumeGame();
        }

        if (pauseUI != null)
        {
            if (isPaused)
            {
                // Position Pause UI cleanly in front of the camera right when pausing
                Transform cam = null;
                if (Camera.main != null) cam = Camera.main.transform;
                else
                {
                    OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
                    if (rig != null) cam = rig.centerEyeAnchor;
                }

                if (cam != null)
                {
                    Vector3 forward = cam.forward;
                    forward.y = 0f;
                    forward.Normalize();
                    
                    // Position 3 meters in front, at eye level height
                    Vector3 menuPos = cam.position + forward * 3.0f;
                    menuPos.y = cam.position.y;
                    if (menuPos.y < 0.5f) menuPos.y = 1.35f; // Safe minimum height

                    pauseUI.transform.position = menuPos;
                    pauseUI.transform.rotation = Quaternion.LookRotation(forward);
                    
                    Debug.Log($"[GameManager] Positioned Pause UI in front of camera at: {menuPos}");
                }
                else if (uiManager != null)
                {
                    // Fallback to track/HUD position
                    pauseUI.transform.position = uiManager.transform.position;
                    pauseUI.transform.rotation = uiManager.transform.rotation;
                    Debug.Log("[GameManager] Positioned Pause UI at UIManager fallback position");
                }
            }
            
            pauseUI.gameObject.SetActive(isPaused);
        }

        if (vrInteractor != null)
        {
            // Activate ray pointer during pause, deactivate it when resuming
            vrInteractor.SetPointerActive(isPaused);
        }

        Debug.Log($"[GameManager] Pause Toggled: {isPaused}");
    }

    public void ReturnToSongSelect()
    {
        // Resume time scale first
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);

        ReturnToMainMenu(GameState.SongSelect);
    }

    public void ReturnToMainMenuFromPause()
    {
        // Resume time scale first
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);

        ReturnToMainMenu(GameState.MainMenu);
    }

    public void ReturnToMainMenu(GameState targetState = GameState.MainMenu)
    {
        Time.timeScale = 1f; // Ensure time scale is resumed
        isPaused = false;
        if (pauseUI != null) pauseUI.gameObject.SetActive(false);

        targetStateAfterLoad = targetState;

        if (noteSpawner != null)
        {
            noteSpawner.ResetGame();
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        // Load the MainMenu scene asynchronously with progress bar
        StartCoroutine(LoadSceneAsyncRoutine("MainMenuScene", targetState));
    }

    private System.Collections.IEnumerator LoadSceneAsyncRoutine(string sceneName, GameState postLoadState = GameState.MainMenu)
    {
        targetStateAfterLoad = postLoadState;

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
            
            // Smoothly Lerp the displayed progress bar using unscaledDeltaTime to prevent freezing when Time.timeScale is 0
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 120f);
            
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
