using UnityEngine;
public class BaseNote : MonoBehaviour
{
    public float speed = 10f;
    public GameObject JudgementTextPrefab;
    public Vector3 judgementOffset = new Vector3(0f, 0f, 5f);

    protected Transform target;
    protected bool isMoving = false;
    protected Vector3 moveDirection;

    public virtual void SetTarget(Transform hitAreaTarget)
    {
        target = hitAreaTarget;
        moveDirection = (target.position - transform.position).normalized;
        
        // [수정됨] Unity의 LookAt 함수는 방향 벡터가 평행하거나 0일 때 NaN을 반환하여
        // 게임 뷰 렌더링 중 C++ 크래시를 유발할 수 있습니다. 이를 방지하기 위해 안전한 회전 방식을 사용합니다.
        if (moveDirection.sqrMagnitude > 0.001f && transform.up.sqrMagnitude > 0.001f)
        {
            // TrackManager 등에 의해 회전된 현재의 위쪽(Up) 방향을 최대한 유지하면서 타겟을 바라봅니다.
            Quaternion safeRotation = Quaternion.LookRotation(moveDirection, transform.up);
            
            // 만약 계산된 회전값이 정상(NaN이 아님)이라면 적용합니다.
            if (!float.IsNaN(safeRotation.x) && !float.IsNaN(safeRotation.y) && !float.IsNaN(safeRotation.z) && !float.IsNaN(safeRotation.w))
            {
                transform.rotation = safeRotation;
            }
            else
            {
                // 계산 실패 시 월드 Up을 기준으로 안전하게 회전합니다.
                transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            }
        }
        
        isMoving = true;
    }
    protected virtual void Update()
    {
        if (!isMoving) return;
        MoveNote();
    }

    protected virtual void MoveNote()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    protected void SpawnJudgementText(string text, Color color)
    {
        if (JudgementTextPrefab != null)
        {
            GameObject textObj = Instantiate(JudgementTextPrefab, judgementOffset, Quaternion.identity);
            JudgementText jt = textObj.GetComponent<JudgementText>();
            if (jt != null) jt.Setup(text, color);
        }
    }
}