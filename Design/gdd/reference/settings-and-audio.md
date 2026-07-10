# Aftertaste — Settings / Audio / Font 정리

프로젝트의 설정 UI, 오디오 시스템, 폰트 프리와밍 관련 코드 및 애셋 전수 조사.

## 1. Settings UI

### SettingsUIManager (`Assets/Scripts/Unity/Common/SettingsUIManager.cs`, 180 lines)

`SingletonMonoBehaviour<SettingsUIManager>` 기반. **패널의 오픈/클로즈 + backdrop 스타일** 만 담당하고, 실제 슬라이더/셀렉터 로직은 `SettingsController`(Settings.prefab 부착)에 위임.

| 역할 | 세부 |
|---|---|
| Canvas 구성 | 런타임 생성 `SettingsCanvas` (Overlay, sortingOrder=100, 1920×1080 reference) |
| Settings 패널 소스 | `CatalogProvider.Prefabs.settings` → `Assets/Bundles/prefabs/ui/Settings.prefab` |
| Backdrop (GameStart 씬) | solid black `Image` |
| Backdrop (그 외 씬) | `Camera.main` 프레임을 RT로 캡처 → **Dual Kawase Blur** (4 iter, downsample pass0 → upsample pass1 → tint pass2) → `RawImage` |
| Blur 머티리얼 | `Resources.Load<Material>("UI/UIBlurBackdrop")` (없으면 solid로 폴백) |
| ESC 처리 | Update() 매 프레임 — 열려있으면 Close, 다른 modal이 방금 소비했으면 skip (`prevLocked` 캡처), GameStart 씬 아니면 Open |
| Open 부수효과 | `UILockManager.Lock(Owner.Settings)` + `TimeManager.Instance.PauseTime()` |
| Close 부수효과 | `UILockManager.Unlock` + `TimeManager.Instance.ResumeTime()` |
| RT 관리 | 화면 크기 변화 시 lazy 재생성, `OnDestroy`에서 Release |

SettingsUIManager 자체는 SerializeField 노출 없음.

### SettingsController (`Assets/Scripts/Unity/UI/SettingsController.cs`, 96 lines)

Settings.prefab에 부착. 사용자 조작 + 저장/로드.

| SerializeField | 타입 | 역할 |
|---|---|---|
| `effectSlider` | `Slider` | SFX 볼륨 → `SoundManager.SetSFXVolume` |
| `musicSlider` | `Slider` | BGM 볼륨 → `SoundManager.SetBGMVolume` |
| `resolutionSelector` | `ScreenResolutionSelector` | 해상도 prev/next |
| `fullScreenSelector` | `FullScreenSelector` | 창/전체화면 토글 |
| `closeButton` | `Button` | `SettingsUIManager.Instance.Close()` |
| `resetButton` | `Button` | 기본값 복귀 후 저장 |

**저장 위치**: `Application.persistentDataPath + "/saves/settings"` (JSON, `DataSaveUtil` 경유). PlayerPrefs 미사용.

### SettingsSaveData 필드
```csharp
public float effectVolume = 1f;
public float musicVolume = 1f;
public int   resolutionIndex = 2;   // 1920x1080 default
public bool  fullScreen = true;
```

> **주의**: 종료(Quit) 버튼 관련 코드 (`Application.Quit`, `quitButton`) 프로젝트 전체 grep에서 발견되지 않음. 재시작/블러 등의 기능은 위에 명시된 것 이상은 코드로 확인 안됨.

### 하위 셀렉터

| 파일 | 특징 |
|---|---|
| `Assets/Scripts/Unity/UI/ScreenResolutionSelector.cs` | 4단 배열: 1280×720 / 1600×900 / 1920×1080 / 2560×1440. `Screen.SetResolution(w,h,Screen.fullScreen)` 즉시 적용. `OnChanged` 이벤트로 save 트리거 |
| `Assets/Scripts/Unity/UI/FullScreenSelector.cs` | "창 화면" / "전체화면" 2단. prev/next 둘 다 `Toggle`에 바인딩 (동일 동작) |

## 2. SoundManager

**파일**: `Assets/Scripts/Unity/Common/SoundManager.cs` (262 lines)  
`SingletonMonoBehaviour<SoundManager>`. 이전에 존재했던 `UISoundManager` / `GlobalButtonSfxManager`는 완전히 흡수됨 (주석 잔존만 존재).

### AudioSource 필드
| 필드 | loop | 용도 | 생성 방식 |
|---|---|---|---|
| `bgmSource` (public) | true | 씬별 BGM | `CreateAudioSource("BGMSpeaker", loop:true)` — 런타임 생성 |
| `sfxSource` (public) | false | 일회성 SFX `PlayOneShot` | `CreateAudioSource("SFXSpeaker", loop:false)` |
| `loopSfxSource` (private) | true | 조리 미니게임 루프 SFX (페이드 인/아웃 지원) | `CreateAudioSource("LoopSFXSpeaker", loop:true)` |

### UI SFX SerializeField
```csharp
[Header("UI SFX")]
[SerializeField] private AudioClip uiBookSfx;
[SerializeField] private AudioClip buttonClickSfx;
```
→ 래퍼 `PlayUIBook()`, `PlayButtonClick()`. 그 외 SFX는 각 컴포넌트가 `AudioClip`을 SerializeField로 소유 후 `Play2DSFX(clip, volume)` 호출 — **분산 소유 모델**.

### BGM 매핑 (CatalogProvider 경유, `SelectBGMClip()`)

| 조건 | 재생 클립 |
|---|---|
| scene == "Boot" \|\| scene == "GameStart" | null (BGM 정지) |
| `Progress.PhaseData.Phase == Night` | `CatalogProvider.BgmNight` |
| scene == "Cooking" | `CatalogProvider.BgmCooking` |
| 그 외 (Mall/Home 등) | `CatalogProvider.BgmMall` |

**BGM 갱신 트리거**: `SceneManager.activeSceneChanged`, `Progress.OnPhaseChanged`.

### 볼륨 API
```csharp
public void SetBGMVolume(float v)     => bgmSource.volume = Mathf.Clamp01(v);
public void SetSFXVolume(float v)     => sfxSource.volume = Mathf.Clamp01(v);
public void SetLoopSFXVolume(float v) => loopSfxSource.volume = Mathf.Clamp01(v);
```
`loopSfxSource`는 별도 채널 — SFX 슬라이더에 연동되지 않음 (설정 UI에 노출 안 됨).

### AudioListener gating (경고 스팸 방지)
- `OnSceneLoaded`: 다음 프레임에 listener 재스캔, 없으면 `SetSourcesEnabled(false)`
- `OnSceneUnloaded`: 즉시 `bgmSource.Pause` + 소스 비활성
- `Update()`: 0.3초 폴링 → listener 유무로 자동 Play/Pause

### 버튼 자동 등록 (Global button click SFX)
`RegisterButtons(Transform root)`:
- root=null → 전체 `Button` 탐색 (`FindObjectsInactive.Include`)
- `GetInstanceID`로 중복 방지 (`_registeredButtons`)
- Slider 자식 Button(prev/next 화살표)은 제외
- 각 버튼 `onClick`에 `PlayButtonClick` 리스너 추가
- 씬 로드 시 다음 프레임에 자동 스캔 (`ScanButtonsNextFrameAsync`)

### 루프 SFX
- `PlayLoopSFX(clip, fadeIn=0.2f)` / `StopLoopSFX(fadeOut=0.2f)`
- `CancellationTokenSource` 재발급으로 fade 겹침 방지
- 사용처: `FireMiniGame`, `GriddleMinigame`

### 실 사용처 (grep 요약)

| API | 사용처 |
|---|---|
| `PlayUIBook` | BentoSelectionController, ShopUIAdapter, RecipeBookManager, DialogueManager (책/카드/대화창 열림음) |
| `PlayButtonClick` | 모든 Button에 자동 부착 |
| `Play2DSFX` | Slice/Sauce/Mix/Griddle 미니게임, CustomerManager(도어벨), BentoModel(도시락 배치/폐기), CookingToolModel(폐기), RefrigeratorBehavior(문 열림/닫힘), OrderTicketModel(영수증 부착), TimeManager(시계 tick), TakingCustomer, InventoryPageController(폐기), PlayerMove(발소리), StatsAudioAdapter(캐시 서랍) |

## 3. 오디오 애셋 목록

**경로**: `Assets/Bundles/Sound/`

### BGM (`bgm/`)

| 파일명 | Catalog 필드 | 상태 |
|---|---|---|
| `bgm_mall_theme.mp3` | `CatalogProvider.BgmMall` | 연결됨 |
| `bgm_night_theme.mp3` | `CatalogProvider.BgmNight` | 연결됨 |
| `bgm_preperation_theme.mp3` | — | **미연결** (필드 부재) |

Cooking 씬은 자체 클립 `CatalogProvider.BgmCooking` (별도 파일명 — 정확히는 애셋 참조로 확인 필요).

### SFX (`sfx/`, 20종)

| 파일명 |
|---|
| `sfx_cash_drawer.mp3` |
| `sfx_clock_tick.mp3` |
| `sfx_conversation_npc.mp3` |
| `sfx_cook_boilingpot.mp3` |
| `sfx_cook_bowl.mp3` |
| `sfx_cook_cuttingset.mp3` |
| `sfx_cook_ironplate.mp3` |
| `sfx_doorbell_ring.mp3` |
| `sfx_foodtray_drop.mp3` |
| `sfx_foodtray_pick.mp3` |
| `sfx_paper_attach.mp3` |
| `sfx_paper_fly.mp3` |
| `sfx_refrigerator_close.mp3` |
| `sfx_refrigerator_open.mp3` |
| `sfx_trashcan_put.mp3` |
| `sfx_trashcan_put 1.mp3` | ⚠️ 중복 의심 (스페이스+1) |
| `sfx_ui_book.mp3` |
| `sfx_ui_button_click.mp3` |
| `sfx_walk.mp3` |

## 4. 오디오 Import 설정 (실측)

`AudioImporter` `.meta` 필드 참조: `loadType` (0=DecompressOnLoad / 1=CompressedInMemory / 2=Streaming), `compressionFormat` (0=PCM / 1=ADPCM / 2=Vorbis), `quality` (0.0~1.0)

| 파일 | loadType | compressionFormat | quality | preloadAudioData |
|---|---|---|---|---|
| `bgm_mall_theme.mp3.meta` | **1 (CompressedInMemory)** | 2 (Vorbis) | **0.6** | 0 |
| `bgm_night_theme.mp3.meta` | **1 (CompressedInMemory)** | 2 (Vorbis) | **0.6** | 0 |
| `sfx_ui_book.mp3.meta` | **0 (DecompressOnLoad)** | 2 (Vorbis) | **1.0** | 0 |
| `sfx_ui_button_click.mp3.meta` | **0 (DecompressOnLoad)** | 2 (Vorbis) | **1.0** | 0 |
| `sfx_walk.mp3.meta` | **0 (DecompressOnLoad)** | 2 (Vorbis) | **1.0** | 0 |

**정책 요약**:
- BGM: **Compressed In Memory + Vorbis Q0.6** (Streaming 아님)
- SFX: **Decompress On Load + Vorbis Q1.0**
- 공통: mono 변환 없음 (`forceToMono=0`), 44100Hz override, 3D 사운드 off, normalize on

## 5. Font 애셋 (SUIT SDF)

### SDF Font Assets — 모두 Dynamic 모드

경로: `Assets/Resources/TextMesh Pro/Fonts/`

| SDF 애셋 | 소스 TTF | AtlasPopulationMode |
|---|---|---|
| `SUIT-Regular SDF.asset` | `SUIT-Regular.ttf` | **1 (Dynamic)** |
| `SUIT-Medium SDF.asset` | `SUIT-Medium.ttf` | **1 (Dynamic)** |
| `SUIT-SemiBold SDF.asset` | `SUIT-SemiBold.ttf` | **1 (Dynamic)** |
| `SUIT-Bold SDF.asset` | `SUIT-Bold.ttf` | **1 (Dynamic)** |
| `SUIT-ExtraBold SDF.asset` | `SUIT-ExtraBold.ttf` | **1 (Dynamic)** |

**공통 CreationSettings**:
- `pointSize: 72`, `padding: 7`, `paddingMode: 1`
- `atlasWidth: 4096`, `atlasHeight: 4096`
- `characterSequence: 32-126, 44032-55203, 12593-12643, 8200-9900` (ASCII printable + Hangul Syllables + Hangul Jamo + General Punctuation — 초기 정적 베이킹 흔적, 현재 Dynamic이라 실 아틀라스는 pre-warm이 채움)
- `renderMode: 4165` (SDFAA)

**추가 애셋**: `Assets/Resources/TextMesh Pro/Resources/Fonts & Materials/SUIT-Variable SDF.asset` (variable font, TMP 기본 위치. 위 5개와 별도, FontPreWarmer 대상 아님)

## 6. FontPreWarmer

**파일**: `Assets/Scripts/Unity/Common/FontPreWarmer.cs` (44 lines) — static 유틸.

| 항목 | 값 |
|---|---|
| 대상 폰트 5종 | `TextMesh Pro/Fonts/SUIT-Regular/Medium/Bold/SemiBold/ExtraBold SDF` (Resources 경로) |
| 문자 소스 | `Resources.Load<TextAsset>("font_prewarm_chars")` → `Assets/Resources/font_prewarm_chars.txt` (**1874 chars**) |
| API | `font.TryAddCharacters(chars.text, out _)` × 5 폰트 |
| 재호출 안전 | `_done` static flag로 최초 1회만 |
| 호출 위치 | `BootLoader.Start()` line 20 — `ManagerBootstrap.EnsureAll()` 직후, `GameStart` 씬 로드 **전** |
| 실패 시 | txt 없거나 빈 경우 `LogWarning` + skip (fatal 아님) |

### 예열 문자셋 (`font_prewarm_chars.txt`, 1874자)
- 선두: ASCII printable (0x20~0x7E) + 통화/기호 (`¥°·–—''""…₩€℃←↑→↓▶★☆♥♪♬✓✗「」『』`) + 한글 음절 정렬

### 문자 수집 알고리즘 (`Assets/Editor/_TempGenerateFontPrewarm.cs`)
Editor 메뉴: **Tools/Build/Generate Font Prewarm Chars**

1. **기본 ASCII printable + 통화/구두점 리터럴** (32~126 + 한글용 기호 세트)
2. **모든 ScriptableObject의 string 필드** (Reflection 순회 — `string`/`List<string>`/`string[]` 지원, private 포함) — FoodData/RecipeData/DeliveryNpcData 등에서 등장 문자 흡수
3. **TextAsset** 중 경로에 `driveAssets/` 또는 `Bundles/` 포함되는 CSV/JSON
4. **모든 Prefab의 `TMP_Text` 컴포넌트 초기 텍스트** (고정 UI 라벨)
5. 제어 문자(< 32) 제외 후 정렬 → `Assets/Resources/font_prewarm_chars.txt` 저장

> **주의**: Scene 내 하드코딩 TMP 텍스트는 수집 대상 아님 (Prefab만). 새 메뉴명/대화 추가 후에는 재실행 → 재빌드 필요.

## 7. Boot 흐름

- `Assets/Scripts/Unity/Common/BootLoader.cs`:
  1. Managers 씬 additive 로드 + `SetActiveScene(Managers)`
  2. `ManagerBootstrap.EnsureAll()` — 싱글톤 매니저 생성
  3. `FontPreWarmer.WarmAll()` — SDF 폰트 pre-warm
  4. GameStart 씬 additive 로드
  5. Boot 씬 unload
- `Assets/Scripts/Unity/Common/ManagerBootstrap.cs`: `Ensure<SettingsUIManager>()` 등. SoundManager는 Managers 씬에 배치된 GameObject에 부착.

## 8. 요약 / 잠재 이슈

| 구분 | 관찰 |
|---|---|
| 설정 UI | Slider 2개(effect/music) + 해상도 셀렉터 + 창화면 셀렉터 + close + reset. **종료(Quit) 버튼 코드 없음** |
| 저장 | `persistentDataPath/saves/settings` JSON (PlayerPrefs 아님) |
| SoundManager 통합 | UISoundManager / GlobalButtonSfxManager는 완전히 흡수됨 |
| BGM 3종 wire | Mall/Cooking/Night만 Catalog에 assign. `bgm_preperation_theme.mp3` 애셋 존재하나 **미연결** |
| SFX 파일 중복 | `sfx_trashcan_put 1.mp3` (스페이스+1) 정리 후보 |
| BGM Import | **Compressed In Memory + Vorbis Q0.6** (Streaming 아님) |
| SFX Import | **Decompress On Load + Vorbis Q1.0** |
| Font | SUIT 5종 SDF 모두 Dynamic, atlas 4096², 초기 characterSequence는 legacy 흔적 |
| PreWarm | 1874 문자를 Boot에서 5개 폰트에 일괄 add. 재호출 안전 |
| PreWarm 미수집 | 씬 내 하드코딩 TMP 텍스트는 소스에서 빠짐 (프리팹만 수집) |
| loopSfx 볼륨 | `SetLoopSFXVolume` API는 있으나 설정 UI 슬라이더에 연동 안 됨 |
