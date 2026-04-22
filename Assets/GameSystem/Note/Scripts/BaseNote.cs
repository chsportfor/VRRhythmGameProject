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
        transform.LookAt(target);
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