using UnityEngine;

/// <summary>
/// Garden 씬 배경 페이즈별 sprite 교체.
/// Preparation/Morning/Afternoon → 아침, Evening → 저녁, Night → 밤.
/// SpriteRenderer 자체 교체라 DynamicBoundaryWalls (같은 GO) 정상 유지.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GardenBackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bgRenderer;
    [SerializeField] private Sprite morningSprite;
    [SerializeField] private Sprite eveningSprite;
    [SerializeField] private Sprite nightSprite;

    void Awake()
    {
        if (bgRenderer == null) bgRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        ApplyPhase(progress?.PhaseData?.Phase ?? PhaseType.Morning);
        if (progress != null) progress.OnPhaseChanged += ApplyPhase;
    }

    void OnDestroy()
    {
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null) progress.OnPhaseChanged -= ApplyPhase;
    }

    private void ApplyPhase(PhaseType phase)
    {
        if (bgRenderer == null) return;

        Sprite target = morningSprite;
        if (phase == PhaseType.Evening) target = eveningSprite;
        else if (phase == PhaseType.Night) target = nightSprite;

        if (target != null) bgRenderer.sprite = target;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
