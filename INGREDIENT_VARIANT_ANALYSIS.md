# 재료 Variant 이미지 분석 보고서

## 📊 현재 상태 요약

- **완성률**: 91% (79/87개 variant)
- **완성 요리**: 6개 / 11개 (55%)
- **남는 이미지**: 0개 (모든 이미지가 실제로 사용됨!)
- **추가 필요**: 8개 variant 이미지

---

## ✅ 구현 완료 사항

### 1. Tool-Specific Variant System 구현
- **FoodData.cs**: `GetImageForTool(string toolId)` 메서드 추가
  - Tool ID → Tool Type 매핑 (T001→pan, T002→pot, T003→bowl, T004→cut, T005→plate)
  - 자동으로 적절한 variant 스프라이트 로드
  - 누락 시 raw 이미지로 fallback

- **CookingToolModel.cs**: Tool-specific variant 사용
  - 재료를 도구에 넣을 때 해당 도구 타입의 variant 표시

- **BentoModel.cs**: Bowl variant 사용
  - 도시락에 음식 담을 때 bowl variant 표시

### 2. 레시피 최적화
- **R011** (구운 인공고기): M007 (plate) → M001 (pan)
- **R023** (폐건물 삼각밥): M001 (pan) → M007 (plate)

### 3. 이미지 설정 통일
- 전체 156개 food 이미지: Single sprite + 250 pixels per unit

### 4. Unity Editor 검사 도구
- **IngredientImageConsistencyChecker.cs** 생성
- 메뉴: `Tools → Check Ingredient Image Consistency`
- 레시피와 이미지 정합성 자동 검사

---

## ❌ 누락된 Variant 이미지 (8개)

### 🔧 Pan (2개)
```
❌ item_grilledArtificialMeat_pan.png
   - 구운 인공고기
   - 사용: R011 (OUTPUT), R014 (INPUT)

❌ item_guryongNoodles_pan.png
   - 구룡면 (완성품)
   - 사용: R009 (OUTPUT)
```

### 🔧 Pot (2개)
```
❌ item_dawnHerb_pot.png
   - 새벽풀
   - 사용: R003 (INPUT)

❌ item_tofu_pot.png
   - 두부
   - 사용: R004 (INPUT)
```

### 🔧 Bowl (1개)
```
❌ item_ashenSoySauce_bowl.png
   - 잿빛 간장
   - 사용: R022 (INPUT)
```

### 🔧 Cut (2개)
```
❌ item_choppedInsulatingMushroom_cut.png
   - 다진 절연 버섯 (완성품)
   - 사용: R006 (OUTPUT), R007 (INPUT)

❌ item_dicedArtificialMeat_cut.png
   - 깍둑 썬 인공고기 (완성품)
   - 사용: R010 (OUTPUT), R011 (INPUT)
```

### 🔧 Plate (1개)
```
❌ item_caramelTopping_plate.png
   - 캐러멜 토핑
   - 사용: R019 (INPUT)
```

---

## 🍳 요리별 완성도

### ✅ 완성 (6개)
- 루미 젤리
- 구운바람콩
- 네온 샐러드
- 전력실 꼬치
- 옥상 오믈렛
- 환기구 연어구이

### ❌ 미완성 (5개)

#### 🍜 새벽국 (2개 누락)
- item_dawnHerb_pot.png - 새벽풀
- item_tofu_pot.png - 두부

#### 🍝 구룡면 (2개 누락)
- item_choppedInsulatingMushroom_cut.png - 다진 절연 버섯 (완성품)
- item_guryongNoodles_pan.png - 구룡면 (완성품)

#### 🍖 기계장 고기정식 (3개 누락)
- item_dicedArtificialMeat_cut.png - 깍둑 썬 인공고기 (완성품)
- item_grilledArtificialMeat_pan.png - 구운 인공고기 (완성품)
- *(위 2개가 다음 단계 재료로 사용됨)*

#### 🥩 스트리트 스테이크 49 (1개 누락)
- item_caramelTopping_plate.png - 캐러멜 토핑

#### 🍙 폐건물 삼각밥 (1개 누락)
- item_ashenSoySauce_bowl.png - 잿빛 간장

---

## 🎯 제작 우선순위

### 🔴 높음 (MAIN 요리)
1. **새벽국** (2개) - pot variant
2. **구룡면** (2개) - cut + pan variant
3. **기계장 고기정식** (3개) - cut + pan variant

### 🟡 중간
4. **스트리트 스테이크 49** (1개) - plate variant
5. **폐건물 삼각밥** (1개) - bowl variant

---

## 📁 Tool ID → Tool Type 매핑

```
T001 → pan (팬)
T002 → pot (냄비)
T003 → bowl (보울)
T004 → cut (도마)
T005 → plate (철판)
```

---

## 🔧 현재 동작

### INPUT (재료를 도구에 넣을 때)
```csharp
// CookingToolModel.cs line 115-117
string toolId = SchemaInstance.cookingToolData.id;
Sprite variantSprite = food.foodData.GetImageForTool(toolId);
BehaviorInstance.AddTexture(variantSprite);
```

### OUTPUT (완성품을 도구에 표시할 때)
레시피 완성 시 결과물도 해당 도구의 variant로 표시됨

### BENTO (도시락에 담을 때)
```csharp
// BentoModel.cs line 71-72
Sprite variantSprite = food.foodData.GetImageForTool("T003"); // T003 = bowl
BehaviorInstance.AddTexture(variantSprite, position);
```

---

## 🏁 다음 단계

1. **8개 variant 이미지 제작** (우선순위 순)
2. 이미지 추가 후 Unity에서 일괄 import (Single + 250 PPU)
3. Unity Editor에서 `Tools → Check Ingredient Image Consistency` 실행하여 검증
4. 각 요리 테스트하여 시각적 정합성 확인

---

## 📝 참고사항

- 누락된 variant는 자동으로 raw 이미지로 fallback되므로 게임은 작동함
- 하지만 시각적 일관성을 위해 8개 이미지 추가 권장
- 모든 기존 variant 이미지(79개)는 실제로 사용되고 있으므로 삭제 금지
- 레시피 수정으로 누락이 19개 → 8개로 감소 (58% 개선)

---

**생성일**: 2026-03-18
**버전**: 1.0
**상태**: 8개 이미지 제작 대기 중
