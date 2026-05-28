using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopBook 좌측 리스트의 한 행.
/// 데이터 타입(재료/업그레이드)에 무관하게 공통 표시: 아이콘, 이름, 좌측 보조정보, 우측 보조정보.
/// 클릭 시 매니저 콜백.
/// </summary>
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

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => OnClicked?.Invoke(this));
        SetSelected(false);
    }

    public void SetData(Sprite icon, string name, string leftSub, string rightSub, object userData)
    {
        if (iconImage != null) { iconImage.sprite = icon; iconImage.preserveAspect = true; }
        if (nameLabel != null) nameLabel.text = name;
        if (leftSubLabel != null) leftSubLabel.text = leftSub;
        if (rightSubLabel != null) rightSubLabel.text = rightSub;
        UserData = userData;
    }

    public void SetSelected(bool selected)
    {
        if (selectionOverlay != null) selectionOverlay.enabled = selected;
    }
}
