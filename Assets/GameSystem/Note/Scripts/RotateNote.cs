using UnityEngine;

public class RotateNote : BaseNote
{
    public float targetAngle = 90f;
    public float angleTolerance = 30f;
    public float snapShiftAngle = 90f;

    private bool isJudged = false;

    // 시작 손 각도 저장을 위한 변수
    private float initialHandAngle;
    private bool isTrackingStarted = false;

    protected override void Update()
    {
        base.Update();

        if (!isJudged && target != null)
        {
            float distanceError = Vector3.Distance(transform.position, target.position);
            if (!isTrackingStarted && distanceError < 1.5f)
            {
                initialHandAngle = TrackManager.Instance.CurrentHandAngle;
                isTrackingStarted = true;
            }
            if (isTrackingStarted && distanceError < 1.5f)
            {
                CheckSnapAction(distanceError);
            }
        }
    }

    void CheckSnapAction(float currentDistance)
    {
        float currentHandAngle = TrackManager.Instance.CurrentHandAngle;
        float velocity = TrackManager.Instance.CurrentAngularVelocity;
        float velocityThreshold = TrackManager.Instance.snapVelocityThreshold;
        float rotatedAmount = Mathf.DeltaAngle(initialHandAngle, currentHandAngle);
        float angleDiff = Mathf.Abs(Mathf.Abs(rotatedAmount) - Mathf.Abs(targetAngle));
        if (angleDiff <= angleTolerance && Mathf.Abs(velocity) >= velocityThreshold)
        {
            if ((targetAngle > 0 && velocity > 0) || (targetAngle < 0 && velocity < 0))
            {
                Success();
            }
        }
    }

    void Success()
    {
        isJudged = true;
        SpawnJudgementText("SNAP PERFECT!", Color.blueViolet);
        TrackManager.Instance.RotateTracks(snapShiftAngle);

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isJudged && other.CompareTag("MissArea"))
        {
            SpawnJudgementText("MISS", Color.red);
            Destroy(gameObject);
     
        }
    }

    public void InitializeSnap(float angle)
    {
        targetAngle = angle;
        snapShiftAngle = angle;
    }
}