using UnityEngine;
using UnityEngine.InputSystem;

public class AudioReactionSystems : MonoBehaviour
{
    [Header("Audio Source (Debug/Runtime)")]
    public AudioSource audioSource;
    private float[] spectrum = new float[256];

    // ── 기본 감도 설정 ──
    [Header("기본 감도")]
    public float bassSensitivity = 10.0f;
    public float trebleSensitivity = 25.0f;
    public float depthContrast = 2.5f;
    public float deltaMultiplier = 2.0f;

    // ── EMA 스무딩 (노이즈 제거) ──
    [Header("EMA 스무딩")]
    [Tooltip("낮을수록 부드러움(느린 반응), 높을수록 민감(빠른 반응)")]
    [Range(0.05f, 0.5f)]
    public float smoothFactor = 0.2f;

    // ── Onset Detection (비트 펀치감) ──
    [Header("Onset Detection (펀치감)")]
    [Tooltip("활성화 시 비트 어택 순간을 감지하여 펀치감을 추가합니다.")]
    public bool enableOnsetDetection = true;

    [Tooltip("장기평균 대비 급등 비율 임계값. 낮을수록 민감.")]
    [Range(1.2f, 5.0f)]
    public float onsetThreshold = 2.0f;

    [Tooltip("Onset 감지 시 추가되는 강도 부스트.")]
    [Range(0.1f, 1.0f)]
    public float onsetBoostAmount = 0.4f;

    [Tooltip("장기 이동평균의 추적 속도. 낮을수록 더 긴 히스토리를 반영.")]
    [Range(0.01f, 0.1f)]
    public float longAvgFactor = 0.05f;

    // ── 지수 감쇠 (잔향/여운) ──
    [Header("지수 감쇠 (잔향)")]
    [Tooltip("활성화 시 진동이 부드럽게 감쇠하며 잔향감을 줍니다.")]
    public bool enableDecay = true;

    [Tooltip("높을수록 잔향이 길어짐. 0.85=짧은 여운, 0.98=긴 여운.")]
    [Range(0.8f, 0.99f)]
    public float decayRate = 0.92f;

    // ── 내부 상태 ──
    private float lastBassPeak = 0f;
    private float lastTreblePeak = 0f;

    // EMA 스무딩 상태
    private float smoothedBass = 0f;
    private float smoothedTreble = 0f;

    // Onset Detection 상태 (장기 이동평균)
    private float bassLongAvg = 0f;
    private float trebleLongAvg = 0f;

    // 지수 감쇠 상태
    private float sustainedBass = 0f;
    private float sustainedTreble = 0f;

    public OVRInput.Controller controllerMask = OVRInput.Controller.RTouch | OVRInput.Controller.LTouch;

    // ══════════════════════════════════════════════
    //  외부 접근용 프로퍼티 (bHaptics 등에서 사용)
    // ══════════════════════════════════════════════

    /// <summary>저음역 최종 강도 (0~1). 감쇠 포함. Vest/상체 햅틱에 사용.</summary>
    public float FinalBass { get; private set; }

    /// <summary>고음역 최종 강도 (0~1). 감쇠 포함. Forearm/팔 햅틱에 사용.</summary>
    public float FinalTreble { get; private set; }

    /// <summary>저음역 원시 피크 값 (스펙트럼 가중 최대값).</summary>
    public float RawBassPeak { get; private set; }

    /// <summary>고음역 원시 피크 값 (스펙트럼 가중 최대값).</summary>
    public float RawTreblePeak { get; private set; }

    /// <summary>이번 프레임에 Bass Onset(비트 어택)이 발생했는지 여부.</summary>
    public bool BassOnset { get; private set; }

    /// <summary>이번 프레임에 Treble Onset(비트 어택)이 발생했는지 여부.</summary>
    public bool TrebleOnset { get; private set; }

    /// <summary>이번 프레임의 Bass 변화량 (스무딩 후 delta).</summary>
    public float BassDelta { get; private set; }

    /// <summary>이번 프레임의 Treble 변화량 (스무딩 후 delta).</summary>
    public float TrebleDelta { get; private set; }

    void Update()
    {
        if (audioSource == null) return;

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // ────────────────────────────────────────
        // 1단계: 주파수 대역 추출 (확장된 범위 + 가중치)
        // ────────────────────────────────────────
        float bassPeak = 0f;
        for (int i = 0; i <= 7; i++)
        {
            // 낮은 빈일수록 가중치를 높여 서브베이스 강조
            float weight = 1.0f - (i / 10.0f);
            float weighted = spectrum[i] * weight;
            if (weighted > bassPeak) bassPeak = weighted;
        }

        float treblePeak = 0f;
        for (int i = 20; i <= 60; i++)
        {
            if (spectrum[i] > treblePeak) treblePeak = spectrum[i];
        }

        // ────────────────────────────────────────
        // 2단계: EMA 스무딩 (노이즈 제거)
        // ────────────────────────────────────────
        smoothedBass = Mathf.Lerp(smoothedBass, bassPeak, smoothFactor);
        smoothedTreble = Mathf.Lerp(smoothedTreble, treblePeak, smoothFactor);

        // ────────────────────────────────────────
        // 3단계: Delta 계산 (스무딩된 값 기반)
        // ────────────────────────────────────────
        float bassDelta = smoothedBass - lastBassPeak;
        float trebleDelta = smoothedTreble - lastTreblePeak;
        BassDelta = bassDelta;
        TrebleDelta = trebleDelta;

        // ────────────────────────────────────────
        // 4단계: Onset Detection (비트 어택 감지)
        // ────────────────────────────────────────
        BassOnset = false;
        TrebleOnset = false;

        if (enableOnsetDetection)
        {
            // 장기 이동평균 갱신 (느린 추적)
            bassLongAvg = Mathf.Lerp(bassLongAvg, bassPeak, longAvgFactor);
            trebleLongAvg = Mathf.Lerp(trebleLongAvg, treblePeak, longAvgFactor);

            // 장기평균 대비 현재 스무딩값의 비율로 Onset 판정
            float bassOnsetRatio = (bassLongAvg > 0.001f) ? (smoothedBass / bassLongAvg) : 0f;
            float trebleOnsetRatio = (trebleLongAvg > 0.001f) ? (smoothedTreble / trebleLongAvg) : 0f;

            BassOnset = bassOnsetRatio > onsetThreshold;
            TrebleOnset = trebleOnsetRatio > onsetThreshold;
        }

        // ────────────────────────────────────────
        // 5단계: 감도/컨트라스트 적용
        // ────────────────────────────────────────
        float targetBassAmplitude = Mathf.Clamp01(smoothedBass * bassSensitivity);
        float targetTrebleAmplitude = Mathf.Clamp01(smoothedTreble * trebleSensitivity);

        float finalBass = Mathf.Pow(targetBassAmplitude, depthContrast);
        float finalTreble = Mathf.Pow(targetTrebleAmplitude, depthContrast);

        // ────────────────────────────────────────
        // 6단계: Delta 부스트 (양의 변화량 보상)
        // ────────────────────────────────────────
        if (bassDelta > 0)
        {
            finalBass += (bassDelta * bassSensitivity * deltaMultiplier);
        }
        if (trebleDelta > 0)
        {
            finalTreble += (trebleDelta * trebleSensitivity * deltaMultiplier);
        }

        // ────────────────────────────────────────
        // 7단계: Onset 부스트 (비트 펀치감)
        // ────────────────────────────────────────
        if (BassOnset)
        {
            finalBass += onsetBoostAmount;
        }
        if (TrebleOnset)
        {
            finalTreble += onsetBoostAmount;
        }

        finalBass = Mathf.Clamp01(finalBass);
        finalTreble = Mathf.Clamp01(finalTreble);

        // ────────────────────────────────────────
        // 8단계: 지수 감쇠 (잔향/여운)
        // 새 값이 이전보다 크면 즉시 채택, 아니면 감쇠
        // ────────────────────────────────────────
        if (enableDecay)
        {
            sustainedBass = Mathf.Max(finalBass, sustainedBass * decayRate);
            sustainedTreble = Mathf.Max(finalTreble, sustainedTreble * decayRate);
            finalBass = sustainedBass;
            finalTreble = sustainedTreble;
        }

        // ────────────────────────────────────────
        // 최종 출력
        // ────────────────────────────────────────
        FinalBass = finalBass;
        FinalTreble = finalTreble;
        RawBassPeak = bassPeak;
        RawTreblePeak = treblePeak;

        // Periodic logging to debug vibration issues (once every 60 frames)
        if (Time.frameCount % 60 == 0)
        {
            float spectrumSum = 0f;
            for (int i = 0; i < spectrum.Length; i++) spectrumSum += spectrum[i];
            
            Debug.Log($"[AudioReactionSystems Debug] Source: {(audioSource != null ? audioSource.name : "NULL")}, " +
                      $"Playing: {(audioSource != null ? audioSource.isPlaying : false)}, " +
                      $"SpectrumSum: {spectrumSum:F6}, " +
                      $"FinalBass: {finalBass:F4}, FinalTreble: {finalTreble:F4}");
        }

        ApplyOVRHaptics(finalBass, finalTreble);

        lastBassPeak = smoothedBass;
        lastTreblePeak = smoothedTreble;
    }

    private void ApplyOVRHaptics(float bass, float treble)
    {
        // Revert frequency back to 0f as requested by device runtime compatibility (some OVR environments ignore 1f)
        OVRInput.SetControllerVibration(0f, bass, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0f, treble, OVRInput.Controller.LTouch);
    }

    void OnDisable()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.All);
    }
}