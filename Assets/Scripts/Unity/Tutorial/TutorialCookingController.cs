using UnityEngine;

/// <summary>
/// CookingTutorial 씬 전용 mock 컨트롤러.
/// - 재료 무한 리필: 각 storage의 OnFoodDestroyedForRefill → CookingSceneManager.TryTutorialRefill
/// - 손님 1명 후 종료: CustomerManager.OnCustomerResolved → EndEarly
///   (EndEarly가 OnGameEnd 발화 → SubSceneController.ReturnToIdle → PassPhase + Mall 로드)
/// - 시간 정지: TimeManager.PauseTime()
/// </summary>
public class TutorialCookingController : MonoBehaviour
{
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private CookingSceneManager cookingSceneManager;
    [SerializeField] private BaseStorage[] storages;

    [Header("Camera Force")]
    [SerializeField, Tooltip("튜토리얼 진입 시 카메라 초기 위치 (인트로/레시피 탭 등 target 없는 파트).")]
    private Vector3 forceCameraPosition = new Vector3(0f, 0f, -10f);
    [SerializeField, Tooltip("카메라 orthographic size 강제. 0 이하면 무시.")]
    private float forceCameraOrthoSize = -1f;

    [Header("Camera Lerp")]
    [SerializeField, Tooltip("카메라가 target으로 lerp되는 SmoothDamp time (초). 작을수록 빠름.")]
    private float cameraSmoothTime = 0.5f;
    [SerializeField, Tooltip("카메라 최대 이동 속도 (unit/sec).")]
    private float cameraMaxSpeed = 20f;
    [SerializeField, Tooltip("맵 경계 collider (있으면 카메라 lerp 시 클램프).")]
    private Collider2D worldCollider;

    private bool _customerResolved;
    private Camera _cam;
    private HorizontalCameraMove _horizontalMove;
    private Vector3 _cameraTargetPos;
    private Vector3 _cameraVelocity;

    private void OnEnable()
    {
        if (customerManager != null)
            customerManager.OnCustomerResolved += HandleCustomerResolved;

        // 카메라 초기화 + OnPartShown 구독은 Start보다 이르게 — CookingSceneManager.Start가 첫 파트 트리거하기 전.
        SetupCamera();

        var tc = TutorialController.Instance;
        if (tc != null) tc.OnPartShown += HandlePartShown;
    }

    private void SetupCamera()
    {
        if (_cam != null) return; // 이미 초기화됨
        _cam = Camera.main;
        if (_cam == null) return;
        _horizontalMove = _cam.GetComponent<HorizontalCameraMove>();
        if (_horizontalMove != null)
        {
            _horizontalMove.enabled = false;
            if (worldCollider == null) worldCollider = _horizontalMove.WorldCollider;
        }
        if (forceCameraOrthoSize > 0f) _cam.orthographicSize = forceCameraOrthoSize;
        _cameraTargetPos = ClampToBounds(forceCameraPosition);
        _cam.transform.position = _cameraTargetPos;
    }

    private void Start()
    {
        // Storage 이벤트는 CookingSceneManager.Start 이후 (초기 FillStorage 완료 후) 붙임.
        foreach (var s in storages)
        {
            if (s != null) s.OnFoodDestroyedForRefill += HandleFoodDestroyed;
        }
        // 시간 정지 — 아침 페이즈 자동 종료 방지.
        TimeManager.Instance?.PauseTime();

        // 카메라 세팅 + 이벤트 구독은 OnEnable에서 이미 처리됨 (Start 타이밍 회피).

        // 손님 억제 — 튜토리얼 완료 시 해제.
        if (customerManager != null) customerManager.enabled = false;

        // 상호작용 차단 — 별도 owner (Tutorial과 다름: TutorialBubble의 per-bubble Lock/Unlock에 영향 안 받음).
        UILockManager.Lock(UILockManager.Owner.CookingTutorial);
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        // SmoothDamp로 target 위치로 부드럽게 이동.
        _cam.transform.position = Vector3.SmoothDamp(
            _cam.transform.position, _cameraTargetPos, ref _cameraVelocity, cameraSmoothTime, cameraMaxSpeed);
    }

    /// <summary>튜토리얼 마지막 파트 dismiss 후 호출 — 손님/상호작용/카메라 복구.</summary>
    public void ReleaseTutorialLocks()
    {
        UILockManager.Unlock(UILockManager.Owner.CookingTutorial);
        if (customerManager != null) customerManager.enabled = true;

        // 카메라 조작 복구.
        if (_horizontalMove != null) _horizontalMove.enabled = true;
    }

    /// <summary>파트별 카메라 target 이동. target 없는 파트는 기본 위치로.</summary>
    private void HandlePartShown(int stepId, int partIdx, TutorialStepPart part)
    {
        Vector3 targetPos = forceCameraPosition;
        var target = TutorialController.Instance?.GetTarget(part.targetKey);
        if (target != null)
        {
            var sr = target.GetComponent<SpriteRenderer>();
            Vector3 worldPos = sr != null ? sr.bounds.center : target.transform.position;
            targetPos = new Vector3(worldPos.x, worldPos.y, forceCameraPosition.z);
        }
        _cameraTargetPos = ClampToBounds(targetPos);
    }

    /// <summary>worldCollider bounds 안으로 카메라 target 클램프 (벽 밖 이동 방지).</summary>
    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (_cam == null || worldCollider == null) return pos;
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;
        var b = worldCollider.bounds;
        pos.x = Mathf.Clamp(pos.x, b.min.x + halfW, b.max.x - halfW);
        pos.y = Mathf.Clamp(pos.y, b.min.y + halfH, b.max.y - halfH);
        return pos;
    }

    private void OnDisable()
    {
        if (customerManager != null)
            customerManager.OnCustomerResolved -= HandleCustomerResolved;
        foreach (var s in storages)
        {
            if (s != null) s.OnFoodDestroyedForRefill -= HandleFoodDestroyed;
        }
        var tc = TutorialController.Instance;
        if (tc != null) tc.OnPartShown -= HandlePartShown;
    }

    private void HandleCustomerResolved()
    {
        if (_customerResolved) return;
        _customerResolved = true;
        Debug.Log("[Tutorial] First customer resolved — ending phase.");
        customerManager.EndEarly();
    }

    private void HandleFoodDestroyed(BaseStorage storage, FoodData food)
    {
        if (cookingSceneManager == null || storage == null || food == null) return;
        // Destroy 처리 후 다음 프레임에 refill (같은 프레임에 재추가 시 리스트 상태 꼬임 방지).
        StartCoroutine(RefillNextFrame(storage, food));
    }

    private System.Collections.IEnumerator RefillNextFrame(BaseStorage storage, FoodData food)
    {
        yield return null;
        cookingSceneManager.TryTutorialRefill(storage, food);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
