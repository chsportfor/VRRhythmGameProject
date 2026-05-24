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

    [Header("HUD Placement")]
    [SerializeField] private Transform xrCamera;
    [SerializeField] private Vector3 uiOffset = new Vector3(0f, 0f, 4f);
    [SerializeField] private float hudHorizontalOffset = 600f;

    private void OnEnable()
    {
        HeightCalibrator.OnCalibrationUpdated += HandleHeightCalibrationUpdated;
        HeightCalibrator.OnCalibrationCompleted += HandleHeightCalibrationUpdated;
    }

    private void Start()
    {
        ResolveCamera();
        EnsureHudTextElements();
        RepositionNow();
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
        Vector3 forward = xrCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = xrCamera.right;
        right.y = 0f;
        right.Normalize();

        Vector3 basePosition = xrCamera.position;
        if (calibrator != null && calibrator.targetTrack != null)
        {
            basePosition.y = calibrator.targetTrack.position.y;
        }

        transform.position = basePosition + (right * uiOffset.x) + (Vector3.up * uiOffset.y) + (forward * uiOffset.z);

        Vector3 lookDirection = transform.position - xrCamera.position;
        lookDirection.y = 0f;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
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
}
