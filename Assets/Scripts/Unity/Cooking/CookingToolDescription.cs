using TMPro;
using UnityEngine;

/// <summary>
/// 호버 툴팁 패널 (재료/요리도구 공용). title + body 두 줄.
/// 호출자가 prefix까지 만들어서 통째로 넘긴다.
/// </summary>
public class CookingToolDescription : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    public void SetTexts(string title, string body)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
