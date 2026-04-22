using UnityEngine;

public class AudioReactionSystems : MonoBehaviour
{
    public AudioSource audioSource;
    private float[] spectrum = new float[256];

    public float bassSensitivity = 20.0f;
    public float trebleSensitivity = 25.0f;

    public float depthContrast = 2.5f;
    public float deltaMultiplier = 2.0f;

    private float lastBassPeak = 0f;
    private float lastTreblePeak = 0f;

    public OVRInput.Controller controllerMask = OVRInput.Controller.RTouch | OVRInput.Controller.LTouch;

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