# Phase 2-E: DOMAIN.md + 용어 통일 rename

## 목표

도메인 용어집 `DOMAIN.md` 작성 + 결정된 모든 동의어/모호 용어 통일을 코드에 일괄 rename.

## 가설

용어 미정의 → 동의어 발생 (Customer/NPC, Order/DeliveryQuest, Menu/Recipe/Bento 등) → 코드 일관성 깨짐, 신규 클래스 명명 망설임. 한 번에 정리하면 향후 작업도 통일 유지.

## 측정 기준

| 지표 | 베이스라인 | 목표 |
|---|---:|---:|
| `DOMAIN.md` 존재 | No | Yes (프로젝트 루트) |
| 도메인 용어 정의 개수 | 0 | 20+ (예상) |
| 통일 결정 개수 | 0 | N (DOMAIN.md 작성 후 정해짐) |
| rename된 클래스/메서드 개수 | 0 | N (결정 의존) |
| 미통일 잔존 용어 (grep 검색) | n/a | 0 |
| 베이스라인 다른 지표 | (베이스라인) | **변화 없음** (rename은 동치 변환) |

## 검증 방법

1. **DOMAIN.md 사용자 검토** + 모호한 정의 확정 (필수)
2. **컴파일 통과**
3. **모든 EditMode 테스트 GREEN**
4. **미통일 용어 검색**: 예) Customer로 통일 후 `grep -r "NPC" --include="*.cs"` 결과가 의도된 NPC 정의 외 0
5. **씬/프리팹 열람**: 모든 씬 1회 열람 → rename으로 인한 깨짐 없음
6. **세이브 호환**: 기존 세이브 파일 로드 가능 (직렬화 필드 rename 시 `[FormerlySerializedAs]` 적용)
7. **베이스라인 재측정**: 라인/함수 분포 변동 거의 없음 (이름만 바뀜)

## 회귀 방지 체크리스트

- [ ] rename은 IDE Rename 도구 사용 (모든 참조 자동 갱신)
- [ ] `.meta` GUID 보존 (rename은 클래스명만, 파일명 변경 시 git mv)
- [ ] 직렬화 필드 rename 시 `[FormerlySerializedAs("oldName")]` 적용
- [ ] 한 PR로 일괄 처리 (부분 적용 금지)
- [ ] 세이브 호환 검증 (기존 세이브 로드 → 데이터 모두 정상)

## 추정 시간

| 단계 | 시간 |
|---|---|
| DOMAIN.md 초안 작성 (AI) | 0.5일 |
| 사용자 검토 + 모호한 정의 확정 | 0.5-1일 |
| 통일 결정 매트릭스 합의 | 0.5일 |
| rename 작업 (IDE) | 1-2일 |
| 회귀 검증 (씬, 세이브, 테스트) | 0.5일 |
| **합계** | **3-4일** |

## 선결 조건

- [x] Phase 2-A 완료 (안정된 폴더 구조)
- [x] ADR-006 ACCEPTED
- [ ] DOMAIN.md 사용자 검토 완료
- [ ] 통일 결정 매트릭스 사용자 승인

## PR 분할

**사전 작업** (Git 외부 또는 별도 PR):
- DOMAIN.md 초안 작성 → 사용자 검토 → 모호한 정의 확정 → 결정 매트릭스

**1 PR**:
- `DOMAIN.md` 파일 추가 (프로젝트 루트)
- 모든 rename 일괄 적용
- 필요 시 `[FormerlySerializedAs]` 어트리뷰트 추가

## 세부 작업

### 1. DOMAIN.md 초안 작성 (0.5일)

ADR-006 §"DOMAIN.md 구조"를 따라 작성. 위치: `DOMAIN.md` (프로젝트 루트).

작성 방식:
- AI가 코드 전체를 훑어 등장 용어 수집 (이미 부분 완료)
- 각 용어의 정의 후보 작성 (코드 + Design/ 문서 + Recipe_Guide.md 등 참조)
- 동의어/혼용 후보 표시
- 도메인 다이어그램 (ASCII 또는 mermaid)

산출물 골격:
```markdown
# Game Domain Glossary

## Core Concepts (요리 게임)
### Food
정의: ...
유형: GARBAGE / INGREDIENT / PROCESSING / MAIN / SIDE
관계: ...
코드: ...

### Recipe
...

### Bento
...

## Customer Domain
### Customer
...

## Progress / Stats Domain
### Day, Phase, Action, Stats, Settlement
...

## Shop / Upgrade Domain
### Shop, Upgrade, Storage, Crop, Farm, Tile
...

## Validation
...

## 결정된 용어 매핑
| 미통일 용어 | 통일 후 | rename 대상 |
|---|---|---|
| ... | ... | ... |

## 도메인 다이어그램
[ASCII 또는 mermaid]
```

### 2. 사용자 검토 (0.5-1일)

각 모호한 용어에 대해 사용자에게 결정 질문:
- "Menu vs Recipe vs Bento — 어떻게 구분?"
- "Customer vs NPC — 손님은 NPC의 일종? 별개?"
- "Order vs DeliveryQuest — 차이?"
- "Food vs Ingredient — 차이?"
- "Tool vs CookingTool — 동일?"
- "FoodSchema vs FoodData vs FoodModel — 어떻게 구분?"

질문 답변 시 DOMAIN.md 정의 확정.

### 3. 통일 결정 매트릭스

DOMAIN.md 끝에 추가:
```markdown
## 결정된 rename

| 현재 (코드) | 통일 후 | 이유 | 영향 |
|---|---|---|---|
| FoodSchema → ? | FoodState | Schema는 정의, State는 인스턴스 | Schema/State/Cooking/FoodState.cs |
| MenuSchema → ? | MenuState | 동일 이유 | Schema/State/Cooking/MenuState.cs |
| Customer / NPC 혼용 | Customer (가게 손님), NPC (그 외 캐릭터) | 명확한 역할 분리 | OrderingCustomer, WaitingCustomer 등은 그대로 |
| Order / DeliveryQuest | Order (가게 주문), DeliveryQuest (배달 퀘스트) | 다른 흐름 | OrderManager는 그대로 |
| ... | ... | ... | ... |
```

### 4. rename 실행 (1-2일)

각 결정을 IDE Rename 도구로 일괄 적용:
- Visual Studio: F2 (Rename Symbol)
- Rider: Shift+F6 (Rename)
- 자동으로 모든 참조 갱신

**.unity/.prefab 안의 컴포넌트 이름 영향**:
- 컴포넌트는 .meta GUID로 추적되므로 클래스명 rename 시 영향 없음
- 단, 직렬화 필드명 rename 시 `[FormerlySerializedAs("oldName")]` 적용 필수

**직렬화 필드 rename 예시**:
```csharp
// Before
[Serializable]
public class PhaseData {
    public List<string> unlockedFoodIds;  // 기존 세이브 파일에 이 이름으로 저장됨
}

// After
[Serializable]
public class PhaseData {
    [FormerlySerializedAs("unlockedFoodIds")]
    public List<string> unlockedFoods;
}
```

### 5. 검증

```bash
# 1. 컴파일
# Unity Editor 열어서 Console 에러 0

# 2. EditMode 테스트
# Window → Test Runner → Run All

# 3. 미통일 검색 (예: NPC → Customer 통일 후)
grep -rE "\bNPC\b" Assets/Scripts --include="*.cs"
# 결과: 의도된 NPC 정의 외 0

# 4. 세이브 호환
# 기존 세이브 파일로 Continue → 데이터 모두 정상

# 5. 모든 씬 열람
# Mall, Cooking, Garden, Shop, Settlement 각 1회 → 분홍 머티리얼/누락 컴포넌트 없음

# 6. 베이스라인
bash tmp/refactor-2026-05/harness/run_all.sh
diff tmp/refactor-2026-05/reports/history/<prev>.md tmp/refactor-2026-05/reports/baseline.md
# 변동 거의 없어야 함 (이름만 바뀜)
```

### 6. 위험 신호

- 컴파일 에러 다수: rename이 외부 참조까지 닿지 못함 — 수동 검색/수정 필요
- 세이브 로드 시 데이터 누락: `[FormerlySerializedAs]` 누락
- 씬 열기 시 분홍 머티리얼: 클래스명 rename으로 ScriptableObject GUID 매핑 깨짐 (드뭄)
- 미통일 잔존: 통일 매트릭스에 포함 안 된 용어 발견 — DOMAIN.md 보강 후 추가 rename
