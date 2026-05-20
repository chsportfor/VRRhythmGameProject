// 5. PunchNote.cs
using UnityEngine;

public class PunchNote : BaseNote
{
    public float perfectRadius = 0.15f;
    public float greatRadius = 0.3f;
    public float goodRadius = 0.5f;
    public JudgementText judgementText;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public Vector3 hitEffectOffset = Vector3.zero;
    public float hitEffectScale = 1f;
    public float hitEffectLifetime = 2f;

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
        int scorePoints;

        if (distanceToTarget <= perfectRadius)
        {
            scorePoints = 100;
            SpawnJudgementText("PERFECT", Color.cyan);
        }
        else if (distanceToTarget <= greatRadius)
        {
            scorePoints = 80;
            SpawnJudgementText("GREAT", Color.green);
        }
        else if (distanceToTarget <= goodRadius)
        {
            scorePoints = 50;
            SpawnJudgementText("GOOD", Color.yellow);
        }
        else
        {
            scorePoints = 20;
            SpawnJudgementText("BAD", Color.red);
        }

        RegisterHitScore(scorePoints);
        SpawnEffect(hitEffectPrefab, transform.position + hitEffectOffset, hitEffectScale, hitEffectLifetime);
        Destroy(gameObject);
    }

    private void MissNote()
    {
        SpawnJudgementText("MISS", Color.gray);
        RegisterMiss();
        Destroy(gameObject);
    }
}
