using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TimeSection에 붙는 스크립트.
/// 클릭하면 체크/해제를 토글합니다.
/// Checkmark 자식 오브젝트를 자동으로 찾아 설정합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class PhaseCheckToggle : MonoBehaviour
{
    private GameObject checkmark;
    private bool isChecked;
    private Button button;

    public bool IsChecked => isChecked;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Toggle);

        // 루트 Image를 투명하게 (클릭 영역용)
        var img = GetComponent<Image>();
        if (img != null)
            img.color = new Color(0, 0, 0, 0);

        // Label/Checkmark 자동 탐색
        var label = transform.Find("Label");
        if (label != null)
        {
            var cm = label.Find("Checkmark");
            if (cm != null)
            {
                checkmark = cm.gameObject;

                // TMP 텍스트 설정
                var tmp = checkmark.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "\u2713";
                    tmp.fontSize = 24;
                    tmp.color = Color.black;
                    tmp.alignment = TextAlignmentOptions.Center;
                }

                // RectTransform 크기 설정
                var rt = checkmark.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(30, 20);
                }

                checkmark.SetActive(false);
            }
        }
    }

    public void Toggle()
    {
        SetChecked(!isChecked);
    }

    public void SetChecked(bool value)
    {
        isChecked = value;
        if (checkmark != null)
            checkmark.SetActive(value);
    }
}
