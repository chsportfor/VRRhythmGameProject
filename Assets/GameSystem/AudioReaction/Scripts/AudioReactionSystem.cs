using UnityEngine;

public class AudioReactionSystems : MonoBehaviour
{
    public AudioSource audioSource;
    private float[] spectrum = new float[256];

    public float bassSensitivity = 10.0f;
    public float trebleSensitivity = 25.0f;

    public float depthContrast = 2.5f;
    public float deltaMultiplier = 2.0f;

    private float lastBassPeak = 0f;
    private float lastTreblePeak = 0f;

    public OVRInput.Controller controllerMask = OVRInput.Controller.RTouch | OVRInput.Controller.LTouch;

    // ── bHaptics 연동을 위해 외부에서 읽을 수 있도록 노출 ──
    // 0.0 ~ 1.0 범위의 최종 처리된 주파수 강도 값
    /// <summary>저음역 최종 강도 (0~1). Vest/상체 햅틱에 사용.</summary>
    public float FinalBass { get; private set; }

    /// <summary>고음역 최종 강도 (0~1). Forearm/팔 햅틱에 사용.</summary>
    public float FinalTreble { get; private set; }

    /// <summary>저음역 원시 피크 값 (스펙트럼 [0~3] 최대값).</summary>
    public float RawBassPeak { get; private set; }

    /// <summary>고음역 원시 피크 값 (스펙트럼 [25~50] 최대값).</summary>
    public float RawTreblePeak { get; private set; }

    void Update()
    {
        if (audioSource == null) return;

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float bassPeak = 0f;
        for (int i = 0; i <= 3; i++)
        {
            if (spectrum[i] > bassPeak) bassPeak = spectrum[i];
        }

        float treblePeak = 0f;
        for (int i = 25; i <= 50; i++)
        {
            if (spectrum[i] > treblePeak) treblePeak = spectrum[i];
        }

        float bassDelta = bassPeak - lastBassPeak;
        float trebleDelta = treblePeak - lastTreblePeak;

        float targetBassAmplitude = Mathf.Clamp01(bassPeak * bassSensitivity);
        float targetTrebleAmplitude = Mathf.Clamp01(treblePeak * trebleSensitivity);

        float finalBass = Mathf.Pow(targetBassAmplitude, depthContrast);
        float finalTreble = Mathf.Pow(targetTrebleAmplitude, depthContrast);

        if (bassDelta > 0)
        {
            finalBass += (bassDelta * bassSensitivity * deltaMultiplier);
        }
        if (trebleDelta > 0)
        {
            finalTreble += (trebleDelta * trebleSensitivity * deltaMultiplier);
        }

        finalBass = Mathf.Clamp01(finalBass);
        finalTreble = Mathf.Clamp01(finalTreble);

        // 외부 접근용 프로퍼티 갱신
        FinalBass = finalBass;
        FinalTreble = finalTreble;
        RawBassPeak = bassPeak;
        RawTreblePeak = treblePeak;

        ApplyOVRHaptics(finalBass, finalTreble);

        lastBassPeak = bassPeak;
        lastTreblePeak = treblePeak;
    }

    private void ApplyOVRHaptics(float bass, float treble)
    {

        OVRInput.SetControllerVibration(0, bass, OVRInput.Controller.RTouch);

        OVRInput.SetControllerVibration(0, treble, OVRInput.Controller.LTouch);
    }

    void OnDisable()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.All);
    }
}