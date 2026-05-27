using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIManager : MonoBehaviour
{
    private const float TextWidth = 420f;
    private const float TextHeight = 90f;
    private const float TextFontSize = 42f;
    private const float JudgementFontSize = 120f; // 판정 텍스트 크기 대폭 확대

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI missText;
    [SerializeField] private TextMeshProUGUI accuracyText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI judgementText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("HUD Placement")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private Vector3 uiOffset = new Vector3(0f, 0f, 4f);
    [SerializeField] private float hudHorizontalOffset = 600f;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        HeightCalibrator.OnCalibrationUpdated += HandleHeightCalibrationUpdated;
        HeightCalibrator.OnCalibrationCompleted += HandleHeightCalibrationUpdated;
    }

    private void Start()
    {
        ResolveCamera();
        EnsureHudTextElements();
        
        HeightCalibrator calibrator = FindAnyObjectByType<HeightCalibrator>();
        RepositionNow(calibrator);
        
        SubscribeToScoreManager();
        RefreshFromScoreManager();
    }

    private void OnDisable()
    {
        HeightCalibrator.OnCalibrationUpdated -= HandleHeightCalibrationUpdated;
        HeightCalibrator.OnCalibrationCompleted -= HandleHeightCalibrationUpdated;
    }

    private void OnDestroy()
    {
        UnsubscribeFromScoreManager();
    }

    private void HandleHeightCalibrationUpdated(HeightCalibrator calibrator)
    {
        RepositionNow(calibrator);
    }

    private void ResolveCamera()
    {
        if (xrCamera != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            xrCamera = Camera.main.transform;
            return;
        }

        OVRCameraRig ovrCameraRig = FindAnyObjectByType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            xrCamera = ovrCameraRig.centerEyeAnchor;
        }
    }

    private Coroutine judgementCoroutine;

    private void EnsureHudTextElements()
    {
        TextMeshProUGUI[] existingTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (scoreText == null && existingTexts.Length > 0)
        {
            scoreText = existingTexts[0];
        }

        scoreText = scoreText != null
            ? ConfigureHudText(scoreText, "ScoreText", new Vector2(-hudHorizontalOffset, 80f), TextAlignmentOptions.Left)
            : CreateHudText("ScoreText", new Vector2(-hudHorizontalOffset, 80f), TextAlignmentOptions.Left);

        missText = missText != null
            ? ConfigureHudText(missText, "MissText", new Vector2(-hudHorizontalOffset, -15f), TextAlignmentOptions.Left)
            : CreateHudText("MissText", new Vector2(-hudHorizontalOffset, -15f), TextAlignmentOptions.Left);

        accuracyText = accuracyText != null
            ? ConfigureHudText(accuracyText, "AccuracyText", new Vector2(hudHorizontalOffset, 40f), TextAlignmentOptions.Right)
            : CreateHudText("AccuracyText", new Vector2(hudHorizontalOffset, 40f), TextAlignmentOptions.Right);

        // HUD Canvas 최상단 정중앙 상향 및 크기 적용 (Y=350f)
        judgementText = judgementText != null
            ? ConfigureHudText(judgementText, "JudgementText", new Vector2(0f, 500f), TextAlignmentOptions.Center, true)
            : CreateHudText("JudgementText", new Vector2(0f, 500f), TextAlignmentOptions.Center, true);
            
        // 초기에는 판정이 없을 것이므로 비활성화 상태로 둡니다.
        if (judgementText != null)
        {
            judgementText.gameObject.SetActive(false);
        }

        // 카운트다운 텍스트 초기화 (중앙 메인 트랙라인을 피해 화면 상단에 큼직하게 배치)
        countdownText = countdownText != null
            ? ConfigureHudText(countdownText, "CountdownText", new Vector2(0f, 350f), TextAlignmentOptions.Center, true)
            : CreateHudText("CountdownText", new Vector2(0f, 350f), TextAlignmentOptions.Center, true);

        if (countdownText != null)
        {
            countdownText.fontSize = 200f; // 큼직하게 200폰트 적용
            countdownText.gameObject.SetActive(false);
        }
    }

    private TextMeshProUGUI CreateHudText(string objectName, Vector2 anchoredPosition, TextAlignmentOptions alignment, bool isJudgement = false)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        return ConfigureHudText(textObject.GetComponent<TextMeshProUGUI>(), objectName, anchoredPosition, alignment, isJudgement);
    }

    private TextMeshProUGUI ConfigureHudText(TextMeshProUGUI text, string objectName, Vector2 anchoredPosition, TextAlignmentOptions alignment, bool isJudgement = false)
    {
        text.gameObject.name = objectName;
        text.raycastTarget = false;
        text.color = Color.white;
        text.enableAutoSizing = false; // AutoSizing을 끄고 강제로 fontSize를 먹이도록 설정
        text.fontSize = isJudgement ? JudgementFontSize : TextFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        if (scoreText != null)
        {
            text.font = scoreText.font;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = isJudgement ? new Vector2(800f, 300f) : new Vector2(TextWidth, TextHeight);

        return text;
    }

    public void RepositionNow()
    {
        RepositionNow(null);
    }

    private void RepositionNow(HeightCalibrator calibrator)
    {
        if (xrCamera == null)
        {
            ResolveCamera();
        }

        if (xrCamera == null)
        {
            Debug.LogWarning("UIManager: Could not find a VR camera, so the HUD was not positioned.");
            return;
        }

        PositionUI(calibrator);
    }

    private void PositionUI(HeightCalibrator calibrator)
    {
        Vector3 forward;
        Vector3 right;
        Vector3 basePosition;

        // [수직 칼정렬 및 평행 트랙 락]
        // 카메라의 방향이나 흔들림에 따라 UI가 좌우로 이탈하는 현상을 완벽히 차단합니다.
        // calibrator가 있고 targetTrack이 존재하면, 트랙 자체의 forward와 right 벡터를 기반으로 위치를 산출합니다.
        if (calibrator != null && calibrator.targetTrack != null)
        {
            forward = calibrator.targetTrack.forward;
            forward.y = 0f;
            forward.Normalize();

            right = calibrator.targetTrack.right;
            right.y = 0f;
            right.Normalize();

            // X, Y는 트랙의 중심선 값을 완벽하게 고정합니다.
            basePosition = calibrator.targetTrack.position;
            basePosition.y = calibrator.targetTrack.position.y;

            // Z축 상에서의 깊이 위치는 카메라의 위치를 트랙의 정렬축에 투영(Projection)하여
            // 플레이어가 서 있는 상대적인 거리만을 정확히 차용합니다.
            Vector3 toCamera = xrCamera.position - calibrator.targetTrack.position;
            float zProjection = Vector3.Dot(toCamera, forward);
            basePosition += forward * zProjection;
        }
        else
        {
            forward = xrCamera.forward;
            forward.y = 0f;
            forward.Normalize();

            right = xrCamera.right;
            right.y = 0f;
            right.Normalize();

            basePosition = xrCamera.position;
            basePosition.x = 0f; // 기본 트랙 X = 0f 고정
        }

        transform.position = basePosition + (right * uiOffset.x) + (Vector3.up * uiOffset.y) + (forward * uiOffset.z);

        // [원근 정렬 락]
        // 카메라를 삐딱하게 바라보도록 캔버스가 회전되면 3D 공간 상에서 원근감 때문에 정렬이 어긋나 보입니다.
        // 이를 방지하기 위해 캔버스의 회전각을 트랙 라인의 회전각과 완벽히 평행하도록 강제 잠금(Lock)합니다.
        if (calibrator != null && calibrator.targetTrack != null)
        {
            transform.rotation = calibrator.targetTrack.rotation;
        }
        else
        {
            Vector3 lookDirection = transform.position - xrCamera.position;
            lookDirection.y = 0f;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    private void SubscribeToScoreManager()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("UIManager: ScoreManager.Instance was not found, so score HUD events were not connected.");
            return;
        }

        ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
        ScoreManager.Instance.OnComboChanged += UpdateComboText;
        ScoreManager.Instance.OnMissChanged += UpdateMissText;
        ScoreManager.Instance.OnAccuracyChanged += UpdateAccuracyText;
        ScoreManager.Instance.OnJudgementChanged += UpdateJudgementText;
    }

    private void UnsubscribeFromScoreManager()
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
        ScoreManager.Instance.OnComboChanged -= UpdateComboText;
        ScoreManager.Instance.OnMissChanged -= UpdateMissText;
        ScoreManager.Instance.OnAccuracyChanged -= UpdateAccuracyText;
        ScoreManager.Instance.OnJudgementChanged -= UpdateJudgementText;
    }

    private void RefreshFromScoreManager()
    {
        if (ScoreManager.Instance == null)
        {
            UpdateScoreText(0);
            UpdateComboText(0);
            UpdateMissText(0);
            UpdateAccuracyText(100f);
            return;
        }

        ScoreManager.Instance.BroadcastStats();
    }

    private void UpdateScoreText(int newScore)
    {
        scoreText.text = $"SCORE\n{newScore:N0}";
    }

    private void UpdateComboText(int newCombo)
    {
        if (comboText != null)
        {
            comboText.text = $"COMBO\n{newCombo}";
        }
    }

    private void UpdateMissText(int newMissCount)
    {
        missText.text = $"MISS\n{newMissCount}";
    }

    private void UpdateAccuracyText(float accuracy)
    {
        accuracyText.text = $"ACCURACY\n{accuracy:0.0}%";
    }

    private struct JudgementInfo
    {
        public string text;
        public Color color;
    }

    private System.Collections.Generic.Queue<JudgementInfo> judgementQueue = new System.Collections.Generic.Queue<JudgementInfo>();
    private bool isJudgementRunning = false;
    private float lastJudgementTime = 0f;
    private const float MinJudgementDisplayTime = 0.08f; // 최소 표시 시간 (80ms)

    private void UpdateJudgementText(string text, Color color)
    {
        if (judgementText == null) return;

        // 판정이 들어오면 무조건 큐에 삽입
        judgementQueue.Enqueue(new JudgementInfo { text = text, color = color });

        // 이미 판정이 처리 중이 아니라면 큐 처리 코루틴 시작
        if (!isJudgementRunning)
        {
            StartCoroutine(ProcessJudgementQueue());
        }
    }

    private System.Collections.IEnumerator ProcessJudgementQueue()
    {
        isJudgementRunning = true;

        while (judgementQueue.Count > 0)
        {
            // 이전 판정이 방금 표시되었다면, 최소 표시 시간(80ms)을 지키기 위해 대기
            float timeSinceLast = Time.time - lastJudgementTime;
            if (timeSinceLast < MinJudgementDisplayTime && timeSinceLast > 0f)
            {
                yield return new WaitForSeconds(MinJudgementDisplayTime - timeSinceLast);
            }

            JudgementInfo current = judgementQueue.Dequeue();
            
            judgementText.text = current.text;
            judgementText.color = current.color;
            judgementText.gameObject.SetActive(true);
            lastJudgementTime = Time.time;

            RectTransform rect = judgementText.rectTransform;
            
            // 대기 중인 판정이 더 있다면 빠르게 다음 판정으로 넘어가기 위해 0.08초만 대기
            float duration = judgementQueue.Count > 0 ? MinJudgementDisplayTime : 0.45f;
            float elapsed = 0f;

            // 판정이 뜨는 순간 즉시 1.4배의 Scale Pop 크기로 강제 초기화하여 타격감 극대화
            rect.localScale = new Vector3(1.4f, 1.4f, 1f);

            while (elapsed < duration)
            {
                // 대기 중인 판정이 추가되었고, 현재 판정이 최소 표시 시간을 넘어섰다면 즉시 루프 중단
                if (judgementQueue.Count > 0 && elapsed >= MinJudgementDisplayTime)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale Pop: 1.4배에서 1.0배로 튕기듯이 작아지는 연출
                float scale = Mathf.Lerp(1.4f, 1.0f, t);
                rect.localScale = new Vector3(scale, scale, 1f);

                // 페이드 아웃 (대기 중인 판정이 없을 때만 서서히 사라짐)
                if (judgementQueue.Count == 0 && t > 0.5f)
                {
                    float fadeT = (t - 0.5f) / 0.5f;
                    Color c = current.color;
                    c.a = Mathf.Lerp(1f, 0f, fadeT);
                    judgementText.color = c;
                }

                yield return null;
            }
        }

        judgementText.gameObject.SetActive(false);
        isJudgementRunning = false;
    }

    public void SetHudVisible(bool visible)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(visible);
        if (missText != null) missText.gameObject.SetActive(visible);
        if (accuracyText != null) accuracyText.gameObject.SetActive(visible);
        if (comboText != null) comboText.gameObject.SetActive(visible);
        if (judgementText != null) judgementText.gameObject.SetActive(visible && isJudgementRunning);
        if (countdownText != null) countdownText.gameObject.SetActive(visible);
    }

    public void ShowPressToStartPrompt()
    {
        if (countdownText != null)
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
            countdownText.text = "PRESS A OR ENTER TO START";
            countdownText.fontSize = 65f;
            countdownText.color = Color.white;
            countdownText.gameObject.SetActive(true);
            countdownText.rectTransform.localScale = Vector3.one;
            
            // Adjust alpha to full opacity
            Color c = countdownText.color;
            c.a = 1f;
            countdownText.color = c;
        }
    }

    // ─── 카운트다운 시스템 ───
    private Coroutine countdownCoroutine;

    public void StartCountdown(float duration)
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownCoroutine(duration));
    }

    private System.Collections.IEnumerator CountdownCoroutine(float duration)
    {
        if (countdownText == null) yield break;

        countdownText.gameObject.SetActive(true);
        RectTransform rect = countdownText.rectTransform;

        // [핵심: 랙 스파이크 방지]
        // 시작 키 입력 직후 프레임 드랍이나 렉 때문에 '3'이 순식간에 날아가는 것을 방지합니다.
        // 먼저 첫 프레임에 '3'을 확실히 그려두고 루프에 들어가기 전 1프레임을 안전하게 흘려보냅니다.
        countdownText.text = "3";
        countdownText.color = Color.white; // 흰색으로 통일
        rect.localScale = new Vector3(1.5f, 1.5f, 1f);
        yield return null;

        float elapsed = 0f;
        int lastSeconds = 3; // 첫 3은 이미 그렸으므로 3으로 상태 초기화

        while (elapsed < duration)
        {
            // [핵심: 랙 스파이크 방지 2]
            // 혹시라도 루프 도중 프레임 튀어 대량의 시간이 지나가도 숫자가 건너뛰어지지 않도록,
            // 매 프레임당 경과할 수 있는 최대 시간을 0.05초로 락(Clamp)합니다.
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            elapsed += dt;

            float remaining = duration - elapsed;
            
            // remaining 범위에 따른 직관적이고 정확한 3, 2, 1 텍스트 매핑
            string desiredText = "";
            Color desiredColor = Color.white; // 흰색으로 통일

            if (remaining > 2.0f)
            {
                desiredText = "3";
            }
            else if (remaining > 1.0f)
            {
                desiredText = "2";
            }
            else if (remaining > 0.0f)
            {
                desiredText = "1";
            }

            int currentSecInt = Mathf.CeilToInt(remaining);
            if (currentSecInt != lastSeconds && !string.IsNullOrEmpty(desiredText))
            {
                lastSeconds = currentSecInt;
                countdownText.text = desiredText;
                countdownText.color = desiredColor;
                
                // 새로운 숫자가 등장할 때 크기 팝업 연출 초기화
                rect.localScale = new Vector3(1.5f, 1.5f, 1f);
            }

            // 매 초마다 1.5배에서 1.0배로 튕기듯이 수축
            float t = elapsed % 1f;
            float scale = Mathf.Lerp(1.5f, 1.0f, t);
            rect.localScale = new Vector3(scale, scale, 1f);

            // 가독성을 극대화하기 위해 매 초 부드럽게 불투명도를 감쇄
            Color c = countdownText.color;
            c.a = Mathf.Lerp(1f, 0.1f, t);
            countdownText.color = c;

            yield return null;
        }

        // 3초 카운트가 끝나는 음악 재생 타이밍에 화려하게 START! 등장
        countdownText.text = "START!";
        countdownText.color = Color.white; // 흰색으로 통일
        rect.localScale = new Vector3(1.6f, 1.6f, 1f); // START!는 1.6배로 더욱 크게 팝업!

        // START! 가 번쩍이고 자연스럽게 스케일 아웃 및 투명 페이드아웃 연출
        float startDuration = 0.5f;
        float startElapsed = 0f;
        while (startElapsed < startDuration)
        {
            startElapsed += Time.deltaTime;
            float t = startElapsed / startDuration;
            
            // 1.6배에서 1.0배로 축소
            float scale = Mathf.Lerp(1.6f, 1.0f, t);
            rect.localScale = new Vector3(scale, scale, 1f);

            // 0.5초 동안 서서히 투명화
            Color c = countdownText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            countdownText.color = c;

            yield return null;
        }

        countdownText.gameObject.SetActive(false);
        countdownCoroutine = null;
    }
}
