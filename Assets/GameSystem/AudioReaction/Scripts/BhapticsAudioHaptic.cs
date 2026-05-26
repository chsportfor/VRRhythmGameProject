using UnityEngine;
using Bhaptics.SDK2;

/// <summary>
/// AudioReactionSystems의 주파수 분석 결과를 bHaptics Suit에 매핑합니다.
/// - Vest (상체, 32모터): 저음역 (FinalBass)
/// - ForearmL / ForearmR (팔, 각 3모터): 고음역 (FinalTreble)
/// 
/// 사용법:
/// 1. 씬에 [bhaptics] 프리팹이 있어야 합니다 (SDK 초기화용).
/// 2. 이 컴포넌트를 AudioReactionSystems와 같은 GameObject에 붙이거나,
///    Inspector에서 audioReaction 필드에 참조를 연결하세요.
/// </summary>
public class BhapticsAudioHaptic : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("AudioReactionSystems 컴포넌트 참조. 비어있으면 같은 GameObject에서 자동 검색합니다.")]
    public AudioReactionSystems audioReaction;

    [Header("bHaptics 활성화")]
    [Tooltip("bHaptics 햅틱 출력을 활성화/비활성화합니다.")]
    public bool enableBhaptics = true;

    [Header("Vest (상체) — 저음역 매핑")]
    [Tooltip("Vest 모터 강도 배율. 저음이 약하면 올리세요.")]
    [Range(0.1f, 3.0f)]
    public float vestIntensityMultiplier = 1.5f;

    [Tooltip("이 값 이하의 저음은 무시합니다 (노이즈 컷).")]
    [Range(0f, 0.3f)]
    public float vestThreshold = 0.05f;

    [Tooltip("Vest 진동 지속시간 (ms). 프레임마다 갱신하므로 짧게 설정합니다.")]
    [Range(20, 200)]
    public int vestDurationMs = 50;

    [Header("Forearm (팔) — 고음역 매핑")]
    [Tooltip("Forearm 모터 강도 배율. 고음이 약하면 올리세요.")]
    [Range(0.1f, 3.0f)]
    public float forearmIntensityMultiplier = 2.0f;

    [Tooltip("이 값 이하의 고음은 무시합니다 (노이즈 컷).")]
    [Range(0f, 0.3f)]
    public float forearmThreshold = 0.05f;

    [Tooltip("Forearm 진동 지속시간 (ms).")]
    [Range(20, 200)]
    public int forearmDurationMs = 40;

    [Header("업데이트 주기")]
    [Tooltip("햅틱 전송 간격 (초). 너무 짧으면 bHaptics 부하, 너무 길면 반응 느림.")]
    [Range(0.016f, 0.1f)]
    public float updateInterval = 0.03f; // ~33Hz

    // ── 내부 상태 ──
    private float lastUpdateTime = 0f;

    // Vest: 앞면 16 + 뒷면 16 = 32 모터
    private int[] vestMotors = new int[32];

    // Forearm: 각 3 모터
    private int[] forearmLMotors = new int[3];
    private int[] forearmRMotors = new int[3];

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

        Debug.Log("[BhapticsAudioHaptic] 초기화 완료. bHaptics Suit 햅틱을 시작합니다.");
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

        // ── Vest (상체): 저음역 ──
        if (bass > vestThreshold)
        {
            int intensity = Mathf.Clamp((int)(bass * vestIntensityMultiplier * 100f), 1, 100);
            ApplyVestHaptic(intensity);
        }

        // ── Forearm (양팔): 고음역 ──
        if (treble > forearmThreshold)
        {
            int intensity = Mathf.Clamp((int)(treble * forearmIntensityMultiplier * 100f), 1, 100);
            ApplyForearmHaptic(intensity);
        }
    }

    /// <summary>
    /// Vest 전체에 균일한 저음 진동을 적용합니다.
    /// 추후 모터별 패턴 분배로 확장 가능합니다.
    /// </summary>
    private void ApplyVestHaptic(int intensity)
    {
        // 전체 32모터에 동일 강도 적용 (기본 전략)
        // 추후 상/하반신 분리, 파동 패턴 등으로 고도화 가능
        for (int i = 0; i < vestMotors.Length; i++)
        {
            vestMotors[i] = intensity;
        }

        BhapticsLibrary.PlayMotors(
            (int)PositionType.Vest,
            vestMotors,
            vestDurationMs
        );
    }

    /// <summary>
    /// 양쪽 팔에 고음 진동을 적용합니다.
    /// </summary>
    private void ApplyForearmHaptic(int intensity)
    {
        for (int i = 0; i < forearmLMotors.Length; i++)
        {
            forearmLMotors[i] = intensity;
            forearmRMotors[i] = intensity;
        }

        BhapticsLibrary.PlayMotors(
            (int)PositionType.ForearmL,
            forearmLMotors,
            forearmDurationMs
        );

        BhapticsLibrary.PlayMotors(
            (int)PositionType.ForearmR,
            forearmRMotors,
            forearmDurationMs
        );
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
