using UnityEngine;
using System.Collections;

public class HeightCalibrator : MonoBehaviour
{
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
        Vector3 pPos = centerEyeAnchor.position;
        Vector3 pForward = centerEyeAnchor.forward;

        pForward.y = 0;
        pForward.Normalize();

        Vector3 newPos = new Vector3(pPos.x, pPos.y + heightOffset, pPos.z);
        Quaternion newRot = Quaternion.LookRotation(pForward);

        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
        }
        calibrationCoroutine = StartCoroutine(CalibrateRoutine(newPos, newRot));
    }

    private IEnumerator CalibrateRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        while (Vector3.Distance(targetTrack.position, targetPos) > 0.001f)
        {
            targetTrack.position = Vector3.Lerp(targetTrack.position, targetPos, Time.deltaTime * smoothSpeed);
            yield return null;
        }
        
        targetTrack.position = targetPos;
        calibrationCoroutine = null;
    }
}
