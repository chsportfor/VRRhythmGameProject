using UnityEngine;
using System.Collections;

public class HeightCalibrator : MonoBehaviour
{
    public static event System.Action<HeightCalibrator> OnCalibrationUpdated;
    public static event System.Action<HeightCalibrator> OnCalibrationCompleted;

    public Transform centerEyeAnchor;
    public Transform targetTrack;
    public float heightOffset = -0.1f;
    public float smoothSpeed = 10f;

    private Coroutine calibrationCoroutine;

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)) 
        {
            CalibrateHeight();
        }
    }

    public void CalibrateHeight()
    {
        if (centerEyeAnchor == null || targetTrack == null)
        {
            Debug.LogWarning("HeightCalibrator: Center Eye Anchor or Target Track is not assigned.");
            return;
        }

        float targetHeight = centerEyeAnchor.position.y + heightOffset;

        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
        }
        calibrationCoroutine = StartCoroutine(CalibrateRoutine(targetHeight));
    }

    private IEnumerator CalibrateRoutine(float targetHeight)
    {
        OnCalibrationUpdated?.Invoke(this);

        while (Mathf.Abs(targetTrack.position.y - GetSafeTargetHeight(targetHeight)) > 0.001f)
        {
            float nextHeight = Mathf.Lerp(GetCurrentTargetHeight(), targetHeight, Time.deltaTime * smoothSpeed);
            SetTargetTrackHeight(nextHeight);
            OnCalibrationUpdated?.Invoke(this);
            yield return null;
        }

        SetTargetTrackHeight(targetHeight);
        calibrationCoroutine = null;
        OnCalibrationUpdated?.Invoke(this);
        OnCalibrationCompleted?.Invoke(this);
    }

    private float GetCurrentTargetHeight()
    {
        TrackManager trackManager = GetTargetTrackManager();
        return trackManager != null ? trackManager.DesiredRootY : targetTrack.position.y;
    }

    private float GetSafeTargetHeight(float targetHeight)
    {
        TrackManager trackManager = GetTargetTrackManager();
        return trackManager != null ? trackManager.GetFloorSafeRootHeight(targetHeight) : targetHeight;
    }

    private void SetTargetTrackHeight(float targetHeight)
    {
        TrackManager trackManager = GetTargetTrackManager();
        if (trackManager != null)
        {
            trackManager.SetDesiredRootHeight(targetHeight);
            return;
        }

        Vector3 position = targetTrack.position;
        position.y = targetHeight;
        targetTrack.position = position;
    }

    private TrackManager GetTargetTrackManager()
    {
        return targetTrack != null ? targetTrack.GetComponent<TrackManager>() : null;
    }
}
