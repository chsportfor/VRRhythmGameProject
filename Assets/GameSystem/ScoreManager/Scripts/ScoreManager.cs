using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const int MaxAccuracyPointsPerNote = 100;

    public static ScoreManager Instance { get; private set; }

    public event Action<int> OnScoreChanged;
    public event Action<int> OnComboChanged;
    public event Action<int> OnMissChanged;
    public event Action<float> OnAccuracyChanged;
    public event Action<string, Color> OnJudgementChanged;

    private int score;
    private int combo;
    private int misses;
    private int judgedNotes;
    private int earnedAccuracyPoints;

    public int CurrentScore => score;
    public int CurrentCombo => combo;
    public int MissCount => misses;
    public int JudgedNoteCount => judgedNotes;



    public float Accuracy
    {
        get
        {
            if (judgedNotes <= 0)
            {
                return 100f;
            }

            float maxPoints = judgedNotes * MaxAccuracyPointsPerNote;
            return Mathf.Clamp01(earnedAccuracyPoints / maxPoints) * 100f;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TriggerJudgement(string text, Color color)
    {
        OnJudgementChanged?.Invoke(text, color);
    }

    public void ResetScore()
    {
        score = 0;
        combo = 0;
        misses = 0;
        judgedNotes = 0;
        earnedAccuracyPoints = 0;
        BroadcastStats();
    }

    public void AddScore(int points)
    {
        RegisterHit(points);
    }

    public void RegisterHit(int scorePoints)
    {
        RegisterHit(scorePoints, scorePoints);
    }

    public void RegisterHit(int scorePoints, int accuracyPoints)
    {
        score += Mathf.Max(0, scorePoints);
        
        // Good(50점) 판정 이상일 때만 콤보를 증가시키고, 그 미만(Bad: 20점, Poor: 40점 등)일 때는 콤보를 리셋합니다.
        if (accuracyPoints >= 50)
        {
            combo++;
        }
        else
        {
            combo = 0;
        }
        
        judgedNotes++;
        earnedAccuracyPoints += Mathf.Clamp(accuracyPoints, 0, MaxAccuracyPointsPerNote);

        OnScoreChanged?.Invoke(score);
        OnComboChanged?.Invoke(combo);
        OnAccuracyChanged?.Invoke(Accuracy);
    }

    public void RegisterMiss()
    {
        misses++;
        judgedNotes++;
        combo = 0;

        OnComboChanged?.Invoke(combo);
        OnMissChanged?.Invoke(misses);
        OnAccuracyChanged?.Invoke(Accuracy);
    }

    public void BreakCombo()
    {
        combo = 0;
        OnComboChanged?.Invoke(combo);
    }

    public void BroadcastStats()
    {
        OnScoreChanged?.Invoke(score);
        OnComboChanged?.Invoke(combo);
        OnMissChanged?.Invoke(misses);
        OnAccuracyChanged?.Invoke(Accuracy);
    }
}
