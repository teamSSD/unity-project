# WebGL 실제 E2E 실행 가이드

이 절차는 Unity 내부 시뮬레이션이 아니라, 브라우저에 WebGL을 띄우고 실제 키보드·마우스 입력을 보내는 smoke test다.

## 빌드와 격리

튜토리얼 실제 입력 smoke는 `Tools > Build > WebGL — E2E (Development)`를 사용한다. 장기 정책 운영은 `Tools > Build > WebGL — E2E Long-run (Development)`를 사용하며, 튜토리얼 완료·seed 42 상태로 시작한다.

빌드 폴더는 일반 개발 서버와 다른 포트로 제공한다. 예를 들어 새 터미널에서 다음처럼 실행한다.

```sh
python3 -m http.server 8100 -d "Builds/WebGL/0.1.0_e2e/<timestamp>"
```

포트가 다르면 브라우저 origin이 달라져 IndexedDB/LocalStorage 세이브도 일반 플레이와 분리된다. 기존 사용자 세이브를 삭제하거나 덮어쓰지 않는다.

## 브라우저 테스트 API

Development 출력에서만 `window.AftertasteE2E`가 생긴다.

```js
AftertasteE2E.clear();
AftertasteE2E.command({ action: "snapshot", label: "booted" });
AftertasteE2E.command({ action: "mark", label: "before-shop" });
AftertasteE2E.command({ action: "timeScale", value: 2 });
AftertasteE2E.events;
```

`events`에는 부팅, 씬 전환, 상태 스냅샷, Unity Error/Exception/Assert가 최대 500개까지 순서대로 남는다. 릴리스 빌드에서는 이 객체가 존재하면 안 된다.

## 자동 smoke journey

`Tools/e2e/webgl-smoke.spec.mjs`는 Playwright가 브라우저를 새 profile로 열고, 실제 Canvas 클릭과 키보드 `Space`를 보낸다. 이동·메뉴·미니게임을 테스트 API로 통과시키지 않는다.

최초 1회만 의존성과 Chromium을 설치한다.

```sh
npm install
npx playwright install chromium
```

E2E 전용 빌드를 위의 별도 포트로 제공한 뒤 실행한다. runner는 `localhost`/`127.0.0.1`의 `8100`–`8199` 포트만 허용하고, E2E 전용 빌드 표식이 없으면 실패한다. 일반 개발 origin을 잘못 지정해 사용자 저장소를 건드리는 것을 막기 위한 안전장치다.

```sh
E2E_WEBGL_URL=http://localhost:8100/ npm run test:e2e:webgl
```

사람이 실제 입력 과정을 보려면 `--headed`와 유지 시간을 준다.

```sh
E2E_WEBGL_URL=http://localhost:8100/ E2E_HOLD_OPEN_MS=20000 npm run test:e2e:webgl -- --headed
```

Playwright report의 test output에는 `01-boot.png`, `02-new-game-click.png`, `03-space-input.png`, `e2e-events.json`, `browser-console.json`이 남는다. 통과 기준은 부팅 snapshot이 하나 이상 있고 브라우저 오류가 없으며, 실제 클릭과 `Space` 입력 직후의 구조화 snapshot이 순서대로 남고 Canvas 화면이 각각 이전 화면과 달라지는 것이다.

## 수동 smoke journey

1. 새 격리 origin에서 게임을 연다.
2. 화면을 캡처하고 `snapshot: booted`를 남긴다.
3. 실제 `Space`, `A/D` 또는 방향키, 화면 클릭으로 튜토리얼/이동을 진행한다.
4. 각 체크포인트에서 `mark`와 `snapshot`을 남기고, 화면과 `events`의 scene·UI lock·시간·소지금을 함께 확인한다.
5. 새로고침 뒤 부팅/저장 동작을 확인한다.
6. 캡처, events JSON, 브라우저 콘솔 오류, 총 시간을 하나의 결과로 보관한다.

테스트 편의를 위한 시간 배율 변경은 허용하지만, 이동·화면 전환·메뉴 선택·미니게임 결과를 API로 만들어 통과시키면 안 된다.
