using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteSpawner : MonoBehaviour
{
    [Header("Beatmap & Audio")]
    public BeatmapData currentBeatmap;
    public AudioSource audioSource;

    [Header("Note Prefabs")]
    public GameObject punchNotePrefab;
    public GameObject rotateNotePrefab;
    public GameObject holdNotePrefab;

    [Header("Spawn Settings")]
    [Tooltip("버튼 입력 후 음악 재생까지의 대기 시간(초). 노트가 미리 날아오는 시간을 확보합니다.")]
    public float musicStartDelay = 3f;

    [Tooltip("노트가 판정선 도달 전 미리 생성되는 시간(초).\n★ musicStartDelay 와 같은 값으로 설정하면 버튼 누르는 순간 노트가 스폰되어\n  3초 후 음악 시작과 첫 노트 도달이 완벽하게 동기화됩니다.")]
    public float noteApproachTime = 3f;

    // ─── 스폰 포인트 참조 ───────────────────────────────────────────
    // NoteSpawner는 TrackManager의 spawners / hitZones 배열을 직접 참조합니다.
    // 인스펙터에서 별도 배열을 만들지 않아도 됩니다.
    // TrackManager.Instance.spawners[laneIndex] = 스폰 위치
    // TrackManager.Instance.hitZones[laneIndex]  = 판정선 위치
    // ──────────────────────────────────────────────────────────────

    private double dspStartTime;   // AudioSettings.dspTime 기준 게임 시작 절대 시간
    private int    nextNoteIndex = 0;
    private bool   isPlaying    = false;

    private List<NoteData> sortedNotes;

    // ───────────────────────────── 초기화 ────────────────────────────

    void Start()
    {
        // 씬 시작 시 자동 재생 완벽 차단
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }

        if (currentBeatmap != null)
            PrepareBeatmap(currentBeatmap);
    }

    public void PrepareBeatmap(BeatmapData beatmap)
    {
        currentBeatmap = beatmap;

        // rawCsvData에 CSV가 있지만 notes 리스트가 비어있으면 자동 파싱
        beatmap.EnsureNotesReady();

        sortedNotes    = new List<NoteData>(beatmap.notes);
        sortedNotes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    // ───────────────────────────── 게임 시작 ─────────────────────────

    public void StartGame()
    {
        if (currentBeatmap == null)
        {
            Debug.LogWarning("[NoteSpawner] BeatmapData가 없습니다.");
            return;
        }
        if (audioSource == null)
        {
            Debug.LogWarning("[NoteSpawner] AudioSource가 없습니다.");
            return;
        }

        // 채보 노트가 정렬되어 있는지 보장
        if (sortedNotes == null)
            PrepareBeatmap(currentBeatmap);

        // 음악 클립 교체 (BeatmapData에 musicClip이 있으면)
        if (currentBeatmap.musicClip != null)
            audioSource.clip = currentBeatmap.musicClip;

        audioSource.playOnAwake = false;
        audioSource.Stop();

        // ─── 핵심: PlayScheduled로 정확히 musicStartDelay 초 후에 재생 ───
        // dspTime은 오디오 하드웨어 기준의 절대 시간이므로 프레임 드랍 영향을 받지 않습니다.
        dspStartTime = AudioSettings.dspTime + musicStartDelay;
        audioSource.PlayScheduled(dspStartTime);
        // ────────────────────────────────────────────────────────────────

        // UI 카운트다운 시작
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartCountdown(musicStartDelay);
        }

        isPlaying     = true;
        nextNoteIndex = 0;

        Debug.Log($"[NoteSpawner] 시작! {musicStartDelay}초 후 음악 재생. 비트맵 싱크 시작.");
    }

    public void ResetGame()
    {
        isPlaying = false;
        nextNoteIndex = 0;

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Destroy all spawned note objects in the scene
        BaseNote[] activeNotes = FindObjectsByType<BaseNote>(FindObjectsSortMode.None);
        foreach (BaseNote note in activeNotes)
        {
            if (note != null && note.gameObject != null)
            {
                Destroy(note.gameObject);
            }
        }
    }

    // ───────────────────────────── 매 프레임 ─────────────────────────

    void Update()
    {
        if (!isPlaying)
        {
            // Only allow starting if in the Playing state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                return;

            // 버튼 입력 감지
            if (OVRInput.GetDown(OVRInput.Button.One))
                StartGame();

            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                StartGame();

            return;
        }

        if (sortedNotes == null || nextNoteIndex >= sortedNotes.Count)
            return;

        // ─── 핵심 싱크 계산 ─────────────────────────────────────────
        // dspStartTime = 음악이 재생될 절대 dsp 시각
        // AudioSettings.dspTime = 현재 절대 dsp 시각
        //
        // currentGameTime < 0  : 아직 음악 시작 전 (노트 사전 스폰 구간)
        // currentGameTime >= 0 : 음악 재생 중
        //
        // audioOffset는 '음악 시작 기준으로 노트를 앞당기거나 뒤로 밀 때' 사용합니다.
        float currentGameTime = (float)(AudioSettings.dspTime - dspStartTime)
                                - currentBeatmap.audioOffset;

        // ★ 버튼을 누르기 이전(currentGameTime < -musicStartDelay)에는
        //   노트를 절대 스폰하지 않습니다. 이 가드가 없으면 이론상 불가능하지만
        //   dspTime 정밀도 이슈로 아주 조금 일찍 스폰되는 경우를 방지합니다.
        if (currentGameTime < -(musicStartDelay + 0.05f))
            return;
        // ────────────────────────────────────────────────────────────

        while (nextNoteIndex < sortedNotes.Count)
        {
            NoteData note = sortedNotes[nextNoteIndex];

            // 스폰 시각 = 판정 시각 - 사전 접근 시간
            float spawnTime = note.time - noteApproachTime;

            if (currentGameTime >= spawnTime)
            {
                float lateBy = currentGameTime - spawnTime;
                SpawnNote(note, lateBy);
                nextNoteIndex++;
            }
            else
            {
                break;
            }
        }

        // Check if all notes are spawned and audio has finished playing to trigger OnSongFinished
        if (isPlaying && nextNoteIndex >= sortedNotes.Count && audioSource != null && !audioSource.isPlaying && AudioSettings.dspTime > dspStartTime)
        {
            isPlaying = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSongFinished();
            }
        }
    }

    // ───────────────────────────── 노트 스폰 ─────────────────────────

    void SpawnNote(NoteData data, float lateBy = 0f)
    {
        // TrackManager가 없으면 스폰 불가
        if (TrackManager.Instance == null)
        {
            Debug.LogWarning("[NoteSpawner] TrackManager.Instance가 없습니다.");
            return;
        }

        Transform[] spawners  = TrackManager.Instance.spawners;
        Transform[] hitZones  = TrackManager.Instance.hitZones;

        if (spawners == null || hitZones == null)
        {
            Debug.LogWarning("[NoteSpawner] TrackManager의 spawners 또는 hitZones가 비어있습니다.");
            return;
        }

        int laneIdx   = Mathf.Clamp(data.laneIndex, 0, spawners.Length - 1);
        Transform sp  = spawners[laneIdx];
        Transform hit = hitZones.Length > laneIdx ? hitZones[laneIdx] : null;

        GameObject prefab = null;
        switch (data.type)
        {
            case NoteType.Punch:  prefab = punchNotePrefab;  break;
            case NoteType.Rotate: prefab = rotateNotePrefab; break;
            case NoteType.Hold:   prefab = holdNotePrefab;   break;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[NoteSpawner] 프리팹이 할당되지 않았습니다. 타입: {data.type}");
            return;
        }

        // 부모는 TrackManager (회전 동기화를 위해 반드시 필요)
        Transform parent = TrackManager.Instance.transform;
        GameObject noteObj = Instantiate(prefab, sp.position, sp.rotation, parent);
        BaseNote baseNote  = noteObj.GetComponent<BaseNote>();

        if (baseNote == null) return;

        if (hit != null)
        {
            baseNote.SetTarget(hit);

            float dist = Vector3.Distance(sp.position, hit.position);
            if (noteApproachTime > 0.001f)
            {
                baseNote.speed = dist / noteApproachTime;

                // 지각만큼 미리 당겨서 싱크 보정
                if (lateBy > 0f)
                    baseNote.AdvanceAlongPath(baseNote.speed * lateBy);
            }
            else
            {
                baseNote.speed = 0f;
                noteObj.transform.position = hit.position;
            }
        }

        // 노트 타입별 추가 초기화
        if (data.type == NoteType.Rotate)
        {
            RotateNote rn = noteObj.GetComponent<RotateNote>();
            rn?.InitializeSnap(data.targetAngle);
        }
        else if (data.type == NoteType.Hold)
        {
            HoldNote hn = noteObj.GetComponent<HoldNote>();
            if (hn != null)
            {
                // Convert duration from beats to real-world seconds based on BPM
                float bpm = currentBeatmap != null ? currentBeatmap.bpm : 120f;
                float durationInSeconds = data.duration * (60f / bpm);
                hn.InitializeHold(durationInSeconds, baseNote.speed);
            }
        }
    }

    // ───────────────────── 에디터 자동화 (에디터 전용) ────────────────

#if UNITY_EDITOR
    // Reset()은 컴포넌트를 처음 추가하거나 우클릭 Reset 시 한 번만 호출됩니다.
    // AddComponent는 여기서만 안전합니다.
    private void Reset()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        TryBindDefaultClip();
    }

    // OnValidate는 인스펙터 값 변경, 직렬화/역직렬화 등 여러 번 호출됩니다.
    // AddComponent를 여기서 호출하면 중복 생성되므로 GetComponent만 합니다.
    private void OnValidate()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void TryBindDefaultClip()
    {
        if (audioSource == null || audioSource.clip != null) return;

        string path = "Assets/GameSystem/AudioReaction/SoundSource/02 INFX - Firework (feat. NC.A).wav";
        AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        if (clip == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("INFX - Firework t:AudioClip");
            if (guids != null && guids.Length > 0)
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (clip != null)
        {
            audioSource.clip = clip;
            Debug.Log($"[NoteSpawner] {clip.name} 자동 바인딩 완료.");
        }
    }
#endif
}
