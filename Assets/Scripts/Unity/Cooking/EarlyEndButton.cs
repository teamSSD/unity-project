using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cooking 씬 우상단 "영업 조기 종료" 버튼. 클릭 → ConfirmModal → CustomerManager.EndEarly().
/// </summary>
[RequireComponent(typeof(Button))]
public class EarlyEndButton : MonoBehaviour
{
    [SerializeField] private CustomerManager customerManager;

    private void Start()
    {
        if (customerManager == null)
            customerManager = Object.FindFirstObjectByType<CustomerManager>();

        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        ConfirmModal.Show(
            title: "영업 조기 종료",
            message: "다음 페이즈로 넘어갑니다.\n남은 손님은 모두 떠납니다.",
            onConfirm: () => customerManager?.EndEarly(),
            yesText: "종료하기",
            noText: "취소"
        );
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
