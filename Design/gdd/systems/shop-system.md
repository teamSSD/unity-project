# Shop 시스템

## 개요

**Shop (상점) 씬**은 재료 매입 및 3종 업그레이드(도구 / 창고 / 농장)를 담당하는 씬. Mall의 `PhaseActionSelector.Shopping`이나 Mall 내 상점 카운터(`UnifiedShopInteraction`)를 통해 진입한다.

씬 파일: `Assets/Scenes/ForReal/Shop.unity`
핵심 컨트롤러: [`ShopSceneController.cs`](../../../Assets/Scripts/Unity/Shop/ShopSceneController.cs), [`ShopUIAdapter.cs`](../../../Assets/Scripts/Unity/Shop/ShopUIAdapter.cs)
구매 로직 (POCO): [`PurchaseService.cs`](../../../Assets/Scripts/Domain/Shop/PurchaseService.cs)

## 상점 진입 / 이탈

### ShopSceneController (Scene_Shop.unity 진입 시)
- `entryPoint` transform의 X 좌표에 플레이어 위치
- `floor` BoxCollider2D 상단에 정확히 착지 (플레이어 collider bounds + offset 반영)
- `CameraFollow.SnapToPlayer()` 호출 — 첫 프레임 슬라이드 방지
- 이동 속도는 씬별 override 안 함 (카메라 orthographicSize로 이동감 조정)

### UnifiedShopInteraction (Mall 내 오버레이 UI)
- Mall 씬의 카운터에서 Space → `ShopUIAdapter.Instance.OpenShop(targetTab)` 호출
- Shop.unity 씬으로 전환하지 않음 — UI 오버레이만 뜸
- `targetTab`은 인스펙터 설정 (Item / Tool / Storage / Farm)

## ShopUIAdapter (통합 상점 UI)

파일: [`ShopUIAdapter.cs`](../../../Assets/Scripts/Unity/Shop/ShopUIAdapter.cs), 업그레이드 partial: [`ShopUIAdapter.UpgradeTabs.cs`](../../../Assets/Scripts/Unity/Shop/ShopUIAdapter.UpgradeTabs.cs)

- **DontDestroyOnLoad Singleton** — `ShopBook.prefab`을 런타임에 인스턴스화 후 전 씬에서 재사용
- 오픈 시 `UILockManager.Lock(Shop)` — 중복 오픈 차단
- Esc로 CloseShop

### 4개 탭 (북마크)

| Tab | 이름 | 담당 |
|---|---|---|
| Item | 재료 | 재료 매입 (`PurchaseService`) |
| Tool | 도구 | 조리 도구 업그레이드 (`ToolUpgradeService`) |
| Storage | 창고 | 냉장고/찬장 확장 (`StorageUpgradeService`) |
| Farm | 농장 | 텃밭 확장 (`FarmUpgradeService`) |

### 카테고리 라벨 매핑

Storage:
- `refrigerator` → 냉장고 확장
- `upperShelf` → 윗 찬장 확장
- `lowerShelf` → 아랫 찬장 확장

Farm:
- `tile` → 농장 확장
- `timeReduction` → 수확 시간 감소
- `harvestCount` → 수확량 증가

## PurchaseService (재료 상점 구매 로직)

파일: [`PurchaseService.cs`](../../../Assets/Scripts/Domain/Shop/PurchaseService.cs)

POCO Service. `GameSessionRoot.Purchase`로 접근. 상태와 UI가 완전히 분리.

### ProductType (구매 대상 분류)

파일: [`ItemShopSlotInfo.cs`](../../../Assets/Scripts/Schema/State/Shop/ItemShopSlotInfo.cs)

```csharp
public enum ProductType { General, Special }
```

| Type | 재고 (`stock`) | 페이즈별 리셋 | UI 표시 |
|---|---|---|---|
| **General** | `-1` (무제한) | — | "재고 N" 표시 안 함, `IsUnlimited=true` |
| **Special** | `0` 이상 (한정) | 페이즈마다 새 라인업 뽑음 + 구매수 리셋 | "재고 N" 표시 (0도 표시) |

### 페이즈 단위 라인업

- 캐시 키: `(day, phaseIndex)` — `(day << 8) | phaseIndex`
- 조합 바뀌면 `_config.BuildSlotList(rng)` 재호출 + `_phasePurchased.Clear()`
- 재현성 RNG: `GameRandom.PhaseRandom(day, phase)` — 같은 day/phase면 동일 라인업

### `GetRemaining(info)` — 특별 아이템 제한 판정

```csharp
public int GetRemaining(ItemShopSlotInfo info)
{
    if (info == null) return 0;
    if (info.IsUnlimited) return int.MaxValue;
    return Math.Max(0, info.stock - GetPurchasedThisPhase(info.item));
}
```

- General은 항상 `int.MaxValue` 반환
- Special은 `initialStock - purchasedThisPhase` (0 미만이면 clamp)

### 매일 리셋 vs 페이즈 리셋

- **PurchaseService는 페이즈 단위 리셋** (day + phase 조합 바뀌면). 하루 5페이즈이므로 페이즈마다 새 4개 Special 라인업이 뽑힘
- 재고 카운터 `_phasePurchased`는 라인업 갱신 시 완전 초기화
- 즉 Special 아이템은 하루에 최대 20회(4슬롯 × 5페이즈) 라인업으로 뜰 수 있으나, 각 페이즈의 특정 아이템은 `stock`만큼만 구매 가능

### 가격 산정

- 개별 아이템의 단가는 `FoodData.ingredient.defaultPrice` (파일: [`IngredientData.cs`](../../../Assets/Scripts/Schema/Config/Cooking/IngredientData.cs))
- 총액: `qty * unitPrice`
- 페이즈별 가격 변동 없음 — 고정가

### 구매 트랜잭션 (`TryBuy`)

```
1. CanBuy 체크: 잔액 >= total && Inventory.CanAcceptType(food)
2. Money.TrySpend(total) — 잔액 차감
3. Expense.Add("재료 구매", total) — Settlement에 지출 기록
4. Inventory.AddFood(food, qty)
5. NotifyPurchased(food, qty) — 페이즈 구매수 누적
```

### 인벤토리 수용량 체크 (`CanAcceptType`)

파일: [`InventoryService.cs`](../../../Assets/Scripts/Domain/Common/InventoryService.cs#L120)

- `IngredientDisplayCategory` (Refrigerator / UpperShelf / LowerShelf)별로 슬롯 상한 존재
- 이미 인벤토리에 있는 재료면 항상 True (다른 배치 병합)
- 신규 재료면 `StorageUpgradeService.GetCurrentData(type).value` (슬롯 상한) 대비 현재 종류 수 비교

## FoodShopConfig (재료 라인업 정의)

파일: [`FoodShopConfigSO.cs`](../../../Assets/Scripts/Schema/Config/Shop/FoodShopConfigSO.cs)
Asset: `Assets/Bundles/ScriptableObjects/Config/FoodShopConfig.asset`

```csharp
public List<FoodData> generalFoods;       // 무한 매입 재료
public List<SpecialEntry> specialSlots;   // 페이즈별 랜덤 뽑기 대상
public int specialPickCount = 4;          // 페이즈마다 4개 랜덤 픽
```

### `BuildSlotList(rng)`
1. `specialSlots`에서 `GameRandom.Pick(rng, ..., specialPickCount)` — 4개 뽑아 Special 슬롯 생성 (`stock`은 SpecialEntry의 값 사용)
2. `generalFoods` 전체를 General 슬롯으로 추가 (`stock = -1`)
3. 반환 순서: Special 4개 → General 전체

### FoodShopConfig 자산 실제 라인업

**General (16종, 항상 판매)**: I001, I002, I003, I004, I005, I007, I008, I015, I016, I018, I023, I024, I025, I066, I068, I069

**Special (19종, 매 페이즈 4개 랜덤, stock=10)**: I009, I010, I012, I013, I014, I017, I019, I020, I021, I022, I026, I027, I028, I029, I030, I031, I065, I067, I070

(각 재료 상세는 [`../content/ingredients.md`](../content/ingredients.md) 참조 — 존재 시)

> 참고: `Assets/Bundles/driveAssets/dataTables/foodStore.csv`는 각 재료에 General/Special 태그를 정의하지만, 실제 상점 라인업은 위 ScriptableObject가 SSOT. CSV는 참조용이거나 레거시로 보인다.

## 업그레이드 서비스 (Tool / Storage / Farm)

3종 업그레이드는 **동일 패턴**의 POCO Service.

| 서비스 | 파일 | 상태 저장소 | 데이터 CSV |
|---|---|---|---|
| ToolUpgrade | [`ToolUpgradeService.cs`](../../../Assets/Scripts/Domain/Shop/ToolUpgradeService.cs) | `ShopPersistent.toolIds/toolLevels` | `upgrade_tool.csv` |
| StorageUpgrade | [`StorageUpgradeService.cs`](../../../Assets/Scripts/Domain/Shop/StorageUpgradeService.cs) | `ShopPersistent.storageTypes/storageLevels` | `upgrade_storage.csv` |
| FarmUpgrade | [`FarmUpgradeService.cs`](../../../Assets/Scripts/Domain/Garden/FarmUpgradeService.cs) | `GardenPersistent.upgradeTypes/upgradeLevels` | `upgrade_farm.csv` |

### 공통 API
- `GetCurrentData(id/type)` — 현재 레벨 데이터
- `GetNextData(id/type)` — 다음 레벨 (`null`이면 MAX)
- `IsMax(id/type)`
- `TryUpgrade(id/type)` — `Money.TrySpend(next.cost)` → `Expense.Add("업그레이드", cost)` → 레벨 증가

### 도구 업그레이드 (`upgrade_tool.csv`)

| toolId | level 0 | level 1 (5,000G) | level 2 (12,000G) |
|---|---|---|---|
| T001~T005 (전 도구) | 스태미나 5 / 시간 100% | 스태미나 4 / 시간 80% | 스태미나 3 / 시간 60% |

- 5개 도구(T001~T005) 모두 동일한 코스트/효과
- `durationMultiplier`는 미니게임 진행 시간에 곱해짐 (짧을수록 유리)
- `staminaCost`는 요리 1회당 스태미나 소모

### 창고 업그레이드 (`upgrade_storage.csv`)

| type | level 0 | level 1 (10,000G) | level 2 (25,000G) | level 3 (60,000G) |
|---|---|---|---|---|
| refrigerator | 7칸 | 10칸 | 13칸 | 15칸 |
| upperShelf | 3칸 | 5칸 | 6칸 | 8칸 |
| lowerShelf | 4칸 | 6칸 | 8칸 | 8칸 |

- `value`는 각 카테고리의 최대 재료 종류 수 (동일 재료의 배치는 하나로 병합됨)
- 시작 재료 9종은 초기 lv0 상한(7+3+4=14) 안에 맞춤

### 농장 업그레이드 (`upgrade_farm.csv`)

| type | level 0 | 1 (3,000G) | 2 (7,000G) | 3 (12,000G) | 4 (20,000G) |
|---|---|---|---|---|---|
| tile | 3개 | 4개 | 5개 | 6개 | 7개 |
| timeReduction | 0% | 16% | 24% | 32% | 40% |
| harvestCount | 5개 | 7개 | 9개 | 11개 | 13개 |

- `tile.value`가 잠금 해제된 타일 수 (Farm.IsLocked 판정)
- `timeReduction`이 성장 페이즈 요구량 감소율
- `harvestCount`가 1회 수확 시 획득 재료 개수
- 상세 효과는 [`garden-system.md`](garden-system.md) 참조

## 상점 UI 흐름

### 진입 (`OpenShop`)
1. `UILockManager.Lock(Shop)` — 중복 오픈 차단
2. `bookInstance.SetActive(true)` — 책 UI 표시
3. `SoundManager.PlayUIBook()` (책 넘기는 SFX)
4. `SwitchTab(tab)` — 초기 탭 활성화

### 탭 전환 (`SwitchTab`)
- 이전 행 clear + 상세 패널 empty
- 헤더 라벨 갱신 + 북마크 크기 조정 (선택된 것은 105px, 나머지 70px)
- 탭에 맞는 `Populate*List()` 호출

### 재료 매입 UI (Item 탭 상세)
- `ShopDetailPanel.ShowItem(food, unitPrice, remainingStock)`
- +/- 버튼으로 `qty` 조정 (0 ~ remainingStock)
- 총액 실시간 표시 (`qty * unitPrice`)
- Buy 클릭 → `PurchaseService.TryBuy` → 성공 시 `NotifyItemPurchased` → 행/상세 업데이트

### 업그레이드 UI (Tool/Storage/Farm 탭)
- `ShopDetailPanel.ShowUpgrade(kind, id, ...)`
- 현재/다음 레벨 값 라벨로 표시 (`Lv.1 → Lv.2` 형식)
- MAX 도달 시 코스트/버튼 비활성
- Upgrade 클릭 → 각 서비스의 `TryUpgrade(id/type)`

## 매일 리셋 여부 정리

| 리셋 대상 | 주기 | 트리거 |
|---|---|---|
| Special 라인업 (4개 뽑기) | 페이즈마다 | `PurchaseService.GetItemList(day, phase)` 캐시 키 변경 |
| Special 페이즈 구매수 | 페이즈마다 | 캐시 리로드 시 `_phasePurchased.Clear()` |
| Money | 아님 (누적) | `PassDay`에서 관리비 -1000G만 차감 |
| 업그레이드 레벨 | 아님 (영구) | `ShopPersistent` / `GardenPersistent` 저장 |
| Stamina | 매일 (하루 시작) | `PassDay`에서 100으로 세팅 |

## 관련 문서

- Mall 씬 흐름: [`mall-system.md`](mall-system.md)
- 인벤토리 / 유통기한: [`save-system.md`](save-system.md)
- 정산 (지출 리포트): [`settlement-system.md`](settlement-system.md)
- 진행도 언락 (레시피): [`progression-system.md`](progression-system.md)
- 재료 데이터 목록: [`../content/ingredients.md`](../content/ingredients.md)
