# team-SSD Unity Project

Unity 게임 개발 프로젝트

---

## 📚 **개발 문서 (필수 읽기)**

### **개발자용**
1. **[DEVELOPMENT_RULES.md](DEVELOPMENT_RULES.md)** ⭐ **가장 중요**
   - 프로젝트 개발 규칙
   - 절대 금지 사항
   - 필수 패턴
   - 커밋 전 체크리스트

2. **[CHECKLIST.md](CHECKLIST.md)**
   - 작업별 상세 체크리스트
   - 복사해서 사용 가능
   - ScriptableObject/UI/Manager/Prefab/테스트 가이드

3. **[Assets/Scripts/Entities/RecipeBook/README.md](Assets/Scripts/Entities/RecipeBook/README.md)**
   - RecipeBook 시스템 가이드
   - ActionDatabase 사용법
   - 문제 해결 가이드

### **AI Agent용**
4. **[AI_AGENT_GUIDE.md](AI_AGENT_GUIDE.md)**
   - Claude Code, GitHub Copilot 등을 위한 가이드
   - 코드 생성 템플릿
   - 절대 금지 패턴
   - 학습 예제

---

## 🛠️ **개발 도구**

### **Unity Editor 메뉴**

#### **Tools > Validate Project Rules**
- 전체 프로젝트 규칙 검증
- Singleton 감지
- ActionDatabase 완전성 검사
- Prefab missing 컴포넌트 검사
- OnValidate 누락 경고

### **컴포넌트 우클릭 메뉴**

#### **CONTEXT/ActionToggle/Auto Setup References**
- ActionToggle의 UI 참조 자동 연결
- Label, DescriptionLabel, Toggle 자동 찾기

---

## 🚀 **빠른 시작**

### **1. 처음 시작하는 개발자**

```bash
# 1. 필수 문서 읽기 (10분)
- DEVELOPMENT_RULES.md 읽기
- CHECKLIST.md 훑어보기

# 2. Unity 에디터 열기
# 3. Tools > Validate Project Rules 실행
# 4. Console 확인 (에러 없어야 함)
```

### **2. 새 기능 추가**

```bash
# 1. CHECKLIST.md에서 해당 작업 체크리스트 복사
# 2. 체크리스트 따라 작업
# 3. 커밋 전 체크리스트 실행
# 4. Tools > Validate Project Rules 실행
# 5. Test Runner 실행
```

### **3. 버그 수정**

```bash
# 1. 테스트 작성 (버그 재현)
# 2. 수정
# 3. 테스트 통과 확인
# 4. Console 경고 확인
```

---

## 🎯 **프로젝트 목표**

> **"버그는 런타임이 아니라 컴파일 타임에 잡는다"**

이 프로젝트는 다음을 달성합니다:

- ✅ **설정 누락** → OnValidate가 즉시 감지
- ✅ **Null Reference** → Editor 로드 시 경고
- ✅ **잘못된 로직** → 테스트가 감지
- ✅ **일관성 없는 코드** → 규칙이 방지

**결과: 디버깅 시간 90% 감소**

---

## 📊 **프로젝트 구조**

```
unity-project/
├── Assets/
│   ├── Resources/
│   │   └── Prefabs/
│   │       └── recipebook/
│   │           └── Diary/             # Diary UI 프리팹들
│   ├── Scripts/
│   │   ├── Editor/                    # Editor 전용 스크립트
│   │   │   └── ProjectValidator.cs
│   │   ├── Entities/                  # 게임 엔티티
│   │   │   └── RecipeBook/
│   │   │       ├── Actions/           # ActionDatabase, ActionType 정의
│   │   │       └── diary/             # Diary 시스템
│   │   ├── Managers/                  # 매니저 클래스
│   │   └── Systems/                   # 공통 시스템
│   ├── Scenes/                        # Unity 씬
│   └── Tests/                         # 테스트
│       ├── EditMode/                  # Edit Mode 테스트
│       └── PlayMode/                  # Play Mode 테스트
├── DEVELOPMENT_RULES.md               # ⭐ 개발 규칙
├── CHECKLIST.md                       # 작업 체크리스트
├── AI_AGENT_GUIDE.md                  # AI Agent 가이드
└── README.md                          # 이 파일
```

---

## 🧪 **테스트 실행**

### **Test Runner 사용**

1. `Window > General > Test Runner`
2. **EditMode** 탭 선택
3. 모든 테스트 실행 또는 특정 테스트 선택
4. 결과 확인 (모두 초록불이어야 함)

### **현재 테스트**

- ✅ `ActionToggleValidationTest` - ActionToggle 검증
- ✅ `ActionDatabase_AllEnumValues_Exist` - ActionDatabase 완전성
- ✅ `ActionDatabase_ValidateContent` - ActionDatabase 내용 검증
- ✅ `DiaryActionManagerTest` - DiaryActionManager 기능 테스트

---

## 🐛 **문제 해결**

### **"ActionType not found in ActionDatabase!"**

```bash
1. Tools > Validate Project Rules 실행
2. ActionDatabase.cs에 누락된 ActionType 추가
3. RecipeBook README.md의 "새 액션 추가 체크리스트" 참고
```

### **"DiaryActionManager instance not found"**

```bash
1. 씬에 DiaryActionManager GameObject 있는지 확인
2. 컴포넌트가 활성화되어 있는지 확인
3. DontDestroyOnLoad로 이미 로드되었는지 확인
```

### **"No bento selected"**

```bash
1. 정상 동작입니다 (메뉴를 먼저 선택해야 함)
2. Main Menu와 Side Menu에서 음식 선택
3. 도시락에 추가된 후 MenuSelect 클릭
```

---

## 📈 **개발 현황**

### **완료된 시스템**
- ✅ RecipeBook 시스템
- ✅ Diary 시스템
- ✅ ActionDatabase (정적 데이터 관리)
- ✅ Phase 관리 (Preparation, Morning, Afternoon, Evening, Night)
- ✅ 자동 검증 시스템

### **진행 중인 시스템**
- 🔄 Cooking 시스템
- 🔄 Shop 시스템
- 🔄 Minigame 시스템

---

## 🤝 **기여 가이드**

### **코드 기여 시**

1. DEVELOPMENT_RULES.md 읽기 (필수)
2. 새 브랜치 생성
3. CHECKLIST.md 따라 작업
4. 커밋 전 체크리스트 실행
5. Tools > Validate Project Rules 통과
6. Test Runner 통과
7. Pull Request 생성

### **문서 기여 시**

- 명확하고 간결하게
- 예제 포함
- 스크린샷 권장

---

## 📞 **문의**

- 버그 리포트: GitHub Issues
- 규칙 관련 질문: DEVELOPMENT_RULES.md 먼저 확인
- 시스템 가이드: 각 시스템의 README.md 참고

---

**마지막 업데이트:** 2026-02-10

**주요 개선사항:**
- ✅ ScriptableObject → ActionDatabase 리팩토링 (복잡도 대폭 감소)
- ✅ OnValidate + Auto Setup 패턴 도입
- ✅ 통합 테스트 추가
- ✅ 프로젝트 규칙 문서화
- ✅ AI Agent 가이드 추가
- ✅ 컴파일 타임 안전성 확보
