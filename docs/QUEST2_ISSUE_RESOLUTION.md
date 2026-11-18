# PETIT PARIS — AI 리뷰 기반 이슈 수정 및 Meta Quest 2 준비 보고서

작성: 2026-11-16 · 대상 커밋: 이 문서와 같은 커밋 (`main`)
작성 환경: 클라우드 Claude Code 세션 (Unity 에디터·Quest 2 없음). **이 문서의 모든 수정은 Unity 에디터 Play와 Quest 2 실기기에서 아직 실행해 보지 않았다.**

---

## 1. 프로젝트 소개

**PETIT PARIS**는 작은 쥐가 되어 약 12cm 눈높이로 파리 거리를 지나는 1인칭 VR 게임이다.
카페 테라스 → 빵집 앞 → 골목 → 센강 강변 → 비스트로 주방을 지나 **식탁 위 치즈**에 닿으면 끝난다.

핵심 경험
- 사람의 거대한 발걸음을 피하며 이동 (발 그림자·착지 예고·3D 발소리)
- 웨이터·셰프에게 들키면 도망치거나 숨기 (의자·벤치·팔레트·조리대 밑)
- 빵 부스러기와 황금 크루아상 수집, 별점(★ 도착 / ★ 부스러기 60% / ★ 황금 3개)
- 체크포인트와 리스폰
- 쥐 눈높이에서 보는 거대한 파리

기술: Unity 6000.6.9f1, URP 17.6, XR Interaction Toolkit 3.6.1, Input System 1.20.1, OpenXR.
세계는 실제 크기(미터)이고, 쥐 크기는 추적 공간 배율 0.1(`RatScaleConfig.trackingScale`)로만 표현한다. 씬 전체를 에디터 코드(`PetitParisLevelBuilder`)가 만든다.

## 2. 리뷰 방법

- **가상 페르소나 5명의 관점으로 AI가 코드를 정적 분석**한 리뷰다. 실제 사용자 테스트나 실기기 측정이 아니다.
- 리뷰 결과 25개 이슈(P1 10개) 중, 이번 작업은 P1을 먼저 고치고 P2·P3는 시스템을 다시 설계하지 않는 범위에서 개선했다.
- 리뷰 원문(`04-final-review-report.md`, `reports/01-project-analysis.md`)은 이 저장소에 없었다 (사용자 PC에만 있음).
  그래서 작업 요청서에 적힌 이슈 설명을 기준으로 **실제 코드에서 이슈가 있는지 먼저 다시 확인**한 뒤 고쳤다.
- 검증 방법: Unity 참조 어셈블리(UnityEngine 모듈) + 스텁으로 런타임 스크립트와 레벨 빌더를 컴파일 확인했다.
  XRI 타입을 직접 쓰는 파일(`PetitParisBuildKit`, `PetitParisValidator`, `RatComfortApplier`, `RatLocomotionController`, `RatJumpController`, `RatEyeCalibrator`)은 이 환경에서 컴파일할 수 없어 변경 부분을 직접 읽어 검토했다.

## 3. 발견한 이슈 (25개)

우선순위 P1은 작업 요청서 기준. P2와 P3의 구분은 리뷰 원문이 저장소에 없어 "P2/P3"로 적었다.

| ID | 유형 | 우선순위 | 내용 |
|---|---|---|---|
| ISSUE-01 | 게임 밸런스·UX | P1 | 추격(0.55~0.6m/s)이 쥐 최고 속도(약 0.5)보다 빠름, 숨을 곳 안내 없음, 추격 중에도 화살표가 원래 목표 |
| ISSUE-02 | VR 멀미·레벨 | P1 | 주방 널빤지 15cm, 0.3/0.6/0.9m 상자에서 떨어질 때 완충 없음, 공중 비네트 꺼짐 |
| ISSUE-03 | VR 편안함 | P1 | 달리기 비네트 없음, 게임 안 편안함 설정 없음 |
| ISSUE-04 | UI | P1 | HUD가 눈 아래 10cm라 바닥·널빤지에 가림, 배경 항상 표시 |
| ISSUE-05 | 피드백 | P1 | 실제로 실패했는데 "밟힐 뻔했어요", 원인 구분 없음, 메시지끼리 덮어씀 |
| ISSUE-06 | 온보딩 | P1 | 조작 안내가 이동하면 약 2초 만에 사라짐, 다시 볼 수 없음, 질주 조건 불명확 |
| ISSUE-07 | 레벨 난이도 | P1 | 시작 지점과 첫 부스러기가 웨이터 순찰로 바로 옆 |
| ISSUE-08 | 버그 (이벤트) | P1 | 빗자루 추격 이벤트가 이벤트 호출 순서에 따라 사망 후 다시 준비되지 않음 |
| ISSUE-09 | 안내 문구 | P1 | 힌트가 실제 규칙과 다름 ("발 사이를 질주로", "차양 밑은 안전" 등) |
| ISSUE-20 | 빌드 재현성 | P1 | OpenXR·XR Management 설정이 저장소에 없음, 비네트가 XRI 샘플 3.3.2 경로에 묶임 |
| ISSUE-10 | 진행 버그 | P2/P3 | 체크포인트를 건너뛰면 지나온 체크포인트로 되돌아가라고 안내 |
| ISSUE-11 | 이동 | P2/P3 | 질주와 달리기 속도 차이가 작음 (상한에 걸림) |
| ISSUE-12 | 버그 (페이드) | P2/P3 | 캘리브레이션 페이드와 리스폰 페이드가 겹침 |
| ISSUE-13 | 플랫폼 | P2/P3 | 일시 정지·포커스 상실 처리 없음 |
| ISSUE-14 | AI 판정 | P2/P3 | 벽 너머에서도 사람이 쥐를 발견 |
| ISSUE-15 | 피드백 | P2/P3 | 컨트롤러 진동·앞발 상호작용 없음 |
| ISSUE-16 | UI | P2/P3 | 발견 말풍선(높이 2.15m)이 쥐 시야 밖 |
| ISSUE-17 | 리스폰 | P2/P3 | 리스폰 방향·무적 시간 부족 (리스폰 직후 다시 발견) |
| ISSUE-18 | 안내 | P2/P3 | 수집물·별점 규칙 안내 부족 |
| ISSUE-19 | 레벨 | P2/P3 | 주방 구간에 체크포인트 없음 |
| ISSUE-21 | 성능 | P2/P3 | Quest용 URP 설정이 저장소에 적용되어 있지 않음 |
| ISSUE-22 | 문서 | P2/P3 | README 목표(센강)와 실제 목표(주방 치즈) 불일치 |
| ISSUE-23 | 현지화 | P2/P3 | 코드에서 만드는 한글(안전 구역 이름 등)이 폰트에 미리 등록되지 않음 |
| ISSUE-24 | 플레이 시간 | P2/P3 | 목표 3~5분을 넘길 수 있음 |
| ISSUE-25 | 게임 디자인 | P2/P3 | 웅크리기 활용도 낮음 |

## 4. 실제 수정한 이슈

상태값: **FIXED** 코드 수정 + 이 환경에서 가능한 검증(컴파일·코드 검토) 완료 / **PARTIAL** 일부 개선 / **PENDING** 미수정 /
**NEEDS_DEVICE_TEST** 코드 수정, Quest 2(또는 최소 PC 에디터 Play)에서 확인 필요 / **BLOCKED** 외부 설정·장비 필요.
**어떤 항목도 Unity 에디터 Play나 실기기에서 실행해 확인하지 않았다.**

### ISSUE-01 추격 밸런스와 숨기 안내 — NEEDS_DEVICE_TEST
- **문제**: 들키면 달리기로 도망칠 수 없고, 어디 숨어야 할지 모르며, 화살표는 계속 원래 목표를 가리켰다.
- **원인**: 추격 0.55~0.6m/s > 쥐 상한 0.5m/s(체감 상한 5 × 배율 0.1). 발견 후 반응 0.9초. 숨을 곳 안내 코드 없음.
- **수정**
  - 추격 속도 0.55~0.6 → **0.38~0.42**, 반응 0.9 → **1.3초**, 회전 110 → 90°/s. 편안함 모드에서는 추격 20% 느림
  - 쥐 질주 0.6, 상한 6.5(월드 0.65) → 질주로 확실히 따돌릴 수 있음 (ISSUE-11과 함께)
  - 들키면 HUD에 "들켰어요! 숨어요! / 숨을 곳: 의자 아래 (화살표 방향)" (가장 가까운 안전 구역 이름)
  - 쫓기는 동안 안내 화살표가 **초록색으로 가장 가까운 안전 구역**을 가리키고, 숨으면 사라짐
  - 추격 시작 전(발견 직후)에 숨어도 1.2초 뒤 놓치도록 수정 (전에는 숨어도 추격 시작)
- **파일**: `PedestrianAwareness.cs`, `RatGuideArrow.cs`, `RatSafeZone.cs`(`FindNearest`, `GuidePoint`), `PetitParisLevelBuilder.cs`
- **전/후**: 추격 0.55~0.6 vs 쥐 0.5 → 추격 0.38~0.42 vs 쥐 달리기 0.36·질주 0.6
- **검증**: 컴파일 ✓. Validator에 "추격 속도 ≤ 0.45" 점검 추가. 긴장감은 실제 플레이로 확인 필요

### ISSUE-02 주방 낙하와 멀미 — NEEDS_DEVICE_TEST
- **원인**: 널빤지 폭 0.15m에 옆 턱 없음, 0.9m 낙하에 완충 없음, `vignetteInAir = false`.
- **수정**
  - 공중 비네트 기본 **켬** (작은 턱에서 깜빡이지 않게 0.12초 지연)
  - 새 컴포넌트 `RatFallRecovery`: 땅을 떠난 높이보다 **0.2m(체감 2m)** 넘게 떨어지면 바닥에 닿기 전에 페이드 → 떨어지기 0.4초 전 서 있던 자리로 복귀 → "떨어질 뻔! 올라가던 자리로 돌아왔어요". 실패가 아니라 체크포인트·부스러기·사망 횟수에 영향 없음
  - 리스폰과 충돌 방지: 복귀 중에는 `RatRespawn.Kill`을 무시하고, 리스폰 중에는 복귀하지 않음
  - 널빤지 폭 0.15 → **0.25m**, 양옆 **3cm 턱** (CharacterController stepOffset 2cm보다 높음)
  - 카메라 흔들림·롤 없음 (페이드와 XR Origin 순간이동만)
- **파일**: `RatFallRecovery.cs`(새), `RatComfortSettings.cs`, `RatComfortApplier.cs`, `RatLocomotionController.cs`(`TimeAirborne`), `RatRespawn.cs`, `PetitParisGameplayKit.Plank`, `PetitParisBuildKit.BuildRig`
- **검증**: 컴파일 ✓(RatFallRecovery, RatRespawn). 낙하 체감·멀미는 Quest 2에서 확인 필요

### ISSUE-03 VR 편안함 설정 — NEEDS_DEVICE_TEST
- **수정**
  - `vignetteOnRun`(달리기 비네트) 추가, 기본 켬. 질주 비네트 유지. 켜고 끄는 전환은 XRI TunnelingVignette의 ease in/out
  - **편안함 모드**: X 메뉴에서 A(PC: Space)로 전환, 기기에 저장(`PlayerPrefs`). 켜면 이동 속도 ×0.7, 체감 상한 3.5, 걷기에도 비네트, 점프 끔, 추격 20% 느림
  - `RatComfortSettings`를 그대로 쓰고 `Effective…` 값으로 편안함 모드를 반영. 이전 에셋은 빌더가 v2 기본값으로 옮김(`MigrateToCurrent`)
- **런타임 적용 경로(코드 확인)**: `RatComfortRuntime.ComfortMode` → `RatLocomotionController`(속도·상한), `RatJumpController.IsEnabled`, `RatComfortApplier`(걷기 비네트), `PedestrianAwareness`(추격 속도). 매 프레임 읽으므로 메뉴에서 바꾸면 바로 적용
- **검증**: 컴파일 ✓(RatComfortSettings). 나머지는 코드 검토

### ISSUE-04 HUD 가독성 — NEEDS_DEVICE_TEST
- **수정**: HUD를 눈 아래 10cm → **눈 위 3.5cm(시선 위 약 8°)**, 화살표(눈 아래 약 12°)와 분리. 글자·배경을 **깊이 검사 없는 Overlay 재질**로 그려 바닥·널빤지·벽에 가리지 않음. 배경은 메시지가 있을 때만. 글자 상자 폭 0.34 → 0.26m(두 줄로 접힘), 따라오기 시작 각도 18° → 10° (PC 화면 오른쪽 잘림 대책)
- **파일**: `RatHud.cs`, `PetitParisLevelBuilder.cs`, `PetitParisBuildKit.MakeOverlayAll`(새)

### ISSUE-05 실패 피드백 — NEEDS_DEVICE_TEST (PC 에디터에서도 확인 가능)
- **원인**: `OnDied`가 원인과 상관없이 "밟힐 뻔했어요"를 출력, 부스러기 손실 메시지가 같은 프레임에 덮어씀.
- **수정**: 원인별 문장 + 재도전 힌트 (신발 "진한 그림자가 생기면 피해요", 자전거 "벨 소리가 멀어진 뒤", 빗자루 "팔레트 밑이나 상자 안", 바게트 "차양 밑으로", 낙하). **메시지 우선순위 큐**(실패 2 > 들킴·이벤트 1 > 일반 0): 낮은 메시지는 뒤로 미뤄 차례로 보임. 실패 메시지 4.5초
- **파일**: `PetitParisGameFlow.cs`(`PostMessage(message, priority, duration)`, `FailMessage`), `PetitParisText.cs`, `CrumbCollection.cs`, `FallingHazard.cs`

### ISSUE-06 초기 조작 안내 — NEEDS_DEVICE_TEST
- **수정**: 시작 안내는 **A(PC: Space)를 눌러야 닫힘**, 그동안 이동 잠금 (같은 프레임 점프 방지). 조작법을 Quest Touch 기준으로 다시 씀 ("왼쪽 스틱을 밀면서 오른쪽 트리거 = 질주"). **X(PC: Tab/H) 메뉴**: 일시 정지 + 조작법 + 규칙 + 편안함 모드
- **입력 충돌 확인**: X(왼손 primaryButton)는 기존 매핑에 없음. Y·왼쪽 ≡는 "길게 = 시점 맞추기"로 유지. 새 액션 `Rat/Menu` 추가
- **파일**: `PetitParisGameFlow.cs`, `RatInputReader.cs`, `PetitParisInput.inputactions`, `PetitParisText.cs`, `PetitParisLevelBuilder.cs`

### ISSUE-07 시작 구간 난이도 — NEEDS_DEVICE_TEST
- **수정**: 웨이터 A 출발 지연 3 → **25초**. 첫 부스러기 3개를 웨이터 순환로(z 0.2)에서 0.7m 이상 떨어진 남쪽 벽 쪽으로. 시작 지점에 "벽 틈은 안전해요 / 여기서 천천히 연습해요" 힌트. 안내 경로도 같은 쪽으로

### ISSUE-08 빗자루 이벤트 재준비 — FIXED (정적 검증)
- **원인**: 쥐가 죽으면 `PedestrianAwareness.ChaseEnded`가 먼저 오면 `running=false`가 되고, 뒤에 오는 `Died`는 `if (!running) return`으로 재준비를 건너뜀 → 구독 순서에 따라 이벤트가 영영 안 켜짐.
- **수정**: 상태 기계(Armed/Running/Done). 추격이 끝날 때 `RatRespawn.IsRespawning`(Died보다 먼저 켜짐)을 직접 보고 재준비 여부 결정. 같은 프레임에 순서가 바뀌어도 재준비. 추격자가 이미 추격을 멈췄는데 Running이면 정리하는 안전장치
- **검증**: 두 호출 순서를 모두 코드로 추적, 컴파일 ✓. 반복 사망은 에디터 Play로 확인 권장

### ISSUE-09 힌트와 실제 규칙 — FIXED (문구)
| 전 | 후 | 근거 (실제 코드) |
|---|---|---|
| 발 사이를 질주로 빠져나가요 / 오른쪽 트리거 | 발이 내려오기 전에 지나가요 / 스틱 + 오른쪽 트리거 = 질주 | 질주는 스틱 입력 0.3 이상 필요 (`RatDashController.minStickInput`) |
| B로 웅크리면 사람 눈에 덜 띄어요 | B로 웅크리면 느리지만 사람이 가까이 와야 알아채요 | 웅크리면 발견 거리 ×0.45, 속도 ×0.5 |
| 차양 위에서 바게트가 굴러떨어져요! 차양 밑은 안전해요 | (메시지) …차양 밑엔 안 떨어져요 (요리사 발은 조심) / (새 힌트) 차양 밑엔 바게트가 안 떨어져요, 요리사 발은 조심! | 차양 밑은 바게트만 안전, 빵집 셰프는 차양 밑도 지나감 |
| 오른쪽 트리거 순간 질주 | 왼쪽 스틱을 밀면서 오른쪽 트리거 질주 | 위와 같음 |
| 의자 아래는 안전해요 | 초록 표시가 있는 의자 아래는 안전해요 | 안전 구역 표시가 있는 의자만 |

### ISSUE-20 Quest 빌드 재현성 — PARTIAL (일부 BLOCKED: 에디터에서 설치 필요)
- **확인 결과**: `Packages/manifest.json`에 `com.unity.xr.openxr`, `com.unity.xr.management`가 **없다**. `Assets/XR/` 설정 폴더도 없다.
- **수정**
  - 비네트 프리팹을 `Samples/XR Interaction Toolkit/3.3.2/...` 고정 경로 대신 **프로젝트 전체에서 이름으로 검색** (`FindVignettePrefabPath`)
  - Validator에 Quest 빌드 점검 추가: XR 설정 파일, Android 로더 = OpenXR, **Oculus Touch Controller Profile**(Quest 2), Meta Quest Support, ARM64, IL2CPP, Linear, 비네트 프리팹, Mobile URP 값, 편안함 설정 버전. 패키지가 없어도 컴파일되도록 설정 파일을 글자로 읽어 점검
  - `PetitParis.Editor.asmdef`에 URP 런타임 참조 추가 (이전 커밋, Safe Mode 원인)
- **패키지 버전은 바꾸지 않았다**: OpenXR 버전을 Unity 6000.6.9f1에서 확인하지 않고 manifest에 적는 대신, 에디터의 XR Plug-in Management가 호환 버전을 설치하게 했다 (6절)
- **저장소에 넣을 수 있는 것 / 에디터에서 해야 하는 것**

| 저장소에 포함 (이번 커밋) | 에디터에서 직접 (6절) → 끝나면 커밋 |
|---|---|
| Player Settings 적용 메뉴(0번), Mobile URP 값, 입력 액션, 빌더·Validator | XR Plug-in Management 설치, Android 탭 OpenXR 체크, Meta Quest Support·Touch 프로필 체크 → 생기는 `Packages/manifest.json`, `Assets/XR/**`, `ProjectSettings/*` 변경을 커밋 |

### P2·P3

| ID | 상태 | 수정 내용 | 파일 |
|---|---|---|---|
| 10 | FIXED (정적 검증) | 다음 목표 = 도달한 것 중 가장 앞 체크포인트 다음. 건너뛰어도 되돌아가라고 안 함 | `CheckpointManager.NextTarget`, `HighestReachedIndex` |
| 11 | NEEDS_DEVICE_TEST | 질주 0.55 → 0.6, 체감 상한 5 → 6.5. 걷기 0.2 / 달리기 0.36 / 질주 0.6 (월드 m/s) | `RatScaleConfig`, `RatComfortSettings`, 빌더 |
| 12 | NEEDS_DEVICE_TEST | 리스폰 중 캘리브레이션 요청 → 페이드 없이 즉시. 캘리브레이션 중 리스폰 → 끝날 때까지 대기(최대 1.5초) | `RatEyeCalibrator`, `RatRespawn` |
| 13 | NEEDS_DEVICE_TEST | 포커스 상실·일시 정지 시 메뉴 열기(시간 정지, 소리 정지). 에디터에서는 동작 안 함 | `PetitParisGameFlow` |
| 14 | NEEDS_DEVICE_TEST | 사람 눈(1.55m) → 쥐 몸 시선 검사. 벽·의자·식탁에 가리면 발견 못 함 | `PedestrianAwareness.HasLineOfSight` |
| 15 | PARTIAL | 진동: 위험도(톡톡), 실패, 들킴, 낙하 복귀, 줍기. 앞발 상호작용은 미구현 | `RatHaptics`(새) |
| 16 | NEEDS_DEVICE_TEST | 말풍선 2.15m → 0.55m 높이, 사람 앞 0.3m, 크기 ×0.6 (0.7m 거리 올려다보는 각도 약 71° → 약 31°) | `PetitParisGameplayKit.SpeechBubble` |
| 17 | PARTIAL | 리스폰 무적 1.5 → 2.5초, 무적 중 사람이 알아채지 않음. 체크포인트 방향은 기존 유지(경로 방향) | `RatRespawn`, `PedestrianAwareness` |
| 18 | NEEDS_DEVICE_TEST | X 메뉴에 규칙·별점 기준, 시작 힌트에 "60% → ★, 황금 3개 → ★" | `PetitParisText`, `PetitParisGameFlow` |
| 19 | NEEDS_DEVICE_TEST | 주방 체크포인트 2개: 널빤지 시작 앞 바닥, 두 번째 상자 위 (체크포인트 5 → 7개) | 빌더 |
| 21 | PARTIAL | `Mobile_RPAsset`(Android): HDR 끔, MSAA 1 → 4x, Render Scale 0.8 → 1.0, 그림자 거리 50 → 10m를 저장소에 적용. SRP Batcher 켜짐, 정적 배칭 표시는 기존 유지. **72fps는 측정 안 함** | `Assets/Settings/Mobile_RPAsset.asset`, `PetitParisProjectSetup` |
| 22 | FIXED | README 목표·조작(Quest 2 Touch, PC) 갱신 | `README.md` |
| 23 | NEEDS_DEVICE_TEST | 안전 구역 이름·위험 원인 이름을 `PetitParisText`에 const로 모아 폰트 미리 채우기에 포함 | `PetitParisText.ZoneNames`, `Hazard…` |
| 24 | PENDING | 경로 약 34m: 걷기만 약 170초, 달리기 약 95초 + 숨기·대기. 실제 플레이 시간 측정 후 경로를 줄일지 결정 | - |
| 25 | PARTIAL | 웅크리기 효과(발견 거리 ×0.45)를 힌트·조작법에 명확히 안내. 웅크려야만 지나가는 길은 없음 | `PetitParisText` |

## 5. 미해결 이슈와 계획

| 항목 | 이유 | 계획 |
|---|---|---|
| ISSUE-20 OpenXR 설치·설정 | 에디터에서만 가능 (패키지 서버 접근, 설정 에셋 생성) | 6절 순서로 설치 후 생긴 파일 커밋, Validator 재실행 |
| ISSUE-21 72fps | Quest 2 미연결 | OVR Metrics Tool로 측정, GPU 병목이면 Render Scale 0.9·그림자 끄기, CPU면 보행자 수·Update 줄이기 |
| ISSUE-24 플레이 시간 | 측정 전 | 3명 이상 플레이 시간 기록, 5분 넘으면 골목 구간 단축 |
| ISSUE-15 앞발 상호작용 | 새 기능 설계 필요 | 부스러기를 앞발로 쥐기 (Interact = 왼손 트리거, 이미 매핑 있음) |
| ISSUE-25 웅크리기 | 레벨 설계 변경 필요 | 웅크려야 지나가는 틈(높이 체크) 추가 검토 |
| IPD(눈 사이 거리) | 추적 공간 0.1배라 입체감도 쥐 크기로 줄어든다(의도된 효과). 사람마다 체감 차이 | Quest 2에서 크기감·두통 여부 확인 |
| 배경 품질 | 블록아웃 (무료 에셋 교체 예정) | `docs/NEXT_STEPS.md` 4-4 |

### 이번 작업 중 추가로 발견한 것
1. **Quest 2는 Touch Plus 프로필로 동작하지 않는다**: 이전 설정은 Quest 3용 Touch Plus만 켰다. Quest 2는 *Oculus Touch Controller Profile*이 필요 (Validator 점검 추가).
2. **OpenXR·XR Management 패키지가 manifest에 없다** (Unity 6.6 업그레이드 때 빠졌거나 커밋 안 됨).
3. PC에서 진행 중인 조명 작업이 URP 후처리(Volume)를 쓴다. **Quest 2에서는 후처리 비용이 커서 끄거나 최소화** 권장.
4. 시작 안내가 이동을 잠그므로 MCP 자동 테스트는 `PetitParisGameFlow.CloseIntro()`를 먼저 호출해야 한다.

## 6. Quest 2 빌드 순서와 테스트 계획

### 6-1. 한 번만 하는 준비 (Windows 노트북)
1. **Quest 2 개발자 모드**: 휴대폰 Meta Horizon 앱 → 기기 → 헤드셋 설정 → 개발자 모드 켬 (Meta 개발자 계정·조직 필요). 헤드셋 재부팅.
2. **Unity Android 모듈**: Unity Hub → 설치 → 6000.6.9f1 톱니바퀴 → 모듈 추가 → **Android Build Support** (OpenJDK, Android SDK & NDK Tools 포함).
3. **OpenXR 설치** (Unity에서 PetitParis 프로젝트 열고):
   1. **Edit > Project Settings > XR Plug-in Management** → (처음이면) **Install XR Plugin Management**
   2. **Android 탭(로봇 아이콘)** → **OpenXR** 체크 (OpenXR 패키지가 설치됨) → 아래 **Meta Quest Support** 체크
   3. 왼쪽 **XR Plug-in Management > OpenXR** → Android 탭 → **Enabled Interaction Profiles** **+** → **Oculus Touch Controller Profile** (Quest 3도 쓸 거면 Meta Quest Touch Plus Controller Profile도)
   4. **Meta Quest Support** 옆 톱니바퀴 → Target Devices에서 **Quest 2** 체크
   5. 왼쪽 **XR Plug-in Management > Project Validation** → Android 탭 → **Fix All**
   6. PC 에디터 테스트용으로 **Windows 탭**에서는 OpenXR을 꺼 두어도 된다 (헤드셋 없으면 PC 마우스 모드)
4. **File > Build Profiles** → **Android** 선택 → **Switch Platform**
5. 메뉴 **PetitParis > 0. Apply Quest Android Player Settings** → 적용 (ARM64, IL2CPP, Vulkan, Min API 32, Linear)
6. 메뉴 **PetitParis > 3. Build Game Scene (PetitParisVR)** → 메뉴 **PetitParis > 4. Validate Scene** → Console에서 FAIL 0개 확인
7. 생긴 `Packages/manifest.json`, `Assets/XR/`, `ProjectSettings/` 변경을 커밋

### 6-2. Quest 2 연결
1. Quest 2를 충전 후 켜고 USB-C 케이블로 노트북에 연결
2. 헤드셋 안에 "USB 디버깅 허용" 창 → **이 컴퓨터에서 항상 허용** 체크 → 허용
3. (안 뜨면 케이블을 바꾸거나 헤드셋 설정 > 시스템 > 개발자에서 USB 연결 대화상자 켜기)

### 6-3. Build And Run
1. **File > Build Profiles** → 왼쪽 **Android** (활성 표시 확인)
2. 오른쪽 **Run Device** 드롭다운 → **Refresh** → **Oculus Quest 2** (또는 기기 일련번호) 선택
3. Scene List에 `Assets/PetitParis/Scenes/PetitParisVR.unity`가 맨 위·체크 상태인지 확인 (메뉴 3번이 자동으로 넣음)
4. 오른쪽 아래 **Build And Run** → 저장 위치 `Builds/PetitParis.apk` (`Builds/` 폴더는 커밋하지 않음)
5. 빌드가 끝나면 헤드셋에서 자동 실행. 다시 실행: 헤드셋 앱 목록 → 필터 **알 수 없는 출처** → PETIT PARIS
6. 로그 보기: Unity **Window > Analysis > Android Logcat** (패키지 필요) 또는 `adb logcat -s Unity`

### 6-4. Quest 2 테스트 체크리스트 (모두 미검증)

| 영역 | 확인할 것 | 통과 기준 |
|---|---|---|
| HMD 화면 | 앱이 몰입 모드로 뜨고 두 눈에 정상 출력, 검은 화면·한쪽 눈만 출력 없음 | 시작 안내 패널이 보임 |
| 컨트롤러 | 왼쪽 스틱 이동, 오른쪽 스틱 30° 회전, 그립 달리기, 스틱+트리거 질주, A 점프·안내 닫기, B 웅크리기, X 메뉴, ≡ 길게 시점 맞추기 | 모두 반응, 겹침 없음 |
| 12cm 시점 | 앉은 자세·선 자세에서 눈높이가 바닥 가까이, 머리 움직임 자연스러움, Oculus 버튼 재중심 후 눈높이 재보정 | 바닥에 묻히거나 떠 있지 않음 |
| NPC 추격 | 들킴 → 숨을 곳 메시지·초록 화살표 → 숨기 성공 시 추격 종료, 벽 너머에서는 안 들킴 | 처음 하는 사람도 숨을 수 있음 |
| 빗자루 이벤트 | 추격 중 죽은 뒤 다시 들어가면 이벤트가 다시 시작 | 2회 이상 반복 |
| 체크포인트 | 7개 모두 활성화, 리스폰 방향·무적 2.5초, 주방에서 죽으면 주방 체크포인트 | 시작 지점으로 안 돌아감 |
| 낙하 | 주방 널빤지에서 떨어지면 페이드 후 복귀, 공중 비네트 | 멀미 없음 |
| UI 가독성 | HUD(시선 위), 실패 메시지(원인별), X 메뉴, 완료 패널, 한글 글자 누락 없음 | 1m 거리에서 읽힘 |
| 멀미 | 질주·달리기·낙하 비네트, 편안함 모드 전환 | 5분 플레이 후 불편 없음 (3명 이상) |
| 진동 | 위험 접근 톡톡, 실패, 들킴, 줍기 | 느껴짐, 과하지 않음 |
| 프레임 | OVR Metrics Tool: 72fps 유지, GPU/CPU 사용률, 드로우콜 | 카페·빵집·주방에서 72fps |

### 6-5. 성능 측정 계획 (Quest 2, 72fps 목표 — 아직 측정 안 함)
1. OVR Metrics Tool(Meta Quest 스토어)로 FPS·GPU·CPU 레벨 오버레이
2. Unity Development Build + Autoconnect Profiler로 구간별(카페·빵집·골목·주방) CPU 프레임 시간, GC Alloc
3. GPU 병목이면: Render Scale 0.9 → 그림자 끄기 → 투명 오버레이(비네트·HUD) 면적 줄이기
4. CPU 병목이면: 보행자 Update·Physics 쿼리 확인, 정적 배칭·SRP Batcher 확인 (Frame Debugger)
5. Dynamic Font: 첫 표시 때 글리프 생성으로 끊김이 있으면 메뉴 3번으로 폰트 미리 채우기 재실행

## 7. 5분 발표용 요약

1. **발견** — 5명의 가상 사용자 관점으로 AI가 코드를 정적 분석해 25개 이슈를 찾았다. 가장 큰 문제(P1 10개): 들키면 도망칠 수 없는 추격 속도, 주방 낙하의 멀미 위험, 실패 원인을 알려 주지 않는 메시지, 2초 만에 사라지는 조작 안내, Quest 빌드 설정 누락.
2. **원인** — 숫자의 불균형(추격 0.6 > 쥐 상한 0.5), 이벤트 호출 순서에 의존한 상태 관리, 안내 문구와 실제 규칙의 불일치, 그리고 Quest 3 기준 설정(Touch Plus 프로필)을 Quest 2에 그대로 쓰려던 점.
3. **수정** — 추격 0.4 + 숨을 곳 안내(초록 화살표), 낙하 완충(페이드 후 복귀)과 넓은 널빤지, 편안함 모드(X 메뉴), 원인별 실패 메시지와 우선순위 큐, A로 닫는 조작 안내, 빗자루 이벤트 상태 기계, Quest 2 빌드 점검(Validator)과 모바일 URP 설정.
4. **검증** — 이번에는 정적 검증까지: 수정한 런타임 코드와 레벨 빌더를 Unity 참조 라이브러리로 컴파일했고, 이벤트 순서 문제는 두 순서를 코드로 추적했다. Unity Play와 Quest 2 실기기 확인은 **아직 하지 않았다**.
5. **향후 계획** — Quest 2에서 6-4 체크리스트 실행(멀미·가독성·72fps), 플레이 시간 측정, 무료 파리 에셋으로 배경 교체.
6. **실기기 테스트로 추가 발견 (8절)** — 정적 리뷰로는 못 찾은 4가지: 질주가 한 프레임 조건 때문에 자주 무시됨, 자전거 길 바닥판이 겹쳐 Quest 2에서 깜빡임(z-fighting), 빗자루 구간 숨을 곳 부족, 배경음이 없고 3D 소리가 너무 작음. "실기기 테스트가 왜 필요한가"를 보여 주는 사례.

## 8. Quest 2 실기기 테스트 기반 추가 발견 이슈 (2026-11-16)

사용자가 Quest 2에 APK를 설치해 직접 플레이한 영상(약 15초)과 설명으로 찾은 이슈. AI 정적 리뷰로는 나오지 않았던 문제들이다.
수정은 모두 **코드만 바꿨고 Quest 2에서 다시 확인하지 않았다** (NEEDS_DEVICE_TEST).

### Q2-01 질주가 잘 발동되지 않고 조작이 직관적이지 않음 — NEEDS_DEVICE_TEST
- **문제 상황**: 오른쪽 트리거를 눌러도 질주가 안 될 때가 많고, 어떤 버튼이 달리기·질주인지 헷갈림.
- **원인 (코드 확인)**: 질주는 오른쪽 트리거를 누른 **그 한 프레임**에 아래 조건이 모두 맞아야 발동했고, 실패해도 아무 반응이 없었다.
  1. 왼쪽 스틱을 30% 이상 밀고 있어야 함 (`minStickInput 0.3`) — 서서 트리거만 누르면 무시
  2. 그 프레임에 땅에 닿아 있어야 함 — CharacterController의 `isGrounded`는 경사·턱·널빤지에서 자주 깜빡인다
  3. 쿨다운 1.2초가 끝나 있어야 함 — 조금 일찍 누르면 무시
  4. 지속 0.45초(체감 약 2.5m)라 짧아서 발동돼도 알아채기 어려움
  - 달리기는 왼손 그립을 계속 쥐고 있어야만 해서, 스틱 이동과 동시에 하기 불편했다.
- **수정 내용**
  - 질주: **오른쪽 트리거 또는 오른쪽 그립**. 스틱을 안 밀었으면 **바라보는 방향**으로 질주
  - 누른 뒤 **0.25초 동안 입력을 기억** → 쿨다운이 끝나거나 땅에 닿는 순간 발동
  - 땅에서 떨어진 뒤 0.15초까지는 땅으로 본다
  - 지속 0.45 → **0.6초**, 쿨다운 1.2 → **0.8초**, 스틱 임계값 0.3 → 0.15
  - 발동하면 **오른손 진동** (그동안 비네트도 켜짐)
  - 달리기: **왼쪽 스틱 꾹 누르기 = 켜기/끄기** (스틱을 놓고 0.4초 지나면 자동으로 꺼짐, 켜면 왼손 진동) + 기존 왼손 그립 누르고 있기 유지
  - PC: Caps Lock = 달리기 켜기/끄기 추가 (Shift 누르고 있기, 클릭 질주는 그대로)
  - 조작 안내·메뉴·README·빵집 힌트 문구를 실제 조작에 맞게 수정
- **Quest 2에서 누를 버튼**

| 하고 싶은 것 | 버튼 |
|---|---|
| 걷기 | 왼쪽 스틱 |
| 달리기 | 왼쪽 스틱을 꾹 눌러 켜기 (다시 누르거나 멈추면 꺼짐) 또는 왼손 그립 누르고 있기 |
| 질주 (짧고 아주 빠르게) | 오른쪽 트리거 또는 오른손 그립 한 번 (스틱 방향, 안 밀면 보는 방향) |
| 점프 / 웅크리기 | A / B |
| 메뉴 (조작법·편안함 모드) | X |

- **수정 파일**: `RatDashController.cs`, `RatLocomotionController.cs`, `RatInputReader.cs`, `PetitParisInput.inputactions`(Rat/RunToggle 추가, Dash에 오른손 그립 추가), `RatHaptics.cs`, `PetitParisText.cs`, `README.md`
- **입력 충돌 확인**: 왼쪽 스틱 누르기·오른손 그립은 기존 매핑에 없었다. 왼손 그립(달리기), A·B·X·Y·≡는 그대로
- **검증 필요**: 질주가 매번 발동하는지, 왼쪽 스틱 누르기가 이동 중 실수로 눌리지 않는지, 진동 세기

### Q2-02 자전거 길 바닥이 검게 흐릿하게 보임 — NEEDS_DEVICE_TEST
- **문제 상황**: 자전거가 가로지르는 길(z 16.4~17.6)이 Quest 2에서 검은 띠처럼 보이고, 골목과 닿는 경계가 톱니 모양으로 깜빡임 (영상에서 확인).
- **원인**
  1. **깊이 싸움(z-fighting)**: 자전거 길 판(`Paving_BikeLane`, 높이 0.003m)과 골목 포석 판(`Paving_Alley`, z 13.3~23, 높이 0.003m)이 **같은 높이로 겹쳐** 있었다. 플레이어가 건너는 바로 그 자리다. 카메라 near clip이 0.005m로 아주 작아(쥐 시점) 깊이 정밀도가 낮은 Quest 모바일 GPU에서 두 면이 번갈아 그려졌다
  2. **너무 어두운 질감**: 아스팔트 질감 기본색이 0.2(거의 검정)라 해 질 녘 조명에서 검게 보였다
  3. 바닥판이 땅(y 0)보다 3mm만 높아 먼 곳에서도 깊이 싸움 가능성
  - 셰이더는 URP Simple Lit(모바일용), 텍스처 압축은 Android 기본 ASTC, 이방성 필터는 품질 설정 "질감별"(4x). 저장소의 게임 씬에는 후처리 Volume이 없다. 단, PC에서 진행 중인 조명 작업(후처리)이 그 빌드에 들어갔다면 함께 확인해야 한다
- **수정 내용**
  - 골목 포석을 자전거 길 앞뒤 두 판(`Paving_Alley_South`, `Paving_Alley_North`)으로 나눠 **겹침 제거**
  - 자전거 길을 밝은 회색 아스팔트 질감(`PetitParisTextures.BikeLane`, 기본색 0.5, 대비 낮춤) + Simple Lit 하나로. 높이 7mm로 다른 바닥판보다 조금 높게
  - 흰 점선 가장자리 선 추가 (건널 길이 잘 보이게)
  - 모든 바닥판 두께 3 → 5mm, 주방 바닥도 5mm (먼 곳 깊이 싸움 완화)
- **수정 파일**: `PetitParisLevelBuilder.cs`(BuildGround, BikeLane, Paving), `PetitParisTextures.cs`(BikeLane), `PetitParisValidator.cs`(점검 추가)
- **검증 필요**: Quest 2에서 경계 깜빡임이 없어졌는지, 밝기가 주변 포석과 어울리는지. 남으면 near clip 0.005 → 0.008 검토 (가까운 물체가 잘릴 수 있어 이번엔 안 바꿈)

### Q2-03 빗자루 회피 구간 엄폐물 부족 — NEEDS_DEVICE_TEST
- **문제 상황**: 빗자루 웨이터가 쫓아올 때 숨을 곳이 팔레트와 종이 상자뿐이라 피하기 어려움.
- **원인**: 추격 구간(z 18.6~23, 폭 2.4m)에 안전 구역이 서쪽 팔레트(z 19.25)와 동쪽 끝 종이 상자(z 22.5) 두 곳. 그 사이 3m에 숨을 곳이 없었다. 상자 더미·쓰레기통은 숨는 곳이 아니라 장애물이었다.
- **수정 내용**: 양쪽 벽을 번갈아 숨을 곳 2곳 추가 → 구간 안 숨을 곳 4곳(안전 구역 5개)
  - 동쪽 **와인 상자**(옆으로 눕힘, z 19.05): 추격 시작 직후 오른쪽으로 질주해 들어갈 수 있는 첫 피난처
  - 서쪽 **빵 바구니**(뒤집힌 바구니, z 21.05): 팔레트와 종이 상자 사이 빈 구간
  - 입구는 모두 골목 가운데(안내 경로) 쪽. 숨는 곳 사이 1~1.5m는 열려 있어 웨이터 동선을 보고 옮겨 가야 한다 (너무 쉽지 않게)
  - 추격 시작 지점에 "빗자루가 오면 상자 속에 쏙!" 힌트. 들키면 HUD·초록 화살표가 가장 가까운 곳을 안내 (ISSUE-01)
  - 기존 종이 상자는 같은 모양을 만드는 공통 함수(`HideBox`)로 정리
- **수정 파일**: `PetitParisLevelBuilder.cs`(BuildProps, HideBox), `PetitParisText.cs`(HintBroomHide, ZoneNames), `PetitParisValidator.cs`
- **검증 필요**: 쥐가 입구로 잘 들어가는지(안쪽 높이 22~24cm, 쥐 13cm), 너무 쉬워지지 않았는지

### Q2-04 플레이 중 소리가 거의 안 남 — NEEDS_DEVICE_TEST
- **문제 상황**: Quest 2에서 플레이하는 동안 소리가 안 들림. 사용자가 보낸 녹화 영상의 소리도 평균 −57dB, 최대 −30dB로 거의 무음이었다 (ffmpeg volumedetect로 측정).
- **원인 (코드 확인)**
  1. 배경음·음악이 **하나도 없었다**. 소리는 발소리·자전거·빗자루·효과음뿐이라, 사람이 멀리 있으면 완전히 조용했다
  2. 3D 소리가 거리에 따라 너무 빨리 작아졌다: 소리가 줄기 시작하는 거리(minDistance)가 월드 0.3~0.4m라, 2~4m 떨어진 발소리·빗자루는 10~20분의 1 크기
  3. 웨이터 A가 25초 뒤에 출발하도록 바꿔(ISSUE-07) 시작 구간이 더 조용해졌다
  - 프로젝트 오디오 설정(`AudioManager`: Disable Audio 꺼짐, Spatializer 없음)과 AudioListener 1개는 정상이었다
- **수정 내용**
  - 새 컴포넌트 `RatAmbience`: 거리 배경음(먼 차·웅성임·카페 컵 소리, 8초 루프)과 아코디언 왈츠 배경 음악(3/4박자 8마디 루프, 이 프로젝트용으로 새로 작곡해 코드로 합성 → 라이선스 문제 없음)
  - 쫓길 때는 음악을 줄여 추격 음악·발소리가 잘 들리게, 완료 화면에서는 키움
  - 3D 소리의 minDistance를 3배로 (`PetitParisAudio.Loudness3D`), maxDistance도 1.5배
  - 질주가 발동되면 바람 소리 (Q2-01 피드백)
  - 다시 시작할 때 `AudioListener.pause`·`Time.timeScale`을 초기화 (일시 정지 메뉴가 남긴 무음 방지)
- **수정 파일**: `PetitParisAudio.cs`(CityAmbience, MusetteLoop, Loudness3D), `RatAmbience.cs`(새), `PetitParisGameFlow.cs`, `PetitParisLevelBuilder.cs`, `PetitParisValidator.cs`
- **검증 필요**: Quest 2 스피커로 음악·배경음 크기(너무 크지 않은지), 발소리 방향감, 헤드셋 볼륨이 0이 아닌지. 그래도 무음이면 Quest 시스템 메뉴로 나갔다 온 뒤 X 메뉴가 열린 채로 일시 정지돼 있는지 확인 (일시 정지 중에는 소리도 멈춘다)
- **2026-11-18 확인**: PC 에디터 Play에서 거리 배경음(`pp_city_ambience`)·아코디언(`pp_musette`)·발소리·자전거·빗자루 소리가 재생 중(AudioSource isPlaying, AudioListener 일시 정지 아님). **Quest 2 스피커로 들리는지는 아직 사용자 확인 전 (미검증)** — 첫 실기기 실행은 화면 흔들림(Q2-05) 때문에 소리 확인까지 가지 못함

### Q2-05 머리를 움직이면 세상이 따라 흔들림 — FIXED (코드) / NEEDS_DEVICE_TEST
- **문제 상황 (2026-11-18 Quest 2 실기기)**: 게임 실행 중 머리를 움직이면 화면이 심하게 흔들림
- **원인 (logcat VrApi 측정)**: 게임 중 45~50fps, 앱 GPU 시간 16~18ms (72Hz 예산 13.9ms), GPU 사용률 0.85~0.98, CPU 0.2~0.3 → GPU 병목. 프레임을 못 맞추면 Quest가 이전 프레임을 재투영해서 머리를 돌릴 때 떨려 보인다. 카메라 코드(TrackedPoseDriver Update+BeforeRender, 바인딩) 문제는 아님
- **수정 내용**
  - `RatQuestPerformance`(새, Android 실기기만): 카메라 URP 후처리 끔 (중간 렌더 텍스처 + 전체 화면 패스 제거), 고정 포비티드 렌더링 1.0 (`XRDisplaySubsystem.foveatedRenderingLevel`)
  - OpenXR Foveated Rendering 기능(Android) 켬, Validate 항목 추가
  - PC·에디터는 그대로 (후처리 색감 유지). Quest에서는 색 보정(Tonemapping·Color Adjustments)이 빠져 색감이 약간 달라질 수 있음
- **검증 필요**: Quest 2에서 logcat `VrApi FPS=72/72` 유지, 머리 돌릴 때 떨림 없음. 그래도 GPU가 모자라면 다음 순서: 그림자 끄기 → MSAA 4x→2x → Render Scale 0.9

### 이번에 다시 확인할 순서 (PC 에디터 → Quest 2)
1. PC Claude: git pull → 메뉴 3번(씬 재생성) → 4번(Validate: Q2-02/03 점검 통과 확인)
2. Build And Run (6-3)
3. Quest 2: 자전거 길 경계 깜빡임, 질주(서서 트리거 / 이동하며 트리거 / 그립), 왼쪽 스틱 누르기 달리기, 빗자루 구간 숨기, 배경음·음악·발소리
