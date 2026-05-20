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
    [Tooltip("게임 시작 후 음악이 재생되기까지의 대기 시간 (초 단위). 첫 노트가 갑자기 눈앞에서 생성되는 것을 방지합니다.")]
    public float musicStartDelay = 3f;

    [Tooltip("노트가 판정선에 도달하기 전 미리 생성되는 시간 (초 단위). 이 값에 따라 노트의 낙하 속도가 일정하게 고정됩니다.")]
    public float noteApproachTime = 2.0f;

    [Tooltip("노트가 생성될 위치들. BeatmapData의 laneIndex와 매칭됩니다.")]
    public Transform[] spawnPoints; 
    
    [Tooltip("노트가 향할 타겟들 (판정선 위치). spawnPoints와 1:1로 매칭됩니다.")]
    public Transform[] hitAreaTargets; 
    
    private double dspSongTime; // 정밀한 오디오 시작 시간
    private int nextNoteIndex = 0;
    private bool isPlaying = false;

    // notes를 생성 시간(time - time) 기준으로 정렬하여 사용할 리스트
    private List<NoteData> sortedNotes;

    void Start()
    {
        // currentBeatmap이 인스펙터에 할당되어 있다면 정렬합니다.
        if (currentBeatmap != null)
        {
            PrepareBeatmap(currentBeatmap);
        }
    }

    public void PrepareBeatmap(BeatmapData beatmap)
    {
        currentBeatmap = beatmap;
        sortedNotes = new List<NoteData>(currentBeatmap.notes);
        // 시간에 따라 오름차순으로 정렬
        sortedNotes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public void StartGame()
    {
        if (currentBeatmap == null)
        {
            Debug.LogWarning("BeatmapData가 설정되지 않았습니다.");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource가 연결되지 않았습니다.");
            return;
        }

        if (currentBeatmap.musicClip != null)
        {
            audioSource.clip = currentBeatmap.musicClip;
        }
        
        // 지정된 시간만큼 딜레이 후 재생합니다.
        audioSource.PlayDelayed(musicStartDelay);
        
        // 오디오 시스템의 매우 정밀한 시간을 기록합니다 (지연 방지)
        // 오디오가 나중에 시작되므로 시작 시간도 미래로 설정합니다.
        dspSongTime = AudioSettings.dspTime + musicStartDelay;
        isPlaying = true;
        nextNoteIndex = 0;
    }

    void Update()
    {
        // 게임이 시작되지 않았다면 입력 대기
        if (!isPlaying)
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                StartGame();
            }
            // 키보드 엔터키로도 테스트할 수 있게 예비용으로 둡니다.
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartGame();
            }
            return;
        }

        if (sortedNotes == null || nextNoteIndex >= sortedNotes.Count) return;

        // 현재 노래의 정확한 재생 시간 계산 (audioOffset 반영)
        // dspTime은 double형이므로 float로 변환합니다.
        float currentAudioTime = (float)(AudioSettings.dspTime - dspSongTime) - currentBeatmap.audioOffset;

        // 다음으로 스폰해야 할 노트를 확인
        while (nextNoteIndex < sortedNotes.Count)
        {
            NoteData nextNote = sortedNotes[nextNoteIndex];
            
            // 고정된 Approach Time을 사용하여 스폰 시간 계산
            float spawnTime = nextNote.time - noteApproachTime;

            // 현재 재생 시간이 스폰 시간을 지났다면 노트 생성
            if (currentAudioTime >= spawnTime)
            {
                // 곡이 시작하자마자(혹은 초반에) 나와야 하는 노트는 이미 스폰 시간이 지났을 수 있습니다.
                // 이 경우 노트가 얼만큼 지각했는지(시간)를 계산하여 위치를 보정해줍니다.
                float timePassedSinceSpawn = currentAudioTime - spawnTime;
                
                SpawnNote(nextNote, timePassedSinceSpawn);
                nextNoteIndex++;
            }
            else
            {
                break; // 아직 스폰 시간이 안 된 노트가 나오면 루프 종료
            }
        }
    }

    void SpawnNote(NoteData data, float timePassedSinceSpawn = 0f)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Spawn Points가 설정되지 않았습니다.");
            return;
        }

        // 인덱스 안전성 검사
        int safeLaneIndex = Mathf.Clamp(data.laneIndex, 0, spawnPoints.Length - 1);
        Transform spawnPoint = spawnPoints[safeLaneIndex];
        
        GameObject prefabToSpawn = null;

        // 타입에 맞춰 프리팹 결정
        switch (data.type)
        {
            case NoteType.Punch:
                prefabToSpawn = punchNotePrefab;
                break;
            case NoteType.Rotate:
                prefabToSpawn = rotateNotePrefab;
                break;
            case NoteType.Hold:
                prefabToSpawn = holdNotePrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            // 1. 노트 생성
            Transform targetArea = hitAreaTargets != null && hitAreaTargets.Length > safeLaneIndex 
                ? hitAreaTargets[safeLaneIndex] : null;
            Transform noteParent = GetNoteParent(targetArea, spawnPoint);
            GameObject noteObj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation, noteParent);
            BaseNote baseNote = noteObj.GetComponent<BaseNote>();
            
            if (baseNote != null)
            {
                // 타겟 할당 (인덱스 범위 검사)
                if (targetArea != null)
                {
                    // 2. 노트가 향할 목표 설정 (판정선)
                    baseNote.SetTarget(targetArea);

                    // 3. 고정된 Approach Time에 맞춰 모든 노트의 속도를 동일하게 설정
                    float distance = Vector3.Distance(spawnPoint.position, targetArea.position);
                    
                    if (noteApproachTime > 0.001f)
                    {
                        baseNote.speed = distance / noteApproachTime;
                        
                        // 만약 노트가 예정된 스폰 시간보다 늦게 생성되었다면 (예: 1초에 쳐야하는데 Approach Time이 2초일 경우, -1초에 스폰되어야 함)
                        // 지각한 시간만큼 미리 판정선 쪽으로 이동시켜(땡겨서) 위화감 없이 싱크를 맞춥니다.
                        if (timePassedSinceSpawn > 0f)
                        {
                            baseNote.AdvanceAlongPath(baseNote.speed * timePassedSinceSpawn);
                        }
                    }
                    else
                    {
                        baseNote.speed = 0f;
                        noteObj.transform.position = targetArea.position;
                    }
                }
                else
                {
                    Debug.LogWarning($"Hit Area Target이 설정되지 않았습니다. Lane Index: {safeLaneIndex}");
                }
                
                // 4. 특별한 노트 초기화 설정
                if (data.type == NoteType.Rotate)
                {
                    RotateNote rotateNote = noteObj.GetComponent<RotateNote>();
                    if (rotateNote != null)
                    {
                        // 설정된 타겟 각도로 초기화
                        rotateNote.InitializeSnap(data.targetAngle);
                    }
                }
                else if (data.type == NoteType.Hold)
                {
                    HoldNote holdNote = noteObj.GetComponent<HoldNote>();
                    if (holdNote != null)
                    {
                        // 틱(Tick) 등 HoldNote용 초기화 설정이 필요하다면 여기에 추가
                        // 예: holdNote.SetDuration(data.duration); 
                    }
                }
            }
        }
    }

    private Transform GetNoteParent(Transform targetArea, Transform spawnPoint)
    {
        TrackManager trackManager = null;

        if (targetArea != null)
        {
            trackManager = targetArea.GetComponentInParent<TrackManager>();
        }

        if (trackManager == null && spawnPoint != null)
        {
            trackManager = spawnPoint.GetComponentInParent<TrackManager>();
        }

        return trackManager != null ? trackManager.transform : null;
    }
}
