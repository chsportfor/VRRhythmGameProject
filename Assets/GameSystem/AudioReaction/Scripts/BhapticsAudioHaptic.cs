using UnityEngine;
using UnityEngine.InputSystem; // Added for flat-screen keyboard shortcut
using Bhaptics.SDK2;

/// <summary>
/// AudioReactionSystems의 주파수 분석 결과를 bHaptics Suit에 매핑합니다.
/// - Vest (상체, 40모터):
///   - 저음역 (FinalBass) -> 앞면 모터 (0~19)
///   - 고음역 (FinalTreble) -> 뒷면 모터 (20~39)
/// 
/// 사용법:
/// 1. 씬에 [bhaptics] 프리팹이 있어야 합니다 (SDK 초기화용).
/// 2. 이 컴포넌트를 AudioReactionSystems와 같은 GameObject에 붙이거나,
///    Inspector에서 audioReaction 필드에 참조를 연결하세요.
/// 3. VR 없이 모니터로 테스트할 때는 에디터에서 플레이 중 'N' 키를 누르면 INFX 노래가 재생됩니다.
/// </summary>
public class BhapticsAudioHaptic : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("AudioReactionSystems 컴포넌트 참조. 비어있으면 같은 GameObject에서 자동 검색합니다.")]
    public AudioReactionSystems audioReaction;

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

    [Header("업데이트 주기")]
    [Tooltip("햅틱 전송 간격 (초). 너무 짧으면 bHaptics 부하, 너무 길면 반응 느림.")]
    [Range(0.016f, 0.1f)]
    public float updateInterval = 0.03f; // ~33Hz

    // ── 내부 상태 ──
    private float lastUpdateTime = 0f;

    // Vest: 앞면 20 + 뒷면 20 = 40 모터 (TactSuit X40 스펙 반영)
    private int[] vestMotors = new int[40];

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

        // vestMotors 배열 초기화 (0으로 리셋)
        for (int i = 0; i < vestMotors.Length; i++)
        {
            vestMotors[i] = 0;
        }

        bool hasHaptic = false;

        // ── 앞면 (0 ~ 19): 저음역대 (Bass) ──
        if (bass > vestThreshold)
        {
            int bassIntensity = Mathf.Clamp((int)(bass * vestIntensityMultiplier * 100f), 1, 100);
            for (int i = 0; i < 20; i++)
            {
                vestMotors[i] = bassIntensity;
            }
            hasHaptic = true;
        }

        // ── 뒷면 (20 ~ 39): 고음역대 (Treble) ──
        if (treble > forearmThreshold)
        {
            int trebleIntensity = Mathf.Clamp((int)(treble * forearmIntensityMultiplier * 100f), 1, 100);
            for (int i = 20; i < 40; i++)
            {
                vestMotors[i] = trebleIntensity;
            }
            hasHaptic = true;
        }

        if (hasHaptic)
        {
            BhapticsLibrary.PlayMotors(
                (int)PositionType.Vest,
                vestMotors,
                Mathf.Max(vestDurationMs, forearmDurationMs)
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
