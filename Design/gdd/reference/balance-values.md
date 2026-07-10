# Aftertaste — 밸런스 상수 총집합

프로젝트 전체 코드 / CSV / ScriptableObject에서 추출한 실질 상수. 파일 경로와 라인 번호는 참고용이며 리팩터링에 따라 이동될 수 있음.

## 1. 초기 자산 / 화폐

| 상수 | 값 | 파일:라인 | 의미 |
|---|---|---|---|
| `startingMoney` | **12000G** | `Assets/Scripts/Unity/Common/GameStart.cs:104` | New Game 초기 소지금 (`session.Stats.SetMoney(12000)`) |
| `startTime` | **05:00** | `Assets/Scripts/Unity/Common/GameStart.cs:103` | New Game 시작 시각 (`SetTime(5,0)`) |
| `startingStamina` | **100** | `Assets/Scripts/Unity/Common/GameStart.cs:105` | New Game 시작 스태미나 |
| `PhaseData.Day` (start) | **0** | `Assets/Scripts/Unity/Common/GameStart.cs:106` | New Game 시작 Day (다음 PassDay에서 1로 증가) |
| `PhaseData.Day` (default) | **1** | `Assets/Scripts/Schema/State/Common/PhaseData.cs:15` | PhaseData 기본 Day (New Game이 0으로 덮음) |
| `PhaseData.Phase` (default) | **Preparation** | `Assets/Scripts/Schema/State/Common/PhaseData.cs:16` | 기본 페이즈 |
| `ManagementFee` | **1000** | `Assets/Scripts/Domain/Mall/SettlementService.cs:11` | 하루 관리비 (PassDay에서 자동 차감) |

## 2. 초기 재료 (인벤토리)

| 항목 | 값 | 파일:라인 | 의미 |
|---|---|---|---|
| `StartingIngredients` | 9종 × 3개 | `Assets/Scripts/Domain/Common/InventoryService.cs:19-24` | I007, I008, I009, I010, I017, I019, I020, I026, I027 각 3개 |
| 유통기한 폴백 | **10일** | `Assets/Scripts/Domain/Common/InventoryService.cs:213` | expirationDay 필드 없을 때 폴백 |

## 3. 시간 / Phase

| 상수 | 값 | 파일:라인 | 의미 |
|---|---|---|---|
| `TotalPhaseCount` | **5** | `Assets/Scripts/Unity/Common/ProgressService.cs:26` | Preparation / Morning / Afternoon / Evening / Night |
| `gameTimeScale` | **120f** | `Assets/Scripts/Unity/Common/TimeManager.cs:8` | `60f/gameTimeScale` 사용 (게임 분/실초 환산 계수) |
| `defaultCustomerWaitTime` | **90f** | `Assets/Scripts/Unity/Common/TimeManager.cs:19` | 손님 기본 인내심(초) |
| `closedLocalTimerScale` | **2f** | `Assets/Scripts/Unity/Common/TimeManager.cs:21` | 영업 종료 후 인내심 가속 배율 |
| Time max clamp | **1439** | `Assets/Scripts/Domain/Common/StatsService.cs:68` | 23:59 (0..1439 분) |
| 하루 총 분 | **1440** | `Assets/Scripts/Domain/Common/StatsService.cs:78-80` | `time %= 1440` 회귀 |

### Phase 시작/종료 (`Assets/Scripts/Unity/Common/ProgressService.cs:102-127`)

| Phase | 시작 | 종료 | 시작 분 | 종료 분 |
|---|---|---|---|---|
| Preparation | 05:00 | 07:00 | 300 | 420 |
| Morning | 07:00 | 12:00 | 420 | 720 |
| Afternoon | 12:00 | 17:00 | 720 | 1020 |
| Evening | 17:00 | 22:00 | 1020 | 1320 |
| Night | 22:00 | 29:00 | 1320 | 1740 |

## 4. 손님 스폰

### CustomerSpawner 배치 (`Assets/Scripts/Unity/Cooking/CustomerSpawner.cs`)

| 항목 | 값 | 라인 |
|---|---|---|
| `availableWaitingPositions` | 5 슬롯 (0..4) | 40 |
| `waitingBasePosition` | `(-8, 0.78, 0)` | 41 |
| `waitingPositionOffset` | `(1.75, 0, 0)` | 42 |
| `waitingPositionVariance` | `(0.3, 0.5, 0)` | 43 |
| `exitPosition` | `(-9.89, -0.85, 0)` | 46 |
| `MaxWaitingCustomers` | **3** | 49 |
| `orderingPosition` | `(3.02, -0.26, 0)` | 52 |
| `orderingTakingScale` | 0.45f | 12 |
| `waitingScale` | 0.297f | 15 |
| `timerScale` | 0.6f | 17 |
| Taking 애니 destroy | 3f | 129, 133 |

> 주의: `availableWaitingPositions` init에 5칸 있고 `MaxWaitingCustomers=3` — 상충 가능성.

### CustomerManager 스폰 인터벌 (`Assets/Scripts/Unity/Cooking/CustomerManager.cs`)

| Phase | 평균(초) | σ(variance) | 라인 |
|---|---|---|---|
| 첫 손님 | 3f | 1f | 82 |
| Morning | **35f** | 8f | 93 |
| Afternoon | **30f** | 8f | 94 |
| Evening | **22f** | 5f | 95 |
| Night | **15f** | 4f | 96 |
| baseSpawnInterval | 30f | 28 |  |
| spawnIntervalVariance | 8f | 29 |  |

정규분포 랜덤 (`GameRandom.Normal(mean, sigma)`)

### OrderTicket
| 상수 | 값 | 위치 |
|---|---|---|
| `QuestSlotIndex` | 5 | `Assets/Scripts/Unity/Cooking/OrderTicketController.cs:22` |
| `attachedSortingOrderOffset` | 100 | `Assets/Scripts/Unity/Cooking/OrderTicketModel.cs:21` |
| `attachedScale` | 0.7f | `Assets/Scripts/Unity/Cooking/OrderTicketModel.cs:23` |
| 부착 SFX 볼륨 | 0.4f | `Assets/Scripts/Unity/Cooking/OrderTicketModel.cs:46,92` |
| 도시락 픽업 delay | 0.5~1.5f 정규 | `Assets/Scripts/Unity/Cooking/OrderTicketModel.cs:67` |
| 티켓 speed | 10f | `Assets/Scripts/Unity/Cooking/OrderTicketBehavior.cs:8` |
| 티켓 restY | 5.54f | `Assets/Scripts/Unity/Cooking/OrderTicketBehavior.cs:12` |

## 5. 요리 결과 산정 / 보상 배율

### CookingToolSchema (`Assets/Scripts/Schema/State/Cooking/CookingToolSchema.cs`)
| 상수 | 값 | 라인 | 의미 |
|---|---|---|---|
| `maxIngredientSize` | 7 | 16 | 도구 당 최대 재료 수 |
| 가격 base | `Price * (0.85f + weight * score)` | 87 | score=1일 때 100% + weight 보정 |
| `chainBonus` | `1 + 0.15f * (chainDepth - 1)` | 93 | MAIN/SIDE 체인 깊이 보너스 |

### MenuValidator (`Assets/Scripts/Domain/Common/MenuValidator.cs`)
| 상수/식 | 값 | 라인 | 의미 |
|---|---|---|---|
| mainScore | 정답 시 0.6 / 오답 0 | 125 | 메인 정답 가중치 |
| sideScore | 완벽/부분 max 0.4 | 133, 140, 148 | 사이드 정답 가중치 |
| extraPenalty | 초과 사이드 × 0.1 | 139, 152 | 초과 페널티 |
| Grade thresholds | S≥1.0 / A≥0.9 / B≥0.8 / C≥0.7 / D≥0.6 | 195-199 | 등급 컷 |
| mainMultiplier | 1.0 / 0.7 | 230 | 매치/불일치 |
| **sideMultiplier** | `1.0 + 0.15 × 일치사이드수` | 231 | Side coefficient 0.15 |

## 6. 조리도구 업그레이드 (upgrade_tool.csv)

`Assets/Bundles/driveAssets/dataTables/upgrade_tool.csv`

| toolId | level | cost | staminaCost | durationMultiplier |
|---|---|---|---|---|
| T001~T005 | 0 | 0 | 5 | 1.00 |
| T001~T005 | 1 | 5000 | 4 | 0.80 |
| T001~T005 | 2 | 12000 | 3 | 0.60 |

매핑: T001=Bake / T002=Boil / T003=Sauce·Mix / T004=Slice(Cut) / T005=Griddle.

## 7. 저장고 업그레이드 (upgrade_storage.csv)

`Assets/Bundles/driveAssets/dataTables/upgrade_storage.csv`

| type | level | cost | 용량 |
|---|---|---|---|
| refrigerator | 0/1/2/3 | 0 / 10000 / 25000 / 60000 | 7 / 10 / 13 / 15 |
| upperShelf | 0/1/2/3 | 0 / 10000 / 25000 / 60000 | 3 / 5 / 6 / 8 |
| lowerShelf | 0/1/2/3 | 0 / 10000 / 25000 / 60000 | 4 / 6 / 8 / 8 |

### 코드 폴백
| 파일 | 라인 | 상수 | 값 |
|---|---|---|---|
| `Assets/Scripts/Unity/Cooking/Refrigerator.cs` | 10 | `DefaultCapacity` | 7 |
| `Assets/Scripts/Unity/Cooking/UpperShelf.cs` | 10 | `DefaultCapacity` | 3 |
| `Assets/Scripts/Unity/Cooking/LowerShelf.cs` | 10 | `DefaultCapacity` | 4 |
| `Assets/Scripts/Unity/Cooking/BaseStorage.cs` | 13 | `capacity` SerializeField | 99 (업그레이드로 덮음) |

## 8. 농장 업그레이드 (upgrade_farm.csv)

`Assets/Bundles/driveAssets/dataTables/upgrade_farm.csv`

| type | level | cost | value |
|---|---|---|---|
| tile | 0/1/2/3/4 | 0 / 3000 / 7000 / 12000 / 20000 | 3 / 4 / 5 / 6 / 7 |
| timeReduction | 0/1/2/3/4 | 0 / 3000 / 7000 / 12000 / 20000 | 0.00 / 0.16 / 0.24 / 0.32 / 0.40 |
| harvestCount | 0/1/2/3/4 | 0 / 3000 / 7000 / 12000 / 20000 | 5 / 7 / 9 / 11 / 13 |

`harvestCount` 폴백: 5 (`Assets/Scripts/Unity/Garden/Farm.cs:93`)
`TileCount` (저장 슬롯 최대): **8** (`Assets/Scripts/Schema/State/Garden/GardenPersistent.cs:17`)

## 9. 재료 유통기한 / 가격 (ingredient.csv)

`Assets/Bundles/driveAssets/dataTables/ingredient.csv`

| id | 이름 | 저장고 | 가격 | 유통기한(일) |
|---|---|---|---|---|
| I001 | 흑미 | LowerShelf | 1500 | 5 |
| I002 | 밀면 | LowerShelf | 1500 | 5 |
| I003 | 두부 | Refrigerator | 1500 | 5 |
| I004 | 마늘 | Refrigerator | 1500 | 5 |
| I005 | 가지 | Refrigerator | 1500 | 5 |
| I007 | 고추장 | UpperShelf | 1500 | 7 |
| I008 | 레몬 | Refrigerator | 600 | 5 |
| I009 | 인공고기 | Refrigerator | 600 | **3** |
| I010 | 은빛생선 | Refrigerator | 600 | **3** |
| I012 | 절연버섯 | LowerShelf | 300 | **2** |
| I013 | 새벽초 | Refrigerator | 1500 | 5 |
| I014 | 청록액 | UpperShelf | 1500 | 7 |
| I015 | 흑액 | LowerShelf | 1500 | 7 |
| I016 | 붉은건재 | Refrigerator | 1500 | 5 |
| I017 | 강철뿌리 | LowerShelf | 1500 | 7 |
| I018 | 대파 | Refrigerator | 1500 | 5 |
| I019 | 흑장 | UpperShelf | 1500 | 7 |
| I020 | 황금유 | UpperShelf | 1500 | 7 |
| I021 | 금결정 | UpperShelf | 1500 | 7 |
| I022 | 향버터 | UpperShelf | 600 | **3** |
| I023 | 은빛건어 | LowerShelf | 1500 | 7 |
| I024 | 흑씨 | UpperShelf | 1500 | 7 |
| I025 | 청양잎 | Refrigerator | 300 | 5 |
| I026 | 루미잎 | Refrigerator | 1500 | 5 |
| I027 | 라이트토마토 | Refrigerator | 600 | **3** |
| I028 | 바람콩 | LowerShelf | 1500 | 5 |
| I029 | 전기꽃 | LowerShelf | 1500 | 5 |
| I030 | 인공육 | Refrigerator | 1500 | 7 |
| I031 | 오렌지육 | Refrigerator | 1500 | 5 |

### 짧은 유통기한 리스크 재료 (3일 이하)
I012 절연버섯(2일), I009 인공고기 · I010 은빛생선 · I022 향버터 · I027 라이트토마토(3일).

## 10. 농작물 스폰 가중치 (crop_data.csv)

`Assets/Bundles/driveAssets/dataTables/crop_data.csv`

| cropId | growPhaseCount | spawnWeight |
|---|---|---|
| I004 마늘 | 7 | 0.04 |
| I005 가지 | 7 | 0.04 |
| I012 절연버섯 | 3 | **0.20** |
| I013 새벽초 | 7 | 0.04 |
| I017 강철뿌리 | 7 | 0.04 |
| I018 대파 | 7 | 0.04 |
| I025 청양잎 | 3 | **0.20** |
| I026 루미잎 | 5 | 0.12 |
| I027 라이트토마토 | 5 | 0.12 |
| I028 바람콩 | 7 | 0.04 |
| I029 전기꽃 | 7 | 0.04 |
| I065 문뿌리 | 7 | 0.04 |
| I066 양파 | 7 | 0.04 |

## 11. 날씨

| 상수 | 값 | 파일:라인 |
|---|---|---|
| `BadWeatherChance` | **0.4f (40%)** | `Assets/Scripts/Domain/Common/WeatherService.cs:12` |

일별 결정 (`GameRandom.InitDay` 이후 결정)

## 12. 상점 (FoodShopConfig)

`Assets/Bundles/ScriptableObjects/Config/FoodShopConfig.asset`

| 항목 | 값 | 파일 |
|---|---|---|
| `specialPickCount` | **4** (페이즈당) | `Assets/Scripts/Schema/Config/Shop/FoodShopConfigSO.cs:18` |
| Special stock | 10 (전 슬롯) | asset |
| General stock | -1 (무한) | `Assets/Scripts/Schema/Config/Shop/FoodShopConfigSO.cs:31` |

## 13. Delivery 퀘스트 (deliveryQuest.csv 발췌)

`Assets/Bundles/driveAssets/dataTables/deliveryQuest.csv`

| GroupId | MenuName | Main | Main2 | Side1 |
|---|---|---|---|---|
| power_room_pair | 전력실 도시락 | I044 | – | I058 |
| night_market | 야시장 도시락 | I039 | I049 | – |
| nimo_solo | 새벽국 도시락 | I034 | – | – |
| seraph_solo | 네온 샐러드 | I060 | – | I056 |
| lede_solo | 삼각밥 도시락 | I053 | – | – |
| gabriel_solo | 기계장 고기정식 | I044 | – | – |

## 14. 미니게임 상수

### GriddleMinigame (`Assets/Scripts/Unity/Cooking/GriddleMinigame.cs`)
| 상수 | 값 | 라인 |
|---|---|---|
| baseLoopVolume | 0.36f | 13 |
| totalArrowCount | 10 | 16 |
| spacing | 150f | 20 |
| MaxVisibleCount | 3 | 28 |
| 뒤 화살표 알파 감쇠 | `1 - i*0.3` | 80 |
| 시각 delay | 0.25s | 150 |
| 볼륨 스파이크 | 0.6f × 0.15s | 172-173 |
| 최소 score | 0.01f | 182 |
| 업그레이드 후 min개수 | 3 | 185 |

### FireMiniGame (`Assets/Scripts/Unity/Cooking/FireMiniGame.cs`)
| 상수 | 값 | 라인 |
|---|---|---|
| baseLoopVolume | 0.36f | 15 |
| gaugePosition | `(-2, 0.33, 0)` | 18 |
| safeZoneRatio | 0.25f | 19 |
| xOffset | 0.3f | 20 |
| gameDuration | 3.0f | 23 |
| coldStartTime | 0.5f | 24 |
| acceleration | 2.0f | 25 |
| maxVelocity | 1.0f | 26 |
| 초기 arrowValue | 0.5f | 30 |
| 속도 자기증폭 | ×1.5f | 81-83 |
| 누를 때 볼륨 | 0.6f | 109 |

### SauceMiniGame (`Assets/Scripts/Unity/Cooking/SauceMiniGame.cs`)
| 상수 | 값 | 라인 |
|---|---|---|
| decreasePerPress | 2.5f | 29 |
| targetGauge (default) | 63 | 31 |
| target 정규분포 | min 20 / max 70 / mean 50 / σ 10 | 32-35 |
| stopMarkerXOffset | 0.35f | 39 |
| stopMarkerYRatioOffset | -0.04f | 41 |
| currentGauge (start) | 100 | 47 |
| waitingThreshold | 0.7f | 51 |
| tolerance | 3f | 53 |
| zeroScoreDiff | 20f | 55 |

### MixMiniGame (`Assets/Scripts/Unity/Cooking/MixMiniGame.cs`)
| 상수 | 값 | 라인 |
|---|---|---|
| width / height | 1f / 0.5f | 12-13 |
| fixedRotationDegrees | -30f | 16 |
| pressRequiringCount | 20 | 17 |
| idleClearTime | 2f | 18 |
| duration | 15 | 35 |
| lap period 비율 | ×0.9f | 115 |
| sweep 왜곡 | ×0.5f (sin) | 120 |

### SliceMiniGame (`Assets/Scripts/Unity/Cooking/SliceMiniGame.cs`)
| 상수 | 값 | 라인 |
|---|---|---|
| totalSlices / segmentsPerSlice | 6 / 20 | 19-20 |
| sliceRangeY | 4f | 21 |
| sliceMargin | 0.3f | 22 |
| tolerance | 0.1f | 23 |
| hintMoveSpeed | 15f | 24 |
| 시작 판정 완화 배율 | ×2 | 146 |

### ClickMiniGame (`Assets/Scripts/Unity/Cooking/ClickMiniGame.cs`)
| 상수 | 값 | 라인 |
|---|---|---|
| maxClickTarget | 20 | 16 |

### MiniGameAbstract / Manager (`Assets/Scripts/Unity/Cooking/`)
| 상수 | 값 | 파일:라인 |
|---|---|---|
| default duration | 5f | MiniGameAbstract.cs:26 |
| 스태미나 폴백 | 5 | MiniGameAbstract.cs:29 / MiniGameManager.cs:49 |
| destroy delay | 1f | MiniGameAbstract.cs:73 |
| minigame offset | `(0, 4.5f)` | MiniGameManager.cs:31 |

## 15. UI 임계치 / 게이지

| 파일:라인 | 상수 | 값 |
|---|---|---|
| `Assets/Scripts/Unity/UI/StaminaGauge.cs:16` | maxValue | 100 |
| `Assets/Scripts/Unity/UI/StaminaGauge.cs:18` | lowThresholdRatio | 0.4f |
| `Assets/Scripts/Unity/UI/StaminaGauge.cs:20` | severeThresholdRatio | 0.15f |
| `Assets/Scripts/Unity/UI/InventoryPageController.cs:39` | barExpiredRatio | 0.3f |
| `Assets/Scripts/Unity/UI/InventoryPageController.cs:40` | barWarningRatio | 0.6f |
| `Assets/Scripts/Unity/UI/InventoryPageController.cs:186` | maxDays fallback | 10 |
| `Assets/Scripts/Unity/UI/ValidationFeedbackUI.cs:22-23` | displayDuration / fadeDuration | 2.0f / 0.5f |
| `Assets/Scripts/Unity/UI/GaugeUI.cs:9` | animationSpeed | 5f |
| `Assets/Scripts/Unity/UI/TooltipController.cs:12` | hoverThreshold | 1.0f |
| `Assets/Scripts/Unity/UI/MoneyUI.cs:10` | smoothSpeed | 6f |

## 16. 튜토리얼

`Assets/Scripts/Unity/Tutorial/TutorialStepId.cs:7-22` — Welcome ~ Closing 총 **16단계** (id 1~16).

## 17. 대화 / 스킵

`Assets/Scripts/Unity/Mall/DialogueManager.cs`

| 상수 | 값 | 라인 |
|---|---|---|
| skipCooldown | 0.15f | 35 |
| holdThreshold | 0.3f | 36 |
| autoAdvanceInterval | 0.15f | 37 |
| npcBlipSfx 볼륨 | 0.7f | 246 |

## 18. 스태미나 회복

| 파일:라인 | 상수 | 값 |
|---|---|---|
| `Assets/Scripts/Unity/Common/ProgressService.cs:82` | PassDay 시 SetStamina | 100 |
| `Assets/Scripts/Unity/UI/PhaseActionSelector.cs:55, 63` | Phase 이동 시 SetStamina | 100 |

## 19. 기타 매직 넘버

| 파일:라인 | 상수 | 값 | 의미 |
|---|---|---|---|
| `Assets/Scripts/Unity/Cooking/BentoModel.cs:22` | maxFoodSlots | 4 | 도시락 최대 슬롯 |
| `Assets/Scripts/Unity/Cooking/FoodBehavior.cs:8` | speed | 10f | 이동 |
| `Assets/Scripts/Unity/Cooking/BentoBehavior.cs:10` | speed | 10f | 이동 |
| `Assets/Scripts/Unity/Cooking/CookingToolBehavior.cs:10` | speed | 10f | 이동 |
| `Assets/Scripts/Unity/Common/PlayerMove.cs:9,19` | moveSpeed / stepInterval | 5f / 0.22f | 플레이어 |
| `Assets/Scripts/Unity/Common/HorizontalCameraMove.cs:6-8` | edgeZone / moveSpeed / inputSmoothTime | 0.1 / 10f / 0.2f | 카메라 |
| `Assets/Scripts/Unity/Common/CameraFollow.cs:8` | smoothTime | 0.15f | 카메라 |
| `Assets/Scripts/Unity/Common/DynamicBoundaryWalls.cs:10` | wallThickness | 0.5f | 경계 |
| `Assets/Scripts/Unity/Common/LoadingManager.cs:13` | FADE_DURATION | 0.3f | 씬 페이드 |
| `Assets/Scripts/Unity/Cooking/CustomerManager.cs:129,289` | doorSfx 볼륨 | 0.7f | SFX |

## 20. 관찰 / 잠재 이슈

- `startingMoney=12000` vs `ManagementFee=1000` — 관리비 12일치 초기 자본.
- Tool 업그레이드 최고 5000+12000 = 17000 (5종 만렙 필요액 85000).
- Storage 만렙 총액: 60000 × 3종 = 180000. Farm 만렙 총액: 20000 × 3종 = 60000.
- 짧은 유통기한 재료 5종은 스포일 리스크 큼 (특히 I012, 2일).
- `CustomerData`에 patience/waitTime 필드 없음 — 대기 시간 캐릭터별 밸런싱 부재. 전부 `TimeManager.defaultCustomerWaitTime=90f`.
- Special 재료: 페이즈당 16개 후보 중 랜덤 4개, 각 stock 10.
- CustomerSpawner: `availableWaitingPositions` (5칸) ↔ `MaxWaitingCustomers=3` 불일치.
- BGM `bgm_preperation_theme.mp3` 애셋 존재하나 CatalogProvider에 미연결 (Preparation phase 진입 시 재생 클립 없음).
