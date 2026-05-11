using System;
using System.Collections.Generic;
using UnityEngine;

public enum NoteType
{
    Punch,
    Rotate,
    Hold
}

[Serializable]
public class NoteData
{
    [Header("타이밍 데이터")]
    [Tooltip("true일 경우 입력한 Beat와 곡의 BPM을 바탕으로 Time을 자동 계산합니다.")]
    public bool useBeat = true;

    [Tooltip("박자(Beat) 단위로 소환 시간을 설정합니다. (예: 0=첫박자, 1=두번째박자, 1.5=엇박)")]
    public float beat;

    [Tooltip("음악 시작 후 판정선에 닿아야 하는 시간 (초 단위). useBeat가 true면 자동 갱신됩니다.")]
    public float time;

    [Header("공통 데이터")]
    [Tooltip("노트가 소환될 라인/위치 인덱스 (TrackManager의 위치 또는 Spawner의 SpawnPoints 인덱스)")]
    public int laneIndex;

    [Tooltip("노트의 종류")]
    public NoteType type;

    // --- 아래는 노트 종류에 따라 선택적으로 쓰이는 데이터들입니다 ---

    [Header("Rotate Note 전용")]
    [Tooltip("회전해야 하는 각도 (예: 90, -90)")]
    public float targetAngle;

    [Header("Hold Note 전용")]
    [Tooltip("얼마나 오랫동안 양손을 유지해야 하는지 (초 단위)")]
    public float duration;
}

// 3. 곡 하나의 전체 채보 정보를 담는 ScriptableObject 입니다.
[CreateAssetMenu(fileName = "New Beatmap", menuName = "RhythmGame/Beatmap Data")]
public class BeatmapData : ScriptableObject
{
    public string songName;           // 곡 이름
    public float bpm = 120f;          // 곡의 BPM
    public float audioOffset = 0f;    // 음악 재생과 노트의 싱크를 맞출 오프셋 시간
    public AudioClip musicClip;       // 재생할 음악 파일

    [Space(10)]
    public List<NoteData> notes = new List<NoteData>();

    private void OnValidate()
    {
        // 에디터에서 값이 변경될 때마다 자동 계산해줍니다.
        if (bpm > 0)
        {
            // BPM(Beats Per Minute): 1분(60초)당 비트 수.
            // 60 / BPM = 1비트당 초(Seconds Per Beat)
            float secondsPerBeat = 60f / bpm;

            if (notes != null)
            {
                foreach (var note in notes)
                {
                    if (note.useBeat)
                    {
                        // 설정한 비트에 따라 time을 자동으로 계산
                        note.time = note.beat * secondsPerBeat;
                    }
                }
            }
        }
    }
}
