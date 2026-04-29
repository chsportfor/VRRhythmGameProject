using UnityEngine;
using UnityEngine.InputSystem;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance;

    public Transform[] spawners;
    public Transform[] hitZones;
    public float laneWidth = 0.55f;

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

    void Awake()
    {
        Instance = this;
        TotalTrackRotation = 0f;
    }

    void Start()
    {
        initialRotation = transform.rotation;
        SetupLanes();
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
}