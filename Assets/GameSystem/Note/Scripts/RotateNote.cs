using UnityEngine;

public class RotateNote : BaseNote
{
    public float targetAngle = 90f;
    public float angleTolerance = 30f;
    public float snapShiftAngle = 90f;

    private bool isJudged = false;

    private float initialHandAngle;
    private bool isTrackingStarted = false;

    protected virtual void Start()
    {
        UpdateCurvedArrow();
    }

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
        
        // 초기 각도 대비 현재 회전량 계산 (방향 및 최소 각도 충족 여부 확인용)
        float rotatedAmount = Mathf.DeltaAngle(initialHandAngle, currentHandAngle);
        
        // 1. 올바른 방향으로 회전하고 있는지 확인 (양수면 시계 방향, 음수면 반시계 방향)
        bool isCorrectDirection = (targetAngle > 0 && rotatedAmount > 2f) || (targetAngle < 0 && rotatedAmount < -2f);
        
        // 2. 속도(회전 방향)도 타겟 방향과 일치하는지 확인
        bool isVelocityDirectionCorrect = (targetAngle > 0 && velocity > 0) || (targetAngle < 0 && velocity < 0);

        // 3. 목표 각도(targetAngle)에 비례하여 필요한 최소 회전량을 동적으로 계산합니다. (목표 각도의 65% 이상 회전 필요, 오작동 방지용 최소 하한선 25도)
        // 예: 45도 스냅 -> 약 29도 필요 | 90도 스냅 -> 약 58.5도 필요 | 180도 스냅 -> 약 117도 필요
        float minRotationRequired = Mathf.Max(25f, Mathf.Abs(targetAngle) * 0.65f);

        // 4. 동적으로 계산된 최소 회전량을 달성했고, 그 과정에서 충분한 스냅 속도(Threshold)를 냈다면 성공 처리!
        if (isCorrectDirection && isVelocityDirectionCorrect)
        {
            if (Mathf.Abs(rotatedAmount) >= minRotationRequired && Mathf.Abs(velocity) >= velocityThreshold)
            {
                Success();
            }
        }
    }

    void Success()
    {
        isJudged = true;
        SpawnJudgementText("SNAP PERFECT!", new Color(0.54f, 0.17f, 0.89f));
        RegisterHitScore(100);
        TrackManager.Instance.RotateTracks(snapShiftAngle);

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isJudged && other.CompareTag("MissArea"))
        {
            isJudged = true;
            SpawnJudgementText("MISS", Color.red);
            RegisterMiss();
            TrackManager.Instance.RotateTracks(snapShiftAngle);
            Destroy(gameObject);
        }
    }

    public void InitializeSnap(float angle)
    {
        targetAngle = angle;
        snapShiftAngle = angle;
        
        UpdateCurvedArrow();
    }

    [Header("Visuals")]
    public float arcRadius = 1.0f; // 화살표가 그려질 원의 반지름
    public int arcResolution = 20; // 90도 기준 곡선의 부드러움(점의 개수)
    public float arrowThickness = 0.1f; // 화살표 선의 두께
    [Tooltip("화살표 시작 각도 (0: 우측/좌측, 90: 위쪽/아래쪽). 게임의 타격 라인에 맞춰 조절하세요.")]
    public float baseStartAngle = 0f; 
    
    [Tooltip("꼬리가 그려지는 최대 각도를 제한합니다. 45도 등 작은 각도에서 꼬리가 너무 길게 느껴질 때 유용합니다. (0이면 제한 없음)")]
    public float maxDrawnAngle = 0f;

    [Header("Sprites")]
    [Tooltip("화살촉으로 사용할 2D 스프라이트 이미지를 넣어주세요 (오른쪽을 가리키는 흰색 이미지 권장)")]
    public Sprite arrowHeadSprite;
    public float arrowheadSize = 0.5f; // 화살촉 이미지의 크기
    [Tooltip("화살촉의 중심점(Pivot)을 미세 조정합니다. (X는 앞뒤, Y는 상하 이동)")]
    public Vector2 arrowheadOffset = Vector2.zero;

    private LineRenderer[] arcRenderers = new LineRenderer[2];
    private SpriteRenderer[] headRenderers = new SpriteRenderer[2]; // 화살촉 전용 스프라이트 렌더러

    void UpdateCurvedArrow()
    {
        // 이전 테스트로 메인 오브젝트에 생성된 LineRenderer가 있다면 제거
        LineRenderer oldRenderer = GetComponent<LineRenderer>();
        if (oldRenderer != null) Destroy(oldRenderer);

        for (int i = 0; i < 2; i++)
        {
            if (arcRenderers[i] == null)
            {
                // 1. 곡선 화살표 꼬리(몸통) 생성
                GameObject arcObj = new GameObject("CurvedArrowArc_" + i);
                arcObj.transform.SetParent(transform);
                arcObj.transform.localPosition = Vector3.zero;
                arcObj.transform.localRotation = Quaternion.identity;

                arcRenderers[i] = arcObj.AddComponent<LineRenderer>();
                arcRenderers[i].useWorldSpace = false;
                arcRenderers[i].material = new Material(Shader.Find("Sprites/Default"));
                arcRenderers[i].numCapVertices = 5; // 선 끝을 둥글게

                // 2. 화살표 머리(촉) 생성 (SpriteRenderer 사용)
                GameObject headObj = new GameObject("CurvedArrowHead_" + i);
                headObj.transform.SetParent(transform);
                headObj.transform.localPosition = Vector3.zero;
                headObj.transform.localRotation = Quaternion.identity;

                headRenderers[i] = headObj.AddComponent<SpriteRenderer>();
                headRenderers[i].sortingOrder = 1; // 선(몸통)보다 위에 표시되도록 설정
            }
            
            // 타겟 각도가 양수(시계 방향)면 매혹적인 네온 핫핑크(Electric Hot Pink), 음수(반시계 방향)면 영롱한 오션 네온 시안(Ocean Neon Cyan) 적용
            Color arrowColor = targetAngle > 0 
                ? new Color(1f, 0.15f, 0.45f)   // Premium Electric Neon Hot Pink (#FF2673)
                : new Color(0f, 0.8f, 1f);      // Premium Ocean Neon Cyan (#00CCFF)
            
            // 몸통 설정 (두께 일정)
            arcRenderers[i].startWidth = arrowThickness;
            arcRenderers[i].endWidth = arrowThickness;
            arcRenderers[i].startColor = arrowColor;
            arcRenderers[i].endColor = arrowColor;

            // 화살촉 스프라이트 적용 및 색상 설정
            if (arrowHeadSprite != null)
            {
                headRenderers[i].sprite = arrowHeadSprite;
            }
            headRenderers[i].color = arrowColor;

            // [수정됨] 꼬리를 그릴 실제 각도 (maxDrawnAngle이 설정되어 있으면 제한)
            float drawnAngle = targetAngle;
            if (maxDrawnAngle > 0f && Mathf.Abs(targetAngle) > maxDrawnAngle)
            {
                drawnAngle = maxDrawnAngle * Mathf.Sign(targetAngle);
            }
            
            // [수정됨] 엄청나게 큰 각도로 인한 LineRenderer 크래시 방지를 위해 최대 점 개수 제한
            int pointsCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawnAngle) / 90f * arcResolution), 10, 100);
            arcRenderers[i].positionCount = pointsCount;

            // 시작 각도 (baseStartAngle을 기준으로 양쪽에 배치)
            float startAngleDeg = baseStartAngle + (i * 180f); 
            float startAngleRad = startAngleDeg * Mathf.Deg2Rad; 
            
            // [수정됨] 모델이 카메라를 마주보도록 회전(-Z방향을 바라봄)되어 있는 VR 게임의 특성상,
            // 로컬 좌표계의 반시계 방향이 화면상에서는 시계 방향으로 뒤집혀 보일 수 있습니다.
            // 양수일 때 화면상 반시계(CCW), 음수일 때 시계(CW) 방향으로 그려지도록 부호를 반전합니다.
            float visualDrawnAngle = -drawnAngle;
            float drawnAngleRad = visualDrawnAngle * Mathf.Deg2Rad;
            
            for (int j = 0; j < pointsCount; j++)
            {
                float t = (float)j / (pointsCount - 1);
                
                // 화면상에서 원하는 방향으로 궤적 생성
                float currentAngle = startAngleRad + (t * drawnAngleRad); 
                
                float x = Mathf.Cos(currentAngle) * arcRadius;
                float y = Mathf.Sin(currentAngle) * arcRadius;
                
                arcRenderers[i].SetPosition(j, new Vector3(x, y, 0));
            }

            // === 화살촉(Sprite) 위치 및 회전 설정 ===
            float endAngle = startAngleRad + drawnAngleRad;
            float sign = Mathf.Sign(visualDrawnAngle);
            
            // 끝나는 지점의 좌표 계산
            Vector3 endPoint = new Vector3(Mathf.Cos(endAngle) * arcRadius, Mathf.Sin(endAngle) * arcRadius, 0);
            
            // 곡선의 끝점에서 나아가는 접선(방향) 계산
            Vector3 tangent = new Vector3(-Mathf.Sin(endAngle), Mathf.Cos(endAngle), 0) * sign;
            tangent.Normalize();

            // 화살촉 회전
            float rotZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            headRenderers[i].transform.localRotation = Quaternion.Euler(0, 0, rotZ);
            
            // [수정됨] transform.right 와 transform.up 은 월드 공간(World Space) 기준이므로 
            // 부모 오브젝트(RotateNote)가 씬에서 회전되어 있으면(예: 45도 트랙에 위치) 방향이 틀어지게 됩니다.
            // 따라서 로컬 공간(Local Space)에서의 접선과 수직 벡터를 직접 계산하여 사용해야 완벽하게 맞물립니다.
            Vector3 localRight = tangent; // 로컬 X축 (접선 방향)
            Vector3 localUp = new Vector3(-tangent.y, tangent.x, 0); // 로컬 Y축 (접선의 수직 방향)
            
            // 음수 각도일 때 로컬 Y축 방향이 시각적으로 뒤집히는 현상을 상쇄하기 위해 Y 오프셋에 sign을 곱해줍니다.
            // 이렇게 하면 Y 오프셋이 항상 원의 안쪽/바깥쪽으로 일관되게 적용됩니다.
            Vector3 offsetPosition = endPoint + (localRight * arrowheadOffset.x) + (localUp * arrowheadOffset.y * sign);

            headRenderers[i].transform.localPosition = offsetPosition;
            
            // 화살촉 크기 설정
            headRenderers[i].transform.localScale = new Vector3(arrowheadSize, arrowheadSize, 1f);
        }
    }

    private void OnDestroy()
    {
        // [수정됨] 동적으로 생성한 Material은 자동으로 메모리에서 해제되지 않아 쌓이면 크래시를 유발합니다.
        // 오브젝트가 파괴될 때 Material도 함께 삭제하여 메모리 누수를 방지합니다.
        if (arcRenderers != null)
        {
            for (int i = 0; i < arcRenderers.Length; i++)
            {
                if (arcRenderers[i] != null && arcRenderers[i].material != null)
                {
                    Destroy(arcRenderers[i].material);
                }
            }
        }
    }
}
