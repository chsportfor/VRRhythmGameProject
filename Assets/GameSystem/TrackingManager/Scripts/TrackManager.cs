using UnityEngine;
using UnityEngine.InputSystem;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance { get; private set; }

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
    public float snapVelocityThreshold = 180f; // 300f에서 180f로 하향 조정하여 자연스러운 곡선 스냅도 부드럽게 감지
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

    /// <summary>
    /// 플레이어의 실제 VR 카메라 위치와 시선 방향을 바탕으로 
    /// 트랙 전체의 수평 중심(X, Z)과 바라보는 각도(Y회전)를 내 정면 앞으로 완벽하게 자동 정렬합니다.
    /// (높이는 HeightCalibrator에서 부드럽게 조정하므로 수평/각도 정렬만 수행)
    /// </summary>
    public void AlignTrackToPlayer()
    {
        Transform cam = null;
        if (Camera.main != null) cam = Camera.main.transform;
        else
        {
            OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null) cam = rig.centerEyeAnchor;
        }

        if (cam == null)
        {
            Debug.LogWarning("[TrackManager] 정렬할 카메라(머리 위치)를 찾을 수 없어 기본 위치를 유지합니다.");
            return;
        }

        // 1. 수평 좌표(X, Z) 정렬 (플레이어가 서 있는 수평 위치가 트랙의 중심이 되도록 스냅)
        Vector3 newPos = transform.position;
        newPos.x = cam.position.x;
        newPos.z = cam.position.z;
        transform.position = newPos;

        // 2. 수평 시선 방향(Y축 회전) 정렬 (플레이어가 바라보는 정방향을 트랙의 Z축 앞방향으로 스냅)
        Vector3 forward = cam.forward;
        forward.y = 0f; // 트랙이 위아래로 기울어지는 롤러코스터 버그 방지를 위해 Y축 평평하게 고정
        forward.Normalize();

        if (forward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(forward);
            initialRotation = transform.rotation; // 스핀 컨트롤 회전을 연동하기 위해 기준 회전 갱신
            TotalTrackRotation = 0f; // 기존 회전 축 누적 리셋
        }
        
        Debug.Log($"[TrackManager] 캘리브레이션 버튼 연동 수평 정렬 완료! 수평위치: ({newPos.x:F2}, {newPos.z:F2}), 바라보는 각도: {transform.rotation.eulerAngles.y:F1}도");
    }

    void Update()
    {
        UpdateTrackRotation();
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

            CurrentAngularVelocity = Mathf.Lerp(CurrentAngularVelocity, rawVelocity, Time.deltaTime * 35f); // 15f에서 35f로 속도 추적 반응성 대폭 상향
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

    /// <summary>
    /// 트랙 상의 모든 비주얼 요소(레일, 스포너, 히트존 판정선 등 자식 오브젝트)를 일괄적으로 켜거나 끕니다.
    /// TrackManager 컴포넌트 자체는 활성화 상태로 두어 다른 참조 스크립트의 NullReferenceException 폭발을 완벽히 차단합니다!
    /// </summary>
    public void SetTrackVisualsActive(bool active)
    {
        // 1. 스포너 및 판정선 비활성화
        if (spawners != null)
        {
            foreach (var sp in spawners)
            {
                if (sp != null) sp.gameObject.SetActive(active);
            }
        }
        
        if (hitZones != null)
        {
            foreach (var hz in hitZones)
            {
                if (hz != null) hz.gameObject.SetActive(active);
            }
        }

        // 2. 부모 아래에 뻗어 있는 모든 3D 레일 매쉬 데코 등 자식 오브젝트도 켜거나 끔
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                // spawners나 hitZones 배열 자식이 아닌 일반 메쉬도 싹 제어
                child.gameObject.SetActive(active);
            }
        }
    }
}
