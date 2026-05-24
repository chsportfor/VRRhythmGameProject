using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoteSpawner))]
public class NoteSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 기본 인스펙터 드로우
        DrawDefaultInspector();

        NoteSpawner spawner = (NoteSpawner)target;

        GUILayout.Space(15);
        GUILayout.Label("🎮 테스트 플레이 보조 도구", EditorStyles.boldLabel);

        // 설명 도움말 박스
        EditorGUILayout.HelpBox(
            "유니티 상단 플레이(▶) 버튼을 누른 후, 아래 버튼을 클릭하면\n" +
            "INFX - Firework 음악과 함께 에디터로 임포트한 채보 스폰이 즉시 시작됩니다.\n\n" +
            "※ 컴파일 또는 컴포넌트 뷰 시, AudioSource 및 INFX - Firework 음원이 자동으로 감지되어 스포너에 등록됩니다.", 
            MessageType.Info
        );

        GUILayout.Space(5);

        // 플레이 중일 때만 동작하도록 분기 처리
        if (Application.isPlaying)
        {
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f, 1f); // 선명한 초록 네온 컬러 강조
            if (GUILayout.Button("▶ 게임 시작 및 채보 테스트 (Start Game)", GUILayout.Height(40)))
            {
                spawner.StartGame();
            }
        }
        else
        {
            GUI.enabled = false; // 플레이 중이 아닐 때는 버튼 비활성화 상태 묘사
            GUILayout.Button("▶ 게임 시작 및 채보 테스트 (플레이 상태에서 활성화)", GUILayout.Height(40));
            GUI.enabled = true;
        }

        GUI.backgroundColor = Color.white; // 배경 리셋
    }
}
