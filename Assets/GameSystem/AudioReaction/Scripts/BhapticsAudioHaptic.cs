using UnityEngine;
using UnityEngine.InputSystem; // Added for flat-screen keyboard shortcut
using Bhaptics.SDK2;

/// <summary>
/// AudioReactionSystems의 주파수 분석 결과를 bHaptics Suit에 매핑합니다.
/// - Vest (상체, 40모터):
///   - 저음역 (FinalBass) -> 앞면 모터 (0~19), 하단 → 상단 그라데이션
///   - 고음역 (FinalTreble) -> 뒷면 모터 (20~39), 상단 → 하단 그라데이션
///   - Onset 감지 시 Burst 패턴 (순간 풀파워)
/// 
/// Vest X40 모터 레이아웃 (앞면/뒷면 각각):
///   행0(상): [0]  [1]  [2]  [3]
///   행1:     [4]  [5]  [6]  [7]
///   행2:     [8]  [9]  [10] [11]
///   행3:     [12] [13] [14] [15]
///   행4(하): [16] [17] [18] [19]
/// 
/// 사용법:
/// 1. 씬에 [bhaptics] 프리팹이 있어야 합니다 (SDK 초기화용).
/// 2. 이 컴포넌트를 AudioReactionSystems와 같은 GameObject에 붙이거나,
///    Inspector에서 audioReaction 필드에 참조를 연결하세요.
/// 3. VR 없이 모니터로 테스트할 때는 에디터에서 플레이 중 'N' 키를 누르면 INFX 노래가 재생됩니다.
/// </summary>
public class BhapticsAudioHaptic : MonoBehaviour
{
    [HideInInspector] public AudioReactionSystems audioReaction;

    [Header("bHaptics 활성화")]
    [Tooltip("bHaptics 햅틱 출력을 활성화/비활성화합니다.")]
    public bool enableBhaptics = true;

    [Header("Vest (상체) — 저음역 매핑 (앞면)")]
    [Tooltip("Vest 모터 강도 배율. 저음이 약하면 올리세요.")]
    [Range(0f, 5.0f)]
    public float vestIntensityMultiplier = 0.05f;

    [Tooltip("이 값 이하의 저음은 무시합니다 (노이즈 컷).")]
    [Range(0f, 0.3f)]
    public float vestThreshold = 0.05f;

    [Tooltip("Vest 진동 지속시간 (ms). 프레임마다 갱신하므로 짧게 설정합니다.")]
    [Range(20, 200)]
    public int vestDurationMs = 50;

    [Header("Vest (상체) — 고음역 매핑 (뒷면)")]
    [Tooltip("등쪽(고음) 모터 강도 배율. 고음이 약하면 올리세요.")]
    [Range(0f, 5.0f)]
    public float forearmIntensityMultiplier = 3.0f; // 변수 이름은 인스펙터 직렬화 호환을 위해 유지

    [Tooltip("이 값 이하의 고음은 무시합니다 (노이즈 컷).")]
    [Range(0f, 0.3f)]
    public float forearmThreshold = 0.05f; // 변수 이름 유지

    [Tooltip("등쪽(고음) 진동 지속시간 (ms).")]
    [Range(20, 200)]
    public int forearmDurationMs = 40; // 변수 이름 유지

    [Header("공간 그라데이션")]
    [Tooltip("그라데이션 최소 강도 비율. 0.3 = 가장 먼 행은 30% 강도.")]
    [Range(0.0f, 1.0f)]
    public float gradientMinRatio = 0.3f;

    [Header("Onset Burst (펀치감)")]
    [Tooltip("Onset 감지 시 Burst 패턴을 활성화합니다.")]
    public bool enableOnsetBurst = true;

    [Tooltip("Onset 시 추가되는 진동 지속시간 (ms).")]
    [Range(0, 100)]
    public int onsetDurationBoostMs = 40;

    [Tooltip("Onset Burst 시 강도 배율. 1.0 = 추가 없음, 2.0 = 2배.")]
    [Range(1.0f, 3.0f)]
    public float onsetIntensityMultiplier = 1.5f;

    [Header("업데이트 주기")]
    [Tooltip("햅틱 전송 간격 (초). 너무 짧으면 bHaptics 부하, 너무 길면 반응 느림.")]
    [Range(0.016f, 0.1f)]
    public float updateInterval = 0.03f; // ~33Hz

    // ── 내부 상태 ──
    private float lastUpdateTime = 0f;

    // Vest: 앞면 20 + 뒷면 20 = 40 모터 (TactSuit X40 스펙 반영)
    private int[] vestMotors = new int[40];

    // 행별 그라데이션 가중치 (5행: 행0=상단, 행4=하단)
    // Bass: 하단(행4)에서 강하고 상단(행0)으로 감쇠
    // Treble: 상단(행0)에서 강하고 하단(행4)으로 감쇠
    private float[] bassRowWeights = new float[5];
    private float[] trebleRowWeights = new float[5];

    void Start()
    {
        // audioReaction 참조 자동 검색
        if (audioReaction == null)
        {
            audioReaction = GetComponent<AudioReactionSystems>();
        }
        if (audioReaction == null)
        {
            audioReaction = FindObjectOfType<AudioReactionSystems>();
        }

        if (audioReaction == null)
        {
            Debug.LogError("[BhapticsAudioHaptic] AudioReactionSystems를 찾을 수 없습니다. Inspector에서 연결해 주세요.");
            enabled = false;
            return;
        }

        // 행별 그라데이션 가중치 사전 계산
        RecalculateGradientWeights();

        Debug.Log("[BhapticsAudioHaptic] 초기화 완료. bHaptics Suit 햅틱을 시작합니다. (그라데이션 + Onset Burst 활성화)");
    }

    /// <summary>
    /// gradientMinRatio가 변경될 때 호출하여 가중치를 재계산합니다.
    /// </summary>
    private void RecalculateGradientWeights()
    {
        // Bass: 하단(행4)=1.0, 상단(행0)=gradientMinRatio 으로 선형 보간
        // Treble: 상단(행0)=1.0, 하단(행4)=gradientMinRatio 으로 선형 보간
        for (int row = 0; row < 5; row++)
        {
            float t = row / 4.0f; // 0.0(행0=상단) ~ 1.0(행4=하단)

            // Bass: 하단이 강함 → t가 클수록(하단일수록) 가중치 높음
            bassRowWeights[row] = Mathf.Lerp(gradientMinRatio, 1.0f, t);

            // Treble: 상단이 강함 → t가 작을수록(상단일수록) 가중치 높음
            trebleRowWeights[row] = Mathf.Lerp(1.0f, gradientMinRatio, t);
        }
    }

    void Update()
    {
        if (!enableBhaptics) return;
        if (audioReaction == null) return;
        if (!BhapticsSDK2.IsInitialized) return;

        // 업데이트 주기 제한
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        float bass = audioReaction.FinalBass;
        float treble = audioReaction.FinalTreble;
        bool bassOnset = audioReaction.BassOnset;
        bool trebleOnset = audioReaction.TrebleOnset;

        // vestMotors 배열 초기화 (0으로 리셋)
        for (int i = 0; i < vestMotors.Length; i++)
        {
            vestMotors[i] = 0;
        }

        bool hasHaptic = false;

        // ── 앞면 (0 ~ 19): 저음역대 (Bass) — 그라데이션 매핑 ──
        if (bass > vestThreshold)
        {
            float intensityBase = bass * vestIntensityMultiplier * 100f;

            // Onset Burst: 비트 어택 시 강도 증폭
            if (enableOnsetBurst && bassOnset)
            {
                intensityBase *= onsetIntensityMultiplier;
            }

            for (int row = 0; row < 5; row++)
            {
                int motorIntensity = Mathf.Clamp((int)(intensityBase * bassRowWeights[row]), 1, 100);
                int startIdx = row * 4; // 각 행은 4개 모터
                for (int col = 0; col < 4; col++)
                {
                    vestMotors[startIdx + col] = motorIntensity;
                }
            }
            hasHaptic = true;
        }

        // ── 뒷면 (20 ~ 39): 고음역대 (Treble) — 그라데이션 매핑 ──
        if (treble > forearmThreshold)
        {
            float intensityBase = treble * forearmIntensityMultiplier * 100f;

            // Onset Burst: 비트 어택 시 강도 증폭
            if (enableOnsetBurst && trebleOnset)
            {
                intensityBase *= onsetIntensityMultiplier;
            }

            for (int row = 0; row < 5; row++)
            {
                int motorIntensity = Mathf.Clamp((int)(intensityBase * trebleRowWeights[row]), 1, 100);
                int startIdx = 20 + (row * 4); // 뒷면은 인덱스 20부터
                for (int col = 0; col < 4; col++)
                {
                    vestMotors[startIdx + col] = motorIntensity;
                }
            }
            hasHaptic = true;
        }

        if (hasHaptic)
        {
            // 진동 지속시간 동적 조절: Onset 시 더 긴 지속시간으로 "울림" 표현
            int baseDuration = Mathf.Max(vestDurationMs, forearmDurationMs);
            bool anyOnset = bassOnset || trebleOnset;
            int dynamicDuration = (enableOnsetBurst && anyOnset)
                ? baseDuration + onsetDurationBoostMs
                : baseDuration;

            BhapticsLibrary.PlayMotors(
                (int)PositionType.Vest,
                vestMotors,
                dynamicDuration
            );
        }
    }



    void OnDisable()
    {
        // 컴포넌트 비활성화 시 모든 bHaptics 햅틱 정지
        if (BhapticsSDK2.IsInitialized)
        {
            BhapticsLibrary.StopAll();
        }
    }
}

