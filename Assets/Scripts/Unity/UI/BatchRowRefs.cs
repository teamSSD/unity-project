using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BatchRow.prefab (인벤토리 상세의 배치 행) 하위 참조 컨테이너.
/// InventoryPageController가 transform.Find 대신 이 컴포넌트로 즉시 접근.
/// </summary>
public class BatchRowRefs : MonoBehaviour
{
    [SerializeField] private Button discardButton;
    [SerializeField] private TextMeshProUGUI qtyLabel;
    [Tooltip("BarBg 하위의 Bar RectTransform. 남은 기간 비율에 따라 anchorMax.x 조절.")]
    [SerializeField] private RectTransform barRect;
    [SerializeField] private TextMeshProUGUI daysLabel;

    public Button DiscardButton      => discardButton;
    public TextMeshProUGUI QtyLabel  => qtyLabel;
    public RectTransform BarRect     => barRect;
    public TextMeshProUGUI DaysLabel => daysLabel;

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
