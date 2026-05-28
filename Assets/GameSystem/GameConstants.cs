/// <summary>
/// 프로젝트 전역에서 공유하는 경로 및 설정 상수를 정의합니다.
/// 런타임 + 에디터 코드 양쪽에서 참조할 수 있습니다.
/// </summary>
public static class GameConstants
{
    // 기본 오디오 클립 경로 (에디터 전용 자동 바인딩에 사용)
    public const string DefaultAudioClipPath =
        "Assets/GameSystem/AudioReaction/SoundSource/02 INFX - Firework (feat. NC.A).wav";
    public const string DefaultAudioClipSearchQuery = "INFX - Firework";

    // 채보 관련 경로
    public const string BeatmapCsvFolder = "Assets/GameSystem/Spawner/Beatmaps/";
    public const string BeatmapDataFolder = "Assets/GameSystem/Spawner/Data";
}
