using UnityEngine;

public class HoldNote : BaseNote
{
    [Header("Sci-Fi Neon Visuals")]
    [SerializeField] private Color beamColor = new Color(0f, 0.75f, 1f, 0.4f); // 40% 투명도의 시안 네온 컬러

    [Header("Hold VFX (Auto-copied from PunchNote if null)")]
    public GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectScale = 0.15f; // VFX 크기를 0.8f에서 0.35f로 아담하고 영롱하게 보정
    [SerializeField] private float hitEffectLifetime = 1.2f;

    private bool isLeftHandIn = false;
    private bool isRightHandIn = false;
    private GameObject leftHandObj;
    private GameObject rightHandObj;

    [SerializeField] private float tickRate = 0.1f;
    private float holdTimer = 0f;

    private int holdTickCount = 0;
    [HideInInspector] public int requiredTicks = 5;
    private float actualHoldDuration = 0f; // 실제 양손 가드가 유지된 시간 (초)

    private Material energyMaterial;

    private GameObject visualObj;
    private BoxCollider boxCollider;

    // ─── 시간 기반 Hold 판정을 위한 필드 ───
    private float holdDurationSeconds;    // Hold 지속 시간(초)
    private float noteSpeed;              // 노트 이동 속도 (캐시)
    private float laserLengthCached;      // 레이저 길이 (캐시)
    private bool holdPhaseStarted = false; // 머리가 판정선에 도달하여 Hold 구간 시작됨
    private float holdPhaseElapsed = 0f;   // Hold 구간 시작 후 경과 시간
    private bool isJudged = false;         // 이미 판정 완료됨

    /// <summary>
    /// 채보 박수·BPM·접근 구간을 바탕으로 롱노트 비주얼/콜라이더 길이와 홀드 틱 수를 설정합니다.
    /// 비주얼 길이 = (스폰~판정 거리) × (홀드 박수 / 접근 구간 박수) — 에디터 차트와 동일한 박 기준.
    /// </summary>
    public void InitializeHold(float durationBeats, float durationSeconds, float approachDistance, float approachTimeSec, float bpm)
    {
        holdDurationSeconds = durationSeconds;
        requiredTicks = Mathf.Max(1, Mathf.RoundToInt(durationSeconds / tickRate));

        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        float safeBpm = bpm > 0f ? bpm : 120f;
        float safeApproachTime = approachTimeSec > 0.001f ? approachTimeSec : 1f;
        float beatsInApproach = safeApproachTime * safeBpm / 60f;
        if (beatsInApproach <= 0.001f)
        {
            beatsInApproach = 1f;
        }

        float laserLength = approachDistance * (durationBeats / beatsInApproach);
        laserLength = Mathf.Max(0.2f, laserLength);
        laserLengthCached = laserLength;

        // 2. 부모 BoxCollider 크기 늘리기 (롱노트가 다가올 때 시작점부터 끝점까지 가드가 유지되도록)
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(boxCollider.size.x, boxCollider.size.y, laserLength);
            // 피벗이 머리에 있으므로 콜라이더 중심을 뒤쪽(-Z)으로 이동
            boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.center.y, -laserLength * 0.5f);
        }

        // 3. 프리미엄 '실린더(원기둥)' SF 레이저 빔 오브젝트를 동적으로 생성하여 자식으로 배치합니다.
        if (visualObj == null)
        {
            visualObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualObj.name = "LaserBeamVisual";
            
            // 동적으로 딸려 나오는 실린더 콜라이더는 충돌 버그 방지를 위해 즉시 제거합니다.
            Collider cylCollider = visualObj.GetComponent<Collider>();
            if (cylCollider != null)
            {
                Destroy(cylCollider);
            }

            visualObj.transform.SetParent(transform, false);
            
            // 유니티 기본 실린더는 Y축 정렬이므로, 노트 Z축 방향으로 눕히기 위해 로컬 회전을 X축 90도 회전시킵니다.
            visualObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // 실린더 기본 높이가 2이므로, laserLength가 되기 위해 Y 스케일은 laserLength * 0.5f로 설정합니다.
        visualObj.transform.localScale = new Vector3(1f, laserLength * 0.5f, 1f);
        // 비주얼 중심을 뒤쪽(-Z)으로 땡겨 콜라이더 영역과 정확히 동기화시킵니다.
        visualObj.transform.localPosition = new Vector3(0f, 0f, -laserLength * 0.5f);

        // 4. 반투명한 일렉트릭 에너지 빔 머티리얼을 실린더에 적용 (Sprites/Default 셰이더)
        MeshRenderer cylinderRenderer = visualObj.GetComponent<MeshRenderer>();
        if (cylinderRenderer != null)
        {
            energyMaterial = new Material(Shader.Find("Sprites/Default"));
            energyMaterial.color = beamColor;
            cylinderRenderer.material = energyMaterial;
        }

        // 5. 만약 이펙트 프리팹이 지정되지 않았다면, 씬/프로젝트 내의 PunchNote 프리팹에서 이펙트를 자동으로 복사해와 바인딩
        if (hitEffectPrefab == null)
        {
            PunchNote[] punchNotes = Resources.FindObjectsOfTypeAll<PunchNote>();
            if (punchNotes != null && punchNotes.Length > 0)
            {
                foreach (var punch in punchNotes)
                {
                    if (punch.hitEffectPrefab != null)
                    {
                        hitEffectPrefab = punch.hitEffectPrefab;
                        break;
                    }
                }
            }
        }
    }

    protected override void MoveNote()
    {
        // Hold 판정 구간이 시작되면 노트 머리가 판정선에 고정되어야 하므로 이동을 중지하고 판정선에 밀착시킵니다.
        if (holdPhaseStarted)
        {
            if (target != null)
            {
                if (useLocalMovement && movementSpace != null)
                {
                    // 로컬 좌표계를 사용하는 경우 부모 트랙 기준의 로컬 타겟 좌표에 정확히 고정
                    transform.localPosition = movementSpace.InverseTransformPoint(target.position);
                }
                else
                {
                    // 월드 좌표계를 사용하는 경우 월드 타겟 좌표에 고정
                    transform.position = target.position;
                }
            }
            return;
        }

        base.MoveNote();
    }

    protected override void Update()
    {
        if (isJudged) return;

        // 🚀 1. 부모의 Update(이동 및 포지션 갱신)를 맨 먼저 호출하여 포지션을 완전히 최신화한 뒤 하위 연산을 수행합니다.
        // 이로써 부모의 프레임 고정 상태와 자식 실린더의 오프셋 셋팅 사이에 발생하던 1프레임 딜레이 지터링(Jitter) 현상을 차단합니다!
        base.Update();

        // ─── 시간 기반 Hold 구간 추적 ───
        // isMoving이 참이고(SetTarget이 완료되어 실제로 움직이기 시작함) moveDirection이 셋팅되었을 때만 감지
        if (!holdPhaseStarted && target != null && isMoving && moveDirection.sqrMagnitude > 0.001f)
        {
            float dotProduct = 1f;

            if (useLocalMovement && movementSpace != null)
            {
                // 로컬 좌표계를 사용하는 경우 부모 트랙 기준의 로컬 좌표 변환 후 닷 프로덕트 연산
                Vector3 localTargetPos = movementSpace.InverseTransformPoint(target.position);
                Vector3 toTargetLocal = localTargetPos - transform.localPosition;
                dotProduct = Vector3.Dot(toTargetLocal, localMoveDirection);
            }
            else
            {
                // 월드 좌표계를 사용하는 경우 기존 월드 기준 닷 프로덕트 연산
                Vector3 toTarget = target.position - transform.position;
                dotProduct = Vector3.Dot(toTarget, moveDirection);
            }

            // 닷 프로덕트가 0 이하이면 머리가 판정선에 완벽히 도달하거나 지나간 것
            if (dotProduct <= 0f)
            {
                holdPhaseStarted = true;
                holdPhaseElapsed = 0f;
                
                // 도달 시점에 판정선 정위치 고정
                if (useLocalMovement && movementSpace != null)
                {
                    transform.localPosition = movementSpace.InverseTransformPoint(target.position);
                }
                else
                {
                    transform.position = target.position;
                }

                // 🚀 핵심: 판정이 시작되는 순간 콜라이더를 쪼그라뜨리지 않고, 
                // 판정선 링 주변에 두툼하고 넓은 가드 전용 핫스팟 볼륨(깊이 2m, 가로세로 1.8배)으로 전환/고정하여 손 떨림 등으로 판정이 끊기는 현상 원천 차단!
                if (boxCollider != null)
                {
                    boxCollider.size = new Vector3(boxCollider.size.x * 1.8f, boxCollider.size.y * 1.8f, 2.0f);
                    boxCollider.center = Vector3.zero; // 피벗(머리) 기준으로 앞뒤 1미터씩 넉넉히 커버
                }
            }
        }

        // Hold 구간이 시작되었으면 경과 시간을 추적
        if (holdPhaseStarted)
        {
            holdPhaseElapsed += Time.deltaTime;

            // ─── 비주얼 레이저 실린더만 실시간으로 깎아내기 (수축 연출) ───
            float currentLaserLength = laserLengthCached - (speed * holdPhaseElapsed);
            currentLaserLength = Mathf.Max(0f, currentLaserLength);

            if (visualObj != null)
            {
                // 실린더 스케일 및 로컬 위치(피벗 Z축 반만큼 뒤로 밀기) 실시간 업데이트
                visualObj.transform.localScale = new Vector3(1f, currentLaserLength * 0.5f, 1f);
                visualObj.transform.localPosition = new Vector3(0f, 0f, -currentLaserLength * 0.5f);
            }

            // 🚀 콜라이더 영역은 이미 도달 순간 고정되었으므로 매 프레임 업데이트에서 제외하여 가드 트리거 안정성 확보!

            // 🚀 가드 상태 체크: 
            // 유니티 물리 엔진의 스냅 좌표 충돌(Trigger) 단절 버그를 완벽하게 차단하기 위해, 
            // TrackManager가 실시간으로 추적하는 VR 컨트롤러의 월드 거리를 판정선과 직접 비교 검증합니다!
            bool leftHandGuarding = false;
            bool rightHandGuarding = false;
            float guardRadius = 0.8f; // 판정선 주변 80cm 넉넉한 가드 반경

            if (TrackManager.Instance != null)
            {
                if (TrackManager.Instance.leftController != null)
                {
                    float dist = Vector3.Distance(transform.position, TrackManager.Instance.leftController.position);
                    if (dist <= guardRadius) leftHandGuarding = true;
                }
                
                if (TrackManager.Instance.rightController != null)
                {
                    float dist = Vector3.Distance(transform.position, TrackManager.Instance.rightController.position);
                    if (dist <= guardRadius) rightHandGuarding = true;
                }
            }

            // 양손이 모두 가드 반경 내에 안정적으로 안착해 있으면 홀드 게이지를 누적하고 연출을 활성화합니다.
            if (leftHandGuarding && rightHandGuarding)
            {
                actualHoldDuration += Time.deltaTime;
                
                // 가드 성공 상태에서는 양손 오브젝트 레퍼런스를 TrackManager의 컨트롤러로 매핑해 이펙트를 보장합니다.
                if (TrackManager.Instance != null)
                {
                    leftHandObj = TrackManager.Instance.leftController.gameObject;
                    rightHandObj = TrackManager.Instance.rightController.gameObject;
                    isLeftHandIn = true;
                    isRightHandIn = true;
                }
                
                ProcessHolding();
            }
            else
            {
                // 영역 이탈 시 가드 상태 해제
                isLeftHandIn = false;
                isRightHandIn = false;
            }

            // holdDurationSeconds가 지나면 자동 판정 & 파괴
            if (holdPhaseElapsed >= holdDurationSeconds)
            {
                JudgeAndDestroy();
                return;
            }
        }
    }

    void ProcessHolding()
    {
        holdTimer += Time.deltaTime;
        
        // 홀드하고 있는 동안 영롱한 네온 빔이 미세하게 진동/맥동(Pulse)하여 살아있는 느낌 부여
        if (energyMaterial != null)
        {
            float pulse = 0.4f + Mathf.PingPong(Time.time * 4f, 0.2f);
            energyMaterial.color = new Color(beamColor.r, beamColor.g, beamColor.b, pulse);
        }

        if (holdTimer >= tickRate)
        {
            holdTickCount++;
            SpawnJudgementText("GUARD", Color.blue);
            holdTimer = 0f;

            // 3. 홀드(가드) 성공 중일 때 양손 위치에서 PunchNote의 고품질 타격 이펙트(VFX)를 실시간 연쇄 생성!
            if (hitEffectPrefab != null)
            {
                if (isLeftHandIn && leftHandObj != null)
                {
                    SpawnEffect(hitEffectPrefab, leftHandObj.transform.position, hitEffectScale, hitEffectLifetime);
                }
                if (isRightHandIn && rightHandObj != null)
                {
                    SpawnEffect(hitEffectPrefab, rightHandObj.transform.position, hitEffectScale, hitEffectLifetime);
                }
            }
        }
    }

    /// <summary>
    /// Hold 판정을 수행하고 노트를 파괴합니다.
    /// 시간 기반 자동 판정 또는 MissArea 퇴장 시 호출됩니다.
    /// </summary>
    private void JudgeAndDestroy()
    {
        if (isJudged) return;
        isJudged = true;

        float holdRatio = 0f;
        if (holdDurationSeconds > 0.001f)
        {
            holdRatio = actualHoldDuration / holdDurationSeconds;
        }

        // 사용자의 판정 기준에 따른 Perfect/Good/Poor/Miss 점수 지급 및 UI 연출
        if (holdRatio >= 0.9f)
        {
            SpawnJudgementText("PERFECT GUARD!", Color.cyan);
            RegisterHitScore(100);
        }
        else if (holdRatio >= 0.6f)
        {
            SpawnJudgementText("GOOD GUARD", Color.green);
            RegisterHitScore(70);
        }
        else if (actualHoldDuration > 0.001f)
        {
            SpawnJudgementText("POOR GUARD", Color.yellow);
            RegisterHitScore(40);
        }
        else
        {
            SpawnJudgementText("MISS", Color.red);
            RegisterMiss();
        }

        // 생성한 동적 머티리얼 메모리 해제하여 누수 방지
        if (energyMaterial != null)
        {
            Destroy(energyMaterial);
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandIn = true;
            leftHandObj = other.gameObject;
        }
        if (other.CompareTag("RightHand"))
        {
            isRightHandIn = true;
            rightHandObj = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandIn = false;
            leftHandObj = null;
        }
        if (other.CompareTag("RightHand"))
        {
            isRightHandIn = false;
            rightHandObj = null;
        }

        // 🚀 MissArea를 '완전히 빠져나갔을 때' — 시간 기반 판정이 아직 안 된 경우의 백업
        if (other.CompareTag("MissArea"))
        {
            JudgeAndDestroy();
        }
    }
}
