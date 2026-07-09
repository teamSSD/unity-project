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

    // 손님 mock 흐름
    private CustomerLifecycle _activeLifecycle;
    private OrderTicketModel _activeTicket;
    private BentoModel _placedBento;
    private UnityEngine.UI.Button _earlyEndButton;
    private TutorialBubble _currentBubble;
    private string _currentPartCondition; // "order_placed", "bento_filled", "receipt_attached", ""

    private void OnEnable()
    {
        if (customerManager != null)
        {
            customerManager.OnCustomerResolved += HandleCustomerResolved;
            // Skip 버튼(EarlyEndButton)으로 페이즈 조기 종료돼도 튜토리얼 상태는 완료 처리.
            customerManager.OnGameEnd += HandleGameEndDuringTutorial;
        }

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

        // 조기 종료 버튼 락 — 스킵 안내 파트("오늘은 손님이") 도달 전까지 클릭 불가.
        var eeb = FindFirstObjectByType<EarlyEndButton>(FindObjectsInactive.Include);
        if (eeb != null)
        {
            _earlyEndButton = eeb.GetComponent<UnityEngine.UI.Button>();
            if (_earlyEndButton != null) _earlyEndButton.interactable = false;
        }
    }

    private bool _tutorialControlsCamera = true;

    private void LateUpdate()
    {
        if (_cam == null) return;
        if (!_tutorialControlsCamera) return; // 자유 카메라 파트: HorizontalCameraMove가 이동 담당.
        // SmoothDamp로 target 위치로 부드럽게 이동.
        _cam.transform.position = Vector3.SmoothDamp(
            _cam.transform.position, _cameraTargetPos, ref _cameraVelocity, cameraSmoothTime, cameraMaxSpeed);
    }

    /// <summary>튜토리얼 마지막 파트 dismiss 후 호출 — 잠금 해제 + 즉시 페이즈 종료.
    /// mock 씬은 튜토리얼 끝나면 바로 다음 페이즈로 전환 (여분 손님 없이).</summary>
    public void ReleaseTutorialLocks()
    {
        UILockManager.Unlock(UILockManager.Owner.CookingTutorial);

        // 카메라 조작 복구.
        if (_horizontalMove != null) _horizontalMove.enabled = true;

        // 시간 재개.
        TimeManager.Instance?.ResumeTime();

        // 튜토리얼 완료 → 페이즈 즉시 종료 (mock 씬 벗어남).
        if (customerManager != null)
        {
            customerManager.enabled = true; // EndEarly 호출 전 재활성 (Update는 안 돌아도 OK).
            customerManager.EndEarly();
        }
    }

    /// <summary>파트별 손님 mock 흐름 트리거 + 카메라 target 이동.
    /// 메시지 prefix로 판정 (mock 씬 전용이라 이 정도 결합은 허용).
    /// 라우팅으로 target을 등록한 후 카메라 이동 계산 → 첫 프레임부터 카메라가 target 향함.</summary>
    private void HandlePartShown(int stepId, int partIdx, TutorialStepPart part)
    {
        // 0) 파트가 게임 상호작용 필요하면 CookingTutorial 락 해제 (bubble 락은 TutorialController가 처리).
        //    한 번 풀리면 이후 파트도 락 없음 — 흐름 상 앞 파트는 non-interactive라 문제 없음.
        if (part.allowSceneInteraction)
        {
            UILockManager.Unlock(UILockManager.Owner.CookingTutorial);
        }

        // 0.5) 카메라 제어권 스위치.
        //  - freeCamera=true → HorizontalCameraMove가 유저 조작 담당, 튜토리얼 SmoothDamp 정지.
        //  - freeCamera=false → 튜토리얼이 target으로 강제 이동.
        _tutorialControlsCamera = !part.freeCamera;
        if (_horizontalMove != null) _horizontalMove.enabled = part.freeCamera;

        // 1) 손님 mock 흐름 라우팅 (target 등록 포함).
        string msg = part.message ?? "";
        if (msg.StartsWith("손님이 오면"))
        {
            SpawnCustomerForTutorial();
        }
        else if (msg.StartsWith("도시락을 원하는"))
        {
            BentoModel.OnBentoPlacedForTutorial += HandleBentoPlaced;
        }
        else if (msg.StartsWith("완성한 요리"))
        {
            // 유저가 방금 놓은 도시락에 target 부여 → 힌트가 가리킴.
            if (_placedBento != null)
                RegisterTutorialTarget(_placedBento.gameObject, "placed-bento");
            BentoModel.OnFoodAddedForTutorial += HandleBentoFilled;
        }
        else if (msg.StartsWith("영수증"))
        {
            if (_activeTicket != null)
            {
                // 영수증 GameObject에 TutorialTarget key="receipt" 부여 (anchor 하단 → 말풍선이 영수증 밑을 기준으로).
                RegisterTutorialTarget(_activeTicket.gameObject, "receipt", verticalAnchor: 0f);
                // 부착만 하면 dismiss 안 하고 Taking 손님 스폰까지 대기 — 그 사이 시간안내 파트는 안 보임.
                if (_activeLifecycle != null)
                    _activeLifecycle.OnTutorialTakingSpawned += HandleTakingSpawnedForReceiptPart;
            }
        }
        else if (msg.StartsWith("시간 안에"))
        {
            // Taking 손님 target은 이미 앞 파트(HandleTakingSpawnedForReceiptPart)에서 등록됨.
            // dynamic follow가 taking 손님 이동을 따라감. 별도 이벤트 구독 불필요.
        }
        else if (msg.StartsWith("오늘은 손님이"))
        {
            // 스킵 버튼 파트 진입 → EarlyEndButton 활성화 (이전 파트들에선 락).
            if (_earlyEndButton != null) _earlyEndButton.interactable = true;
        }

        // 2) 카메라 이동 (target 있으면). 위 라우팅에서 target을 등록했다면 여기서 잡힘.
        var target = TutorialController.Instance?.GetTarget(part.targetKey);
        if (target != null)
        {
            var sr = target.GetComponent<SpriteRenderer>();
            Vector3 worldPos = sr != null ? sr.bounds.center : target.transform.position;
            _cameraTargetPos = ClampToBounds(new Vector3(worldPos.x, worldPos.y, forceCameraPosition.z));
        }
    }

    private void SpawnCustomerForTutorial()
    {
        if (customerManager == null) return;
        _activeLifecycle = customerManager.TutorialSpawnOne();
        if (_activeLifecycle == null) return;

        // 스폰 즉시 ordering customer에 target 부여 → 힌트가 손님을 가리킴.
        var orderingGo = customerManager.GetCurrentOrderingCustomer();
        if (orderingGo != null) RegisterTutorialTarget(orderingGo, "customer");

        _activeLifecycle.OnTutorialOrderPlaced += HandleOrderPlaced;
    }

    private void HandleOrderPlaced(OrderTicketModel ticket)
    {
        _activeTicket = ticket;
        // Waiting customer 타이머 정지 — 시간 흐름 mock 잠금 (손님 안 나감).
        _activeLifecycle?.GetWaitingCustomer()?.StopTimer();

        // 현재 활성 파트 dismiss → 다음 파트로.
        TutorialController.Instance?.DismissActivePart();
    }

    private void HandleBentoPlaced(BentoModel bento)
    {
        BentoModel.OnBentoPlacedForTutorial -= HandleBentoPlaced;
        _placedBento = bento; // 다음 파트에서 target으로 사용.
        TutorialController.Instance?.DismissActivePart();
    }

    /// <summary>영수증 파트에서 hook. 부착 후 Taking 손님 스폰될 때 발화. target 등록 + Part dismiss (다음 = 시간안내).</summary>
    private void HandleTakingSpawnedForReceiptPart(GameObject takingGo)
    {
        if (_activeLifecycle != null)
            _activeLifecycle.OnTutorialTakingSpawned -= HandleTakingSpawnedForReceiptPart;
        if (takingGo != null) RegisterTutorialTarget(takingGo, "taking-customer");
        TutorialController.Instance?.DismissActivePart();
    }

    private void HandleBentoFilled(BentoModel bento, FoodSchema food)
    {
        BentoModel.OnFoodAddedForTutorial -= HandleBentoFilled;
        TutorialController.Instance?.DismissActivePart();
    }

    private void RegisterTutorialTarget(GameObject go, string key, float verticalAnchor = 1f)
    {
        var t = go.GetComponent<TutorialTarget>();
        if (t == null) t = go.AddComponent<TutorialTarget>();
        t.SetVerticalAnchor(verticalAnchor);
        t.SetKey(key);
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
        {
            customerManager.OnCustomerResolved -= HandleCustomerResolved;
            customerManager.OnGameEnd -= HandleGameEndDuringTutorial;
        }
        foreach (var s in storages)
        {
            if (s != null) s.OnFoodDestroyedForRefill -= HandleFoodDestroyed;
        }
        var tc = TutorialController.Instance;
        if (tc != null) tc.OnPartShown -= HandlePartShown;

        // Mock 흐름 훅 클린업 (파트 진행 도중 씬 종료 대비).
        BentoModel.OnBentoPlacedForTutorial -= HandleBentoPlaced;
        BentoModel.OnFoodAddedForTutorial -= HandleBentoFilled;
        if (_activeLifecycle != null)
        {
            _activeLifecycle.OnTutorialOrderPlaced -= HandleOrderPlaced;
            _activeLifecycle.OnTutorialTakingSpawned -= HandleTakingSpawnedForReceiptPart;
        }
    }

    private void HandleCustomerResolved(bool wasServed)
    {
        if (!wasServed)
        {
            // 화나서 나감 — 튜토리얼에선 사실상 없음 (타이머 정지). 실제 발생 시 튜토리얼 로직 오류.
            Debug.LogWarning("[Tutorial] Customer left angry — should not happen (timer stopped).");
            return;
        }
        if (_customerResolved) return;
        _customerResolved = true;
        // "시간 안내" 파트 → "스킵 버튼 안내" 파트로 자동 진행. 스킵 파트는 dismissKey 없어서 유저가 실제 버튼 클릭해야 함.
        TutorialController.Instance?.DismissActivePart();
    }

    /// <summary>Skip 버튼(EarlyEndButton)으로 페이즈 조기 종료됐을 때 — 튜토리얼 상태 마감 + 인벤토리 원상복귀.
    /// (튜토리얼에서 refill로 인벤토리 상태가 바뀔 수 있으므로 초기값으로 리셋 → Day 1 정상 시작.)</summary>
    private void HandleGameEndDuringTutorial()
    {
        GameSessionRoot.Instance?.Tutorial?.MarkShown(TutorialStepId.CookingIntro);
        GameSessionRoot.Instance?.Inventory?.ResetToDefault();
        SaveManager.SaveAll();
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
