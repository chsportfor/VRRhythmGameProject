using UnityEngine;

public class HoldNote : BaseNote
{
    private bool isLeftHandIn = false;
    private bool isRightHandIn = false;

    public float tickRate = 0.1f;
    private float holdTimer = 0f;

    private int holdTickCount = 0;
    public int requiredTicks = 5;

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
        if (holdTimer >= tickRate)
        {
            holdTickCount++;
            SpawnJudgementText("GUARD", Color.blue);
            holdTimer = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand")) isLeftHandIn = true;
        if (other.CompareTag("RightHand")) isRightHandIn = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LeftHand")) isLeftHandIn = false;
        if (other.CompareTag("RightHand")) isRightHandIn = false;

        // 🚀 핵심 수정: MissArea를 '완전히 빠져나갔을 때' 최종 판정 후 파괴
        if (other.CompareTag("MissArea"))
        {
            if (holdTickCount >= requiredTicks)
            {
                SpawnJudgementText("PERFECT HOLD!", Color.cyan);
            }
            else if (holdTickCount > 0)
            {
                SpawnJudgementText("POOR HOLD", Color.yellow);
            }
            else
            {
                SpawnJudgementText("MISS!", Color.red);
            }

            Destroy(gameObject);
        }
    }
}