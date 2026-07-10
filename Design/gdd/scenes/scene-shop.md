# Shop.unity — 상점 씬

> 파일: `Assets/Scenes/ForReal/Shop.unity`
> 진입: Mall의 상점 트리거(`UnifiedShopInteraction`) 또는 `SceneTransitionInteraction`.

## 역할 한 줄

**재료/도구/창고/농장 4종 상점**을 통합한 사이드-스크롤 뷰. 실제 UI는 Managers 씬에 상주하는 `ShopUIAdapter`가 오버레이로 표시.

## GameObject 계층

```
Shop.unity (root)
├── EventSystem
├── Canvas                          (screen-space; 대부분의 UI는 ShopUIAdapter가 별도로 인스턴스)
├── Camera (Main Camera)             CameraFollow + DynamicBoundaryWalls
├── background                       Shop 배경
├── Floor                            BoxCollider2D (플레이어 착지 판정)
├── Square                           (지오메트리 요소)
├── PlayerSpawnPoint                 진입 지점 Transform
├── ExitToMall                       SceneTransitionInteraction → Mall
├── Wall_ShopThreshold               Shop 진입 지점 경계 벽
├── ShopManager                      ShopSceneController 부착
└── ShopTrigger_Item                 UnifiedShopInteraction (재료 탭 오픈)
```

## 붙어있는 스크립트

| 클래스 | 위치 | 부착 대상 |
|---|---|---|
| `ShopSceneController`     | `Unity/Shop/ShopSceneController.cs`      | ShopManager GO |
| `SceneTransitionInteraction` | `Unity/Mall/SceneTransitionInteraction.cs` | ExitToMall |
| `UnifiedShopInteraction`  | `Unity/Mall/UnifiedShopInteraction.cs`   | ShopTrigger_Item |
| `CameraFollow`            | `Unity/Common/CameraFollow.cs`           | Main Camera |
| `DynamicBoundaryWalls`    | `Unity/Common/DynamicBoundaryWalls.cs`   | Main Camera |

## ShopSceneController — 진입 좌표 처리

[`ShopSceneController.cs`](../../../Assets/Scripts/Unity/Shop/ShopSceneController.cs)

```csharp
[SerializeField] private Transform entryPoint;   // 없으면 GameObject.Find("ExitToMall")로 폴백
[SerializeField] private BoxCollider2D floor;    // 바닥 착지 판정
```

`Start()`에서:
- `entryPoint` X + Floor 상단 Y로 플레이어 위치 정확히 배치 (플레이어 collider offset/size 반영)
- `CameraFollow.SnapToPlayer()` 즉시 카메라 정렬

플레이어 이동속도(`PlayerMove.moveSpeed`)는 씬별 override 없음. 대신 카메라 `orthographicSize`로 이동감 조정.

## UnifiedShopInteraction — 상점 트리거

[`UnifiedShopInteraction.cs`](../../../Assets/Scripts/Unity/Mall/UnifiedShopInteraction.cs)

```csharp
[SerializeField] private ShopUIAdapter.Tab targetTab = ShopUIAdapter.Tab.Item;
```

- OnTriggerEnter2D(Player) → `InteractPromptUI.Show("(press spacebar to open shop)")`
- Space 입력 + `!UILockManager.IsLocked` → `ShopUIAdapter.Instance.OpenShop(targetTab)` 오버레이 표시

**주의**: Shop 씬은 사실 컨테이너. UI는 `ShopUIAdapter`가 관리하는 ShopBook prefab이 오버레이로 뜬다.

## ShopUIAdapter — 실제 상점 UI

[`ShopUIAdapter.cs`](../../../Assets/Scripts/Unity/Shop/ShopUIAdapter.cs) (Managers 씬 singleton)

**ShopBook.prefab을 런타임에 인스턴스화** — 레시피북 메타포 재사용.

### 4 탭 (Bookmarks)
```csharp
public enum Tab { Item, Tool, Storage, Farm }
```

- **Item**: 재료 구매 (일반 상점 로직 — `PurchaseService`)
- **Tool**: 요리 도구 업그레이드 (`ToolUpgradeService`)
- **Storage**: 냉장고/캐비넷 용량 업그레이드 (`StorageUpgradeService`)
- **Farm**: 밭 확장 (`FarmUpgradeService`)

업그레이드 3탭 로직은 partial 클래스 `ShopUIAdapter.UpgradeTabs.cs`에 분리.

### 주요 참조
- 헤더 라벨, 리스트 컨테이너, `ShopDetailPanel`, close 버튼, 4 bookmark 버튼
- `ShopListRow` prefab (행 템플릿)
- Bookmark UI: 비선택 70px / 선택 105px × 45px 높이

## 종료 조건

- **Escape 키**: ShopBook의 close 버튼 (별도 ESC 핸들러)
- **ExitToMall**: `SceneTransitionInteraction`으로 Mall 복귀. `SceneLoader.SetMallReturnPosition`이 이 트리거 X를 저장

`Wall_ShopThreshold`가 Shop 씬 진입점 경계에 있어 뒤로 걸어나가는 실수는 방지.

## 관련 시스템

- [Mall 씬](./scene-mall.md) — Shop 진입/복귀 시작 지점
- ADR-008 (Singleton 최소화) — `ShopUIAdapter`는 마지막까지 남은 Singleton 중 하나
- `PurchaseService`, `ToolUpgradeService`, `StorageUpgradeService`, `FarmUpgradeService` — POCO 도메인 로직
- Shop 스키마: `ShopConfigSO foodShopConfig` + Csv (`toolUpgrade`, `storageUpgrade`, `farmUpgrade`)
