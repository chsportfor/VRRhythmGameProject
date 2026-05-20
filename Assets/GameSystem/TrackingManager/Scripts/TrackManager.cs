using UnityEngine;
using UnityEngine.InputSystem;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance;

    public Transform[] spawners;
    public Transform[] hitZones;
    public float laneWidth = 0.55f;

    [Header("Floor Clearance")]
    public bool keepLanesAboveFloor = true;
    public float floorY = 0f;
    public float laneFloorClearance = 0.05f;

    public Transform leftController;
    public Transform rightController;

    public float rotationSensitivity = 1.0f;
    public float rotationSmoothSpeed = 10f;
    public float snapVelocityThreshold = 300f;
    public float CurrentHandAngle { get; private set; }
    public float CurrentAngularVelocity { get; private set; }

    public float TotalTrackRotation { get; private set; }

    private Quaternion initialRotation;
    private float lastHandAngle;
    private float desiredRootY;
    private bool hasDesiredRootY;

    void Awake()
    {
        Instance = this;
        TotalTrackRotation = 0f;
    }

    void Start()
    {
        initialRotation = transform.rotation;
        SetupLanes();
        SetDesiredRootHeight(transform.position.y);
    }

    void Update()
    {
        UpdateTrackRotation();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame) SpawnNoteOnLane(0);
            if (Keyboard.current.sKey.wasPressedThisFrame) SpawnNoteOnLane(1);
            if (Keyboard.current.dKey.wasPressedThisFrame) SpawnNoteOnLane(2);
            if (Keyboard.current.fKey.wasPressedThisFrame) SpawnNoteOnLane(3);
        }
    }

    void LateUpdate()
    {
        ApplyFloorClearance();
    }

    void UpdateTrackRotation()
    {
        if (leftController != null && rightController != null)
        {
            Vector3 handDirection = rightController.position - leftController.position;
            float rawAngle = Mathf.Atan2(handDirection.y, handDirection.x) * Mathf.Rad2Deg * rotationSensitivity;
            float deltaAngle = Mathf.DeltaAngle(lastHandAngle, rawAngle);
            float rawVelocity = deltaAngle / Time.deltaTime;
            lastHandAngle = rawAngle;

            CurrentAngularVelocity = Mathf.Lerp(CurrentAngularVelocity, rawVelocity, Time.deltaTime * 15f);
            CurrentHandAngle = rawAngle;

            Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, TotalTrackRotation);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }
    }

    void SetupLanes()
    {
        spawners[0].localPosition = new Vector3(-laneWidth, 0, 20f);
        hitZones[0].localPosition = new Vector3(-laneWidth, 0, 0.7f);

        spawners[1].localPosition = new Vector3(0, 0, 20f);
        hitZones[1].localPosition = new Vector3(0, 0, 0.75f);

        spawners[2].localPosition = new Vector3(laneWidth, 0, 20f);
        hitZones[2].localPosition = new Vector3(laneWidth, 0, 0.7f);

        spawners[3].localPosition = new Vector3(0, 0, 20f);
        hitZones[3].localPosition = new Vector3(0, 0, 0.75f);
    }

    public void SpawnNoteOnLane(int laneIndex)
    {
        spawners[laneIndex].GetComponent<SpawnArea>().SpawnNote();
    }

    public void SpawnRotateNoteOnLane(int laneIndex, float angle)
    {
        spawners[laneIndex].GetComponent<SpawnArea>().SpawnRotateNote(angle);
    }
    public void RotateTracks(float targetAngle)
    {
        TotalTrackRotation += targetAngle;
    }

    public float DesiredRootY
    {
        get
        {
            EnsureDesiredRootY();
            return desiredRootY;
        }
    }

    public float SetDesiredRootHeight(float rootY)
    {
        desiredRootY = rootY;
        hasDesiredRootY = true;
        ApplyFloorClearance();
        return transform.position.y;
    }

    public float GetFloorSafeRootHeight(float requestedRootY)
    {
        if (!keepLanesAboveFloor)
        {
            return requestedRootY;
        }

        float lowestRelativeY = GetLowestLanePointY() - transform.position.y;
        float minimumRootY = floorY + laneFloorClearance - lowestRelativeY;
        return Mathf.Max(requestedRootY, minimumRootY);
    }

    private void ApplyFloorClearance()
    {
        EnsureDesiredRootY();

        Vector3 position = transform.position;
        float safeRootY = GetFloorSafeRootHeight(desiredRootY);
        if (Mathf.Abs(position.y - safeRootY) <= 0.0001f)
        {
            return;
        }

        position.y = safeRootY;
        transform.position = position;
    }

    private void EnsureDesiredRootY()
    {
        if (hasDesiredRootY)
        {
            return;
        }

        desiredRootY = transform.position.y;
        hasDesiredRootY = true;
    }

    private float GetLowestLanePointY()
    {
        float lowestY = float.PositiveInfinity;
        bool foundPoint = false;

        IncludeLowestPoint(spawners, ref lowestY, ref foundPoint);
        IncludeLowestPoint(hitZones, ref lowestY, ref foundPoint);

        return foundPoint ? lowestY : transform.position.y;
    }

    private static void IncludeLowestPoint(Transform[] points, ref float lowestY, ref bool foundPoint)
    {
        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
            {
                continue;
            }

            lowestY = Mathf.Min(lowestY, points[i].position.y);
            foundPoint = true;
        }
    }
}
