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
    [SerializeField] private GameObject checkmark;
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

        if (checkmark != null)
            checkmark.SetActive(false);
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
