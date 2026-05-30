using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BeatmapData))]
public class BeatmapDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 필드 그리기 (songName, bpm, audioOffset, musicClip 등)
        DrawDefaultInspector();

        BeatmapData data = (BeatmapData)target;

        GUILayout.Space(20);
        GUILayout.Label("⚡ 웹 에디터 연동 관리자 (CSV)", EditorStyles.boldLabel);

        // 설명 도움말 박스
        EditorGUILayout.HelpBox(
            "웹 채보 에디터(BeatmapEditor.html)에서 작업한 뒤 [📋 클립보드 복사] 버튼을 누르고,\n" +
            "위의 'Raw Csv Data' 텍스트 영역에 붙여넣은 뒤 아래 [가져오기] 버튼을 클릭하세요.\n\n" +
            "반대로 유니티 내부 채보를 수정했다면 [내보내기] 버튼을 눌러 복사한 뒤 웹 에디터에 붙여넣어 수정할 수 있습니다.", 
            MessageType.Info
        );

        GUILayout.BeginHorizontal();

        // 1. 가져오기 (Import) 버튼
        GUI.backgroundColor = new Color(0.2f, 0.8f, 1f, 1f); // 시안 블루 톤 강조
        if (GUILayout.Button("📥 웹 CSV ➔ 유니티 채보 변환 (Import)", GUILayout.Height(35)))
        {
            if (string.IsNullOrEmpty(data.rawCsvData))
            {
                EditorUtility.DisplayDialog("가져오기 실패", "Raw Csv Data 영역에 CSV 텍스트가 비어 있습니다!\n웹 에디터에서 복사한 데이터를 붙여넣어 주세요.", "확인");
            }
            else
            {
                if (EditorUtility.DisplayDialog("채보 가져오기", "현재 유니티에 등록된 모든 노트 데이터가 지워지고 CSV 데이터로 덮어씌워집니다. 진행하시겠습니까?", "예", "아니오"))
                {
                    ImportCsvToNotes(data);
                }
            }
        }

        // 2. 내보내기 (Export) 버튼
        GUI.backgroundColor = new Color(1f, 0.2f, 0.5f, 1f); // 핑크 톤 강조
        if (GUILayout.Button("💾 유니티 ➔ 웹 CSV 변환 및 복사 (Export)", GUILayout.Height(35)))
        {
            ExportNotesToCsv(data);
        }

        GUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white; // 색상 리셋
    }

    /// <summary>
    /// CSV 텍스트를 파싱하여 BeatmapData의 notes 리스트에 채워 넣습니다.
    /// 파싱 로직은 BeatmapData.ParseCsvToNotes()에 통합되어 있습니다.
    /// </summary>
    private void ImportCsvToNotes(BeatmapData data)
    {
        data.ParseCsvToNotes();

        // musicClip이 지정되지 않은 상태라면, INFX - Firework 오디오 클립을 검색하여 자동 할당
        if (data.musicClip == null)
        {
            string assetPath = GameConstants.DefaultAudioClipPath;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            
            if (clip == null)
            {
                string[] guids = AssetDatabase.FindAssets(GameConstants.DefaultAudioClipSearchQuery);
                if (guids != null && guids.Length > 0)
                {
                    string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(foundPath);
                }
            }
            
            if (clip != null)
            {
                data.musicClip = clip;
            }
        }

        // 저장 처리
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "가져오기 성공", 
            $"성공적으로 {data.notes.Count}개의 노트를 변환하여 적용했습니다!\n\n" +
            $"• 곡 이름: {data.songName}\n" +
            $"• BPM: {data.bpm}\n" +
            $"• 오디오 오프셋: {data.audioOffset}초", 
            "확인"
        );
    }

    /// <summary>
    /// BeatmapData의 notes 데이터를 역으로 CSV 텍스트로 만들어 클립보드에 담아줍니다.
    /// </summary>
    private void ExportNotesToCsv(BeatmapData data)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# 곡 이름: {data.songName}");
            sb.AppendLine($"# BPM: {data.bpm}");
            sb.AppendLine($"# 오디오 오프셋: {data.audioOffset}");
            sb.AppendLine($"# 박자(Beat), 레인(LaneIndex), 종류(Type), [Rotate:각도 / Hold:지속박수]");

            if (data.notes != null)
            {
                foreach (var note in data.notes)
                {
                    switch (note.type)
                    {
                        case NoteType.Punch:
                            sb.AppendLine($"{note.beat:F2}, {note.laneIndex}, Punch");
                            break;
                        case NoteType.Rotate:
                            sb.AppendLine($"{note.beat:F2}, {note.laneIndex}, Rotate, {note.targetAngle:F0}");
                            break;
                        case NoteType.Hold:
                            sb.AppendLine($"{note.beat:F2}, {note.laneIndex}, Hold, {note.duration:F2}");
                            break;
                    }
                }
            }

            string csv = sb.ToString();

            data.rawCsvData = csv;

            // 클립보드 복사
            GUIUtility.systemCopyBuffer = csv;

            // 저장 처리
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "내보내기 성공", 
                $"현재 유니티 채보 데이터({data.notes.Count}개)를 성공적으로 CSV 텍스트로 변환하고 시스템 클립보드에 복사했습니다!\n\n" +
                $"웹 에디터 하단의 CSV 연동란에 붙여넣기[Ctrl+V]하여 바로 이어서 작업할 수 있습니다.", 
                "확인"
            );
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("내보내기 실패", $"변환 도중 오류가 발생했습니다.\n에러 내용: {ex.Message}", "확인");
        }
    }
}
