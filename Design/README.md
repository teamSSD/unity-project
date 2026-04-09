# Game Design Documents

도시락 가게 경영 시뮬레이션 게임 기획 문서 모음.

## 기획 문서 (gdd/)

| 문서 | 설명 |
|------|------|
| [concept.md](gdd/concept.md) | 게임 컨셉, 장르, 시놉시스 |
| [core-loop.md](gdd/core-loop.md) | 코어 루프, 페이즈 구조, 자원 흐름 |
| [cooking-system.md](gdd/cooking-system.md) | 요리 시스템, 도구, 가격 산정, weight 체계 |
| [customer-system.md](gdd/customer-system.md) | 손님 시스템, 대기시간, 보상 공식 |
| [garden-system.md](gdd/garden-system.md) | 텃밭 시스템, 작물, 경제적 역할 |
| [progression.md](gdd/progression.md) | 진행도, 머니싱크, 보관소/레시피 해금 |
| [upgrade-system.md](gdd/upgrade-system.md) | 업그레이드 시스템 명세 (미구현) |
| [team.md](gdd/team.md) | 팀 구성 |

## 분석 도구 (tools/)

| 파일 | 설명 |
|------|------|
| [balance.py](tools/balance.py) | 레시피/경제 밸런스 분석 스크립트 |

```bash
cd Design/tools
python3 balance.py              # 전체 리포트
python3 balance.py --chain R009 # 레시피 체인 상세
python3 balance.py --compare    # 구/신체제 비교
```
