using UnityEngine;
using TMPro;

/// <summary>
/// MainMenuUI와 SongSelectUI에서 중복되던 UI 생성 헬퍼 메서드를 통합합니다.
/// World-Space Canvas 기반 VR 메뉴의 텍스트, 버튼 생성에 사용됩니다.
/// </summary>
public static class VRMenuHelper
{
    /// <summary>
    /// TextMeshProUGUI 텍스트 오브젝트를 생성합니다.
    /// </summary>
    public static TextMeshProUGUI CreateText(string objName, string content, float fontSize,
        Vector2 size, Vector2 pos, Color color, Transform parent)
    {
        GameObject textObj = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        TryBindFont(text);
        return text;
    }

    /// <summary>
    /// BoxCollider + VRButton 기반의 World-Space 버튼 오브젝트를 생성합니다.
    /// </summary>
    public static GameObject CreateButton(string objName, string text, float textFontSize,
        Vector2 size, Vector2 pos, System.Action onClick, Transform parent)
    {
        GameObject buttonObj = new GameObject(objName, typeof(RectTransform),
            typeof(UnityEngine.UI.Image), typeof(BoxCollider), typeof(VRButton));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        UnityEngine.UI.Image img = buttonObj.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.12f, 0.18f, 0.32f, 0.8f);

        // 버튼 내부 텍스트
        CreateText(objName + "_Text", text, textFontSize, size, Vector2.zero, Color.white, buttonObj.transform);

        // 물리 레이캐스트 콜라이더
        BoxCollider col = buttonObj.GetComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 10f);
        col.center = Vector3.zero;

        // VRButton 상호작용 설정
        VRButton vrBtn = buttonObj.GetComponent<VRButton>();
        vrBtn.Setup(
            new Color(0.12f, 0.18f, 0.32f, 0.8f),
            new Color(0.18f, 0.42f, 0.95f, 0.95f),
            onClick);

        return buttonObj;
    }

    /// <summary>
    /// UIManager에서 사용 중인 폰트가 있으면 자동 바인딩합니다.
    /// </summary>
    public static void TryBindFont(TextMeshProUGUI text)
    {
        if (UIManager.Instance == null) return;

        TextMeshProUGUI sourceText = UIManager.Instance.GetComponentInChildren<TextMeshProUGUI>(true);
        if (sourceText != null)
        {
            text.font = sourceText.font;
        }
    }
}
