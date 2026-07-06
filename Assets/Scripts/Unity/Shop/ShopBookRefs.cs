using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ShopBook.prefab 하위 UI 참조 컨테이너. ShopUIAdapter가 런타임 계층 탐색(transform.Find) 대신
/// 이 컴포넌트의 SerializeField로 즉시 접근. 계층 rename/reparent 시 인스펙터에서 즉시 감지 가능.
/// </summary>
public class ShopBookRefs : MonoBehaviour
{
    [Header("Left Page")]
    [Tooltip("아이템 리스트 부모 (ScrollView Content).")]
    [SerializeField] private Transform listContent;
    [Tooltip("탭 이름을 표시하는 상단 헤더 라벨.")]
    [SerializeField] private TextMeshProUGUI headerLabel;

    [Header("Right Page")]
    [Tooltip("선택 아이템 상세 정보 패널.")]
    [SerializeField] private ShopDetailPanel detailPanel;

    [Header("Chrome")]
    [Tooltip("책 닫기 X 버튼.")]
    [SerializeField] private Button closeButton;

    [Header("Bookmarks (Item/Tool/Storage/Farm 순)")]
    [Tooltip("탭 전환 북마크 버튼 배열. ShopUIAdapter.Tab enum 순서와 맞춰야 함.")]
    [SerializeField] private Button[] bookmarkButtons;

    public Transform ListContent           => listContent;
    public TextMeshProUGUI HeaderLabel     => headerLabel;
    public ShopDetailPanel DetailPanel     => detailPanel;
    public Button CloseButton              => closeButton;
    public Button[] BookmarkButtons        => bookmarkButtons;

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
