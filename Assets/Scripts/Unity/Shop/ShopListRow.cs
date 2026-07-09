using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopBook 좌측 리스트의 한 행.
/// 데이터 타입(재료/업그레이드)에 무관하게 공통 표시: 아이콘, 이름, 좌측 보조정보, 우측 보조정보.
/// 클릭 시 매니저 콜백.
/// </summary>
[RequireComponent(typeof(Button))]
public class ShopListRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI leftSubLabel;
    [SerializeField] private TextMeshProUGUI rightSubLabel;
    [SerializeField] private Image selectionOverlay;
    [SerializeField] private Button button;

    public object UserData { get; private set; }
    public System.Action<ShopListRow> OnClicked;

    // Awake에 스냅샷: icon 없는 행은 라벨을 왼쪽으로 당겨 표시하기 위해 원 값과 no-icon 값을 스위칭.
    private const float LeftInsetWithoutIcon = 20f;
    private float _nameLabelOrigLeft;
    private float _leftSubLabelOrigLeft;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(this));
        if (nameLabel != null) _nameLabelOrigLeft = nameLabel.rectTransform.offsetMin.x;
        if (leftSubLabel != null) _leftSubLabelOrigLeft = leftSubLabel.rectTransform.offsetMin.x;
        SetSelected(false);
    }

    public void SetData(Sprite icon, string name, string leftSub, string rightSub, object userData)
    {
        bool hasIcon = icon != null;
        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(hasIcon);
            if (hasIcon) { iconImage.sprite = icon; iconImage.preserveAspect = true; }
        }
        // icon 없으면 라벨을 왼쪽 끝으로 당김 (창고/농장 업그레이드용).
        SetLabelLeftInset(nameLabel, hasIcon ? _nameLabelOrigLeft : LeftInsetWithoutIcon);
        SetLabelLeftInset(leftSubLabel, hasIcon ? _leftSubLabelOrigLeft : LeftInsetWithoutIcon);

        if (nameLabel != null) nameLabel.text = name;
        if (leftSubLabel != null) leftSubLabel.text = leftSub;
        if (rightSubLabel != null) rightSubLabel.text = rightSub;
        UserData = userData;
    }

    private static void SetLabelLeftInset(TextMeshProUGUI label, float inset)
    {
        if (label == null) return;
        var rt = label.rectTransform;
        var min = rt.offsetMin;
        min.x = inset;
        rt.offsetMin = min;
    }

    public void SetSelected(bool selected)
    {
        if (selectionOverlay != null) selectionOverlay.enabled = selected;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
