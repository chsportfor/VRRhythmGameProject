// 5. PunchNote.cs
using UnityEngine;

public class PunchNote : BaseNote
{
    public float perfectRadius = 0.15f;
    public float greatRadius = 0.3f;
    public float goodRadius = 0.5f;
    public JudgementText judgementText;
    private bool isJudged = false;

    private void OnTriggerEnter(Collider other)
        {
            if (isJudged) return;
            if (other.CompareTag("LeftHand") || other.CompareTag("RightHand"))
            {
                isJudged = true;
                HitNote(other.gameObject);
            }
            else if (other.CompareTag("MissArea"))
            {
                isJudged = true;
                MissNote();
            }
        }

    private void HitNote(GameObject controller)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= perfectRadius)
        {
            SpawnJudgementText("PERFECT", Color.cyan);
        }
        else if (distanceToTarget <= greatRadius)
        {
            SpawnJudgementText("GREAT", Color.green);
        }
        else if (distanceToTarget <= goodRadius)
        {
            SpawnJudgementText("GOOD", Color.yellow);
        }
        else
        {
            SpawnJudgementText("BAD", Color.red);
        }

        Destroy(gameObject);
    }

    private void MissNote()
    {
        SpawnJudgementText("MISS", Color.gray);
        Destroy(gameObject);
    }
}