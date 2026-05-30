using UnityEngine;
using TMPro;

public class ResultUI : MonoBehaviour
{
    private RectTransform backgroundPanel;

    private TextMeshProUGUI songNameText;
    private TextMeshProUGUI scoreValueText;
    private TextMeshProUGUI accuracyValueText;
    private TextMeshProUGUI missValueText;

    private void Awake()
    {
        // 🚀 부모 Canvas(UICanvas)가 이미 0.001f 크기로 스케일링되어 있으므로, 
        // 자식인 ResultUI의 로컬 스케일을 (3, 3, 3)으로 설정해야 최종 월드 스케일이 (0.003f)가 되어 
        // MainMenuUI와 완전히 동일한 크기(가로 2.4m x 세로 1.8m)로 렌더링됩니다.
        RectTransform canvasRect = GetComponent<RectTransform>();
        if (canvasRect == null) canvasRect = gameObject.AddComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800f, 600f);
        transform.localScale = new Vector3(3f, 3f, 3f);

        CreateUI();
    }

    private void CreateUI()
    {
        // 1. Create Deep Sleek Night-Blue Card Panel Background
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bgObj.transform.SetParent(transform, false);
        backgroundPanel = bgObj.GetComponent<RectTransform>();
        backgroundPanel.sizeDelta = new Vector2(800f, 600f);
        backgroundPanel.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.14f, 0.95f); // 95% opacity card panel

        // 2. Create Header (SONG COMPLETED)
        VRMenuHelper.CreateText("TitleText", "SONG COMPLETED", 54f, new Vector2(750f, 80f), new Vector2(0f, 200f), Color.white, backgroundPanel);

        // 3. Create Song Name display container (Cyan accent text)
        songNameText = VRMenuHelper.CreateText("SongNameText", "SONG TITLE", 36f, new Vector2(750f, 60f), new Vector2(0f, 130f), new Color(0f, 0.85f, 1f, 0.9f), backgroundPanel);

        // 4. Create Stats grid
        float startY = 40f;

        // -- SCORE --
        VRMenuHelper.CreateText("ScoreLabel", "FINAL SCORE", 20f, new Vector2(300f, 40f), new Vector2(-160f, startY), new Color(0.6f, 0.7f, 0.9f, 0.8f), backgroundPanel);
        scoreValueText = VRMenuHelper.CreateText("ScoreValue", "0", 34f, new Vector2(300f, 50f), new Vector2(-160f, startY - 35f), Color.white, backgroundPanel);

        // -- ACCURACY --
        VRMenuHelper.CreateText("AccuracyLabel", "ACCURACY", 20f, new Vector2(300f, 40f), new Vector2(160f, startY), new Color(0.6f, 0.7f, 0.9f, 0.8f), backgroundPanel);
        accuracyValueText = VRMenuHelper.CreateText("AccuracyValue", "100.00%", 34f, new Vector2(300f, 50f), new Vector2(160f, startY - 35f), new Color(0f, 0.95f, 0.5f, 0.9f), backgroundPanel); // Emerald green for high accuracy

        // -- MISSES --
        VRMenuHelper.CreateText("MissLabel", "TOTAL MISSES", 20f, new Vector2(300f, 40f), new Vector2(0f, startY - 105f), new Color(0.6f, 0.7f, 0.9f, 0.8f), backgroundPanel);
        missValueText = VRMenuHelper.CreateText("MissValue", "0", 34f, new Vector2(300f, 50f), new Vector2(0f, startY - 140f), new Color(1f, 0.25f, 0.25f, 0.9f), backgroundPanel); // Pastel red for missed notes

        // 5. Navigation Buttons
        // Restart Button (Play again)
        VRMenuHelper.CreateButton("RestartButton", "RESTART SONG", 26f, new Vector2(280f, 75f), new Vector2(-160f, -200f), OnRestartClicked, backgroundPanel);

        // Song Select Button (Go back)
        VRMenuHelper.CreateButton("SongSelectButton", "SONG SELECT", 26f, new Vector2(280f, 75f), new Vector2(160f, -200f), OnSongSelectClicked, backgroundPanel);
    }

    /// <summary>
    /// Updates the text fields on the Result Screen with finalized gameplay stats.
    /// </summary>
    public void DisplayResult(string songName, int score, float accuracy, int misses)
    {
        if (songNameText != null) songNameText.text = songName.ToUpper();
        if (scoreValueText != null) scoreValueText.text = score.ToString("N0"); // Formatting with thousands separator
        if (accuracyValueText != null) accuracyValueText.text = $"{accuracy:F2}%"; // Floating-point limit
        if (missValueText != null) missValueText.text = misses.ToString();
    }

    private void OnRestartClicked()
    {
        Debug.Log("[ResultUI] Restarting active song.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartCurrentSong();
        }
    }

    private void OnSongSelectClicked()
    {
        Debug.Log("[ResultUI] Returning to song select screen.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToSongSelect();
        }
    }
}
