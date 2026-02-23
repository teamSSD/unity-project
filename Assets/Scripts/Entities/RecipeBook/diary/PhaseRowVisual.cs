using UnityEngine;
using UnityEngine.UI;

public class PhaseRowVisual : MonoBehaviour
{
    [Header("페이즈 정보")]
    [Tooltip("이 row가 나타내는 페이즈")]
    public PhaseType phase;

    [Header("시각 효과")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;

    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color completedColor = new Color(0.7f, 0.7f, 0.7f);

    private DiaryModel diaryModel;

    public void Initialize(DiaryModel model)
    {
        this.diaryModel = model;
        RefreshVisual();
    }

    private void Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        // DiaryUIController에서 Initialize 호출 대기
        if (diaryModel == null && DiaryUIController.Instance != null)
        {
            diaryModel = DiaryUIController.Instance.Model;
        }

        RefreshVisual();
    }

    private void OnEnable()
    {
        if (diaryModel != null)
        {
            diaryModel.OnActionStateChanged += OnActionStateChanged;
        }
    }

    private void OnDisable()
    {
        if (diaryModel != null)
        {
            diaryModel.OnActionStateChanged -= OnActionStateChanged;
        }
    }

    private void OnActionStateChanged(PhaseType changedPhase, ActionType actionType, ActionState newState)
    {
        if (changedPhase == phase)
        {
            RefreshVisual();
        }
    }

    private void RefreshVisual()
    {
        if (diaryModel == null) return;

        bool isCompleted = diaryModel.IsPhaseCompleted(phase);

        if (isCompleted)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.6f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = completedColor;
            }
        }
        else
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }
    }
}
