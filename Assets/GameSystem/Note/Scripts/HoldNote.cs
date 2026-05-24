using UnityEngine;

public class HoldNote : BaseNote
{
    [Header("Sci-Fi Neon Visuals")]
    [SerializeField] private Color beamColor = new Color(0f, 0.75f, 1f, 0.4f); // 40% 투명도의 시안 네온 컬러

    [Header("Hold VFX (Auto-copied from PunchNote if null)")]
    public GameObject hitEffectPrefab;
    public float hitEffectScale = 0.8f;
    public float hitEffectLifetime = 1.2f;

    private bool isLeftHandIn = false;
    private bool isRightHandIn = false;
    private GameObject leftHandObj;
    private GameObject rightHandObj;

    public float tickRate = 0.1f;
    private float holdTimer = 0f;

    private int holdTickCount = 0;
    public int requiredTicks = 5;

    private Material energyMaterial;

    private GameObject visualObj;
    private BoxCollider boxCollider;

    /// <summary>
    /// 스포너로부터 지속 시간과 속도를 전달받아 롱노트의 비주얼 길이와 충돌 판정 영역, 그리고 틱 카운트를 정밀 튜닝합니다.
    /// </summary>
    public void InitializeHold(float duration, float noteSpeed)
    {
        // 틱수 정밀 계산 (채보 상의 지속시간에 맞춤)
        requiredTicks = Mathf.Max(1, Mathf.RoundToInt(duration / tickRate));

        // 1. 기존의 밋밋한 큐브 비주얼(MeshRenderer)은 비활성화하여 콜라이더만 남겨둡니다.
        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        // Z축 길이 계산 (노트의 이동 속도 * 롱노트 지속시간)
        float laserLength = Mathf.Max(1.0f, noteSpeed * duration);

        // 2. 부모 BoxCollider 크기 늘리기 (롱노트가 다가올 때 시작점부터 끝점까지 가드가 유지되도록)
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(boxCollider.size.x, boxCollider.size.y, laserLength);
            // 피벗이 끝에 있으므로 콜라이더 중심을 뒤쪽으로 이동
            boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.center.y, laserLength * 0.5f);
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
        // 비주얼 중심을 뒤로 땡겨 콜라이더 영역과 정확히 동기화시킵니다.
        visualObj.transform.localPosition = new Vector3(0f, 0f, laserLength * 0.5f);

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

    protected override void Update()
    {
        base.Update();
        if (isLeftHandIn && isRightHandIn)
        {
            ProcessHolding();
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

        // 🚀 MissArea를 '완전히 빠져나갔을 때' 최종 판정 후 파괴
        if (other.CompareTag("MissArea"))
        {
            if (holdTickCount >= requiredTicks)
            {
                SpawnJudgementText("PERFECT HOLD!", Color.cyan);
                RegisterHitScore(100);
            }
            else if (holdTickCount > 0)
            {
                SpawnJudgementText("POOR HOLD", Color.yellow);
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
    }
}
