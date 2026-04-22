using UnityEngine;

public class HeightCalibrator : MonoBehaviour
{
    public Transform centerEyeAnchor;
    public Transform targetTrack;
    public float heightOffset = -0.1f;

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

        targetTrack.position = newPos;
        targetTrack.rotation = Quaternion.LookRotation(pForward);
    }
}