using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIManager : MonoBehaviour
{
    private const float TextWidth = 420f;
    private const float TextHeight = 90f;
    private const float TextFontSize = 42f;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI missText;
    [SerializeField] private TextMeshProUGUI accuracyText;
    [SerializeField] private TextMeshProUGUI comboText;

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
    }

    private TextMeshProUGUI CreateHudText(string objectName, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        return ConfigureHudText(textObject.GetComponent<TextMeshProUGUI>(), objectName, anchoredPosition, alignment);
    }

    private TextMeshProUGUI ConfigureHudText(TextMeshProUGUI text, string objectName, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        text.gameObject.name = objectName;
        text.raycastTarget = false;
        text.color = Color.white;
        text.fontSize = TextFontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(TextWidth, TextHeight);

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
}
