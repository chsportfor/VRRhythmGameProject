using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class BeatmapCsvImporter : AssetPostprocessor
{
    // 감지할 폴더 및 파일 패턴 지정
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            // Assets/GameSystem/Spawner/Beatmaps/ 폴더 하위의 .csv 파일만 감지
            if (str.Replace("\\", "/").StartsWith(GameConstants.BeatmapCsvFolder) && str.EndsWith(".csv"))
            {
                ImportCsv(str);
            }
        }
    }

    private static void ImportCsv(string csvPath)
    {
        try
        {
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            string csvData = File.ReadAllText(csvPath);

            // ScriptableObject가 저장될 데이터 폴더
            string dataFolder = GameConstants.BeatmapDataFolder;
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            string assetPath = $"{dataFolder}/{fileName}.asset";
            
            // 기존 에셋 로드 시도
            BeatmapData data = AssetDatabase.LoadAssetAtPath<BeatmapData>(assetPath);
            bool isNew = false;

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BeatmapData>();
                isNew = true;
            }

            // CSV 데이터 바인딩 및 파싱 수행
            data.rawCsvData = csvData;
            data.songName = fileName;
            data.ParseCsvToNotes();

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, assetPath);
                Debug.Log($"[BeatmapCsvImporter] 새로운 채보 에셋 자동 생성 완료: '{assetPath}'");
            }
            else
            {
                EditorUtility.SetDirty(data);
                Debug.Log($"[BeatmapCsvImporter] 기존 채보 에셋 자동 업데이트 완료: '{assetPath}'");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BeatmapCsvImporter] CSV 자동 임포트 중 에러 발생: {ex.Message}");
        }
    }
}
