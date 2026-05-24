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
    [TextArea(5, 15)]
    [Tooltip("웹 에디터에서 복사한 CSV 데이터를 여기에 붙여넣을 수 있습니다.")]
    public string rawCsvData;

    [Space(10)]
    public List<NoteData> notes = new List<NoteData>();

    private void OnValidate()
    {
        // 에디터에서 값이 변경될 때마다 자동 계산해줍니다.
        RecalculateNoteTimes();
    }

    /// <summary>
    /// BPM 기반으로 모든 노트의 time을 beat로부터 재계산합니다.
    /// </summary>
    public void RecalculateNoteTimes()
    {
        if (bpm <= 0) return;

        float secondsPerBeat = 60f / bpm;

        if (notes != null)
        {
            foreach (var note in notes)
            {
                if (note.useBeat)
                {
                    note.time = note.beat * secondsPerBeat;
                }
            }
        }
    }

    /// <summary>
    /// rawCsvData 필드에 담긴 CSV 텍스트를 파싱하여 notes 리스트를 채웁니다.
    /// 에디터의 Import 버튼을 누르지 않아도, 런타임에서 자동으로 호출됩니다.
    /// </summary>
    public void ParseCsvToNotes()
    {
        if (string.IsNullOrEmpty(rawCsvData))
        {
            Debug.LogWarning($"[BeatmapData] '{songName}' — rawCsvData가 비어있어 파싱할 수 없습니다.");
            return;
        }

        try
        {
            string[] lines = rawCsvData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<NoteData> parsedNotes = new List<NoteData>();

            float parsedBpm = bpm;
            float parsedOffset = audioOffset;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 주석 라인 처리 (BPM, 오프셋, 곡 이름 추출)
                if (line.StartsWith("#") || line.StartsWith("//"))
                {
                    if (line.Contains("BPM:"))
                    {
                        string[] parts = line.Split(new[] { "BPM:" }, StringSplitOptions.None);
                        if (parts.Length > 1 && float.TryParse(parts[1].Trim(), out float tempBpm))
                        {
                            parsedBpm = tempBpm;
                        }
                    }
                    else if (line.Contains("오디오 오프셋:"))
                    {
                        string[] parts = line.Split(new[] { "오디오 오프셋:" }, StringSplitOptions.None);
                        if (parts.Length > 1 && float.TryParse(parts[1].Trim(), out float tempOffset))
                        {
                            parsedOffset = tempOffset;
                        }
                    }
                    else if (line.Contains("곡 이름:"))
                    {
                        string[] parts = line.Split(new[] { "곡 이름:" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            songName = parts[1].Trim();
                        }
                    }
                    continue;
                }

                // 데이터 파싱
                string[] tokens = line.Split(',');
                if (tokens.Length < 3) continue;

                if (!float.TryParse(tokens[0].Trim(), out float beat)) continue;
                if (!int.TryParse(tokens[1].Trim(), out int laneIndex)) continue;
                string typeStr = tokens[2].Trim().ToLower();

                NoteData note = new NoteData();
                note.useBeat = true;
                note.beat = beat;
                note.laneIndex = laneIndex;

                if (typeStr.StartsWith("p")) // Punch
                {
                    note.type = NoteType.Punch;
                }
                else if (typeStr.StartsWith("r")) // Rotate
                {
                    note.type = NoteType.Rotate;
                    if (tokens.Length >= 4 && float.TryParse(tokens[3].Trim(), out float angle))
                    {
                        note.targetAngle = angle;
                    }
                    else
                    {
                        note.targetAngle = 90f;
                    }
                }
                else if (typeStr.StartsWith("h")) // Hold
                {
                    note.type = NoteType.Hold;
                    if (tokens.Length >= 4 && float.TryParse(tokens[3].Trim(), out float duration))
                    {
                        note.duration = duration;
                    }
                    else
                    {
                        note.duration = 1.0f;
                    }
                }
                else
                {
                    continue; // 알 수 없는 타입 건너뜀
                }

                parsedNotes.Add(note);
            }

            // BPM/오프셋 적용
            bpm = parsedBpm;
            audioOffset = parsedOffset;

            // 박자 오름차순 정렬
            parsedNotes.Sort((a, b) => a.beat.CompareTo(b.beat));

            // 초 단위 시간(time) 일괄 업데이트
            float spb = 60f / (bpm > 0 ? bpm : 120f);
            foreach (var note in parsedNotes)
            {
                note.time = note.beat * spb;
            }

            notes = parsedNotes;

            Debug.Log($"[BeatmapData] '{songName}' CSV 파싱 완료: {notes.Count}개 노트, BPM={bpm}, offset={audioOffset}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BeatmapData] CSV 파싱 에러: {ex.Message}");
        }
    }

    /// <summary>
    /// notes 리스트가 비어있고 rawCsvData가 있으면 자동으로 파싱합니다.
    /// NoteSpawner.PrepareBeatmap()에서 호출됩니다.
    /// </summary>
    public void EnsureNotesReady()
    {
        if ((notes == null || notes.Count == 0) && !string.IsNullOrEmpty(rawCsvData))
        {
            Debug.Log("[BeatmapData] notes가 비어있어 rawCsvData에서 자동 파싱합니다.");
            ParseCsvToNotes();
        }
    }
}
