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

    private int score;
    private int combo;
    private int misses;
    private int judgedNotes;
    private int earnedAccuracyPoints;

    public int CurrentScore => score;
    public int CurrentCombo => combo;
    public int MissCount => misses;
    public int JudgedNoteCount => judgedNotes;

    // Lowercase aliases keep older scripts working without exposing runtime data in the Inspector.
    public int currentScore => score;
    public int currentCombo => combo;
    public int missCount => misses;
    public int judgedNoteCount => judgedNotes;

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
        combo++;
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
