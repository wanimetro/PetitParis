# 01. 프로젝트 분석 — PETIT PARIS

- 기준: `main` 커밋 `2c927ff` (2026-11-15)
- 방법: **정적 코드 분석만 했다.** 이 분석 환경에서는 Unity 에디터와 Quest 3을 실행할 수 없었다. 실행 결과·프레임·체감에 관한 내용은 모두 **추정**이다.
- 범위(Standard): 런타임 플레이어·입력·게임플레이·UI 스크립트는 전부 읽었다. 에디터 빌더는 레벨 배치 부분만 샘플링했다 (아래 "분석 범위" 참고).

---

## 1. 한 줄 요약

작은 쥐(눈높이 12cm)가 되어 몽마르트르풍 골목 → 센강 → 비스트로 주방 식탁 위 치즈까지 가는 3~5분짜리 Meta Quest 3 VR 게임이다. 사람 발걸음·자전거·바게트를 피하고, 빵 부스러기를 모아 별점을 받는다. 현재는 **블록아웃 프로토타입**이다. 게임 시스템은 대부분 구현되어 있고, PC 에디터에서 부분 검증을 했으며, 실기기 검증은 아직이다.

## 2. 기술 구성

| 항목 | 내용 |
|---|---|
| 엔진 | Unity 6000.6.9f1, URP 17.6 |
| XR | OpenXR + XR Interaction Toolkit 3.6.1, Input System 1.20.1, XR Core Utils 2.6.0 |
| 기기·입력 | Quest 3 + Touch Plus 컨트롤러. 앉아서 플레이 가능. Hand Tracking 없음, 네트워크 없음 |
| 코드 규모 | 직접 작성한 C# 약 9,100줄: 런타임 36개(약 5,600줄), 에디터 7개(약 3,500줄) |
| 씬 | **레포에 게임 씬 파일이 없다.** 에디터 메뉴 `PetitParis > 3. Build Game Scene`이 코드로 씬을 생성한다 (`PetitParisLevelBuilder`, `PetitParisBuildKit`, `PetitParisParisKit`) |
| 외부 에셋 | 없음. 텍스처는 코드로 만들고(`PetitParisTextures`), 효과음도 코드로 만든다(`PetitParisAudio`). 폰트는 나눔고딕(OFL) |
| 기타 패키지 | `com.coplaydev.unity-mcp`(git URL, 버전 고정 없음), Visual Scripting·Collab Proxy·Multiplayer Center(기본 템플릿), Test Framework(테스트 코드 없음) |

### 핵심 설계 (잘 된 부분)
- **스케일 방식 A**: 월드는 실제 미터, `Rat Tracking Space`만 0.1배로 줄인다. 머리 회전은 1:1이고 머리 이동·IPD는 1/10이다 → 세상이 10배 크게 보인다. 속도·충돌은 모두 월드 미터 하나로 다룬다.
- **이동 경로가 하나**: `RatLocomotionController` → LocomotionMediator → XRBodyTransformer → CharacterController. 질주·점프·웅크리기 컨트롤러는 상태만 계산하고 직접 움직이지 않는다. 중복 Provider가 있으면 Validator가 FAIL을 낸다.
- **카메라를 건드리지 않는다**: Roll·Pitch·Head Bob이 없고, 시점이 순간 이동할 때(캘리브레이션, 리스폰)는 항상 페이드를 쓴다.
- **편안함 설정을 에셋 하나로 모았다**: `RatComfortSettings`에 스냅 회전 30°, 질주·점프 켜고 끄기, 비네트, 체감 속도 상한이 있다.
- **문서화와 정직한 상태 표기**: 미검증 항목을 문서마다 명시한다.

## 3. 사용자 여정 (빌더 좌표로 재구성)

| 구간 | 월드 z (m) | 내용 | 위험 | 안전 구역·체크포인트 |
|---|---|---|---|---|
| 시작 | -0.4 | 카페 벽 틈 바로 바깥에서 동쪽을 보고 시작. 조작 안내 패널은 0.53m 앞 | 웨이터 A의 루프 경로(z 0.2 라인)가 시작점에서 **약 0.7m** 거리 | `SafeZone_WallCrack`, CP0 |
| 1. 카페 테라스 | 0 ~ 7 | 테이블 3세트, 의자 아래 숨기. 황금 크루아상은 웨이터 경로 한가운데(-2.45, 5.2) | 웨이터 A: 0.9 m/s 루프, 발견 반경 1.0m, 추격 0.55 m/s | 의자 아래, CP1(-1.5, 6.08) |
| 2. 빵집 앞 | 7 ~ 13 | 셰프 B 지그재그, 차양에서 바게트가 굴러떨어짐 | 셰프 B: 0.8 m/s, 추격 0.55. 바게트: 경고 그림자, 코드 기본값 예고 1.5초·간격 3.5초, 방문당 3개 (빌더가 넘기는 2.6은 낙하 높이. NEXT_STEPS의 "예고 1.1초·간격 2.6초"와 다름) | 벤치 아래, 차양 밑, CP2(-2.7, 10.0) |
| 3. 골목 | 13 ~ 23 | z 17 가로 골목에 자전거 왕복. z 18.6에서 **빗자루 웨이터 추격 이벤트(9초)** | 자전거 2.2 m/s(체감 22 m/s), 끝에서 2.5초 정지. 빗자루 웨이터 추격 0.6 m/s | 벤치 아래 CP3(-1.35, 15.0), 팔레트 밑, 종이 상자 안 |
| 4. 센강변 | 23 ~ 25 | 비둘기 떼(위험 없음), "치즈 냄새" 이야기 메시지 | 없음 | CP4(-1.9, 23.9) 주방 뒷문 |
| 5. 비스트로 주방 | 17.6 ~ 23 (x -7.6 ~ -1.7) | 요리사 통로(z 22.2)를 지나 널빤지(약 21°) → 상자 0.3/0.6/0.9m → 도마 → 식탁(높이 0.9m) 위 치즈 | 주방 요리사: 발견 1.1m, 추격 0.6. **식탁 높이 0.9m = 체감 9m** | 조리대 밑, 식탁 밑 |
| 완료 | — | 완료 패널(별점, 점수, 시간, 다시 시작 횟수) → A로 씬 재시작 | — | — |

### 안내 수단
- 시작 조작 안내 패널: 0.35m(체감 3.5m) 움직이거나 30초가 지나면 사라진다.
- 장소별 힌트 표지: 0.6m 안에서 완전히 보이고 1.2m에서 사라진다. 10개.
- 다음 목표 위 금색 마름모, 바닥 금색 화살표(경로 26개 노드), 시야 아래 HUD(점수와 메시지), 왼손 손목 상태 패널(3~4줄)

### 피드백 수단
- **소리**: 3D 발소리, 자전거 벨과 바퀴 소리, 발견 경고음, 추격 음악, 체크포인트 차임, 실패음, 줍기 소리
- **시각**: 발 그림자(높이에 따라 크기와 농도가 변함), 착지 예고 원, 바게트 그림자, 안전 구역 바닥 초록 표시, "!" 말풍선
- **진동(Haptic)**: **없음.** 코드에서 `SendHapticImpulse` 같은 호출을 찾지 못했다.

## 4. 기능 구조 요약

| 시스템 | 파일 | 핵심 동작 |
|---|---|---|
| 이동 | `RatLocomotionController` | 걷기 0.2 / 달리기 ×1.8 = 0.36 / 질주 0.55 (월드 m/s). 체감 속도 상한 5 m/s → 월드 0.5로 **질주가 0.5에서 잘림**. 가속 1.2, 감속 1.8 m/s². 체감 중력 0.98 m/s², 최대 낙하 1 m/s(체감 10 m/s) |
| 질주 | `RatDashController` | 0.45초, 쿨다운 1.2초, 스틱 0.3 이상 필요, 지상에서만 가능. 정면 벽이면 즉시 정지 |
| 점프·웅크리기 | `RatJumpController`, `RatCrouchController` | 점프 0.035m(체감 35cm), 코요테 타임 0.1초, 입력 버퍼 0.12초. 웅크리기는 Toggle이고 반지름만 70%(높이는 그대로) |
| 캘리브레이션 | `RatEyeCalibrator` | 시작 시 자동으로 눈높이 0.12m와 정면 Yaw를 맞춘다. Quest 재중심 이벤트가 오면 다시 맞춘다. 왼쪽 메뉴 길게 누르기로 수동 실행 |
| 위험 판정 | `RatDangerDetector`, `FootstepHazard` | 매 프레임 캡슐과 박스 사이 거리를 계산. 닿으면(d ≤ 0) 즉사, 단 안전 구역 안이면 무시. **발의 단계(내려오는 중인지, 디딘 상태인지)와 관계없이 박스에 닿으면 실패** |
| 리스폰 | `RatRespawn`, `CheckpointManager` | 페이드 0.25초 → 가장 가까운 도달 체크포인트로 이동 → 캘리브레이션 → 페이드 인 0.4초 → 무적 1.5초 |
| 사람의 인지·추격 | `PedestrianAwareness`, `PedestrianPath` | 발견(반경 1.0m, 시야 ±65°, 웅크리면 반경 ×0.45) → 0.9초 멈춤 → 3.5초 추격(0.55~0.6 m/s, 회전 110°/s, 발을 쥐 쪽으로 최대 0.2m 내딛음) → 1.2초 숨으면 놓침 → 쿨다운 7초 |
| 추격 이벤트 | `ChaseEvent`, `BroomSweep` | 트리거에 들어가면 9초 강제 추격, 빗자루 머리도 위험 판정. 죽으면 이벤트를 다시 준비(`armed`) |
| 수집 | `CrumbCollection`, `CrumbPickup` | 체크포인트 전에 죽으면 그동안 모은 조각을 잃는다. 별점: 도착 / 60% / 황금 3개 |
| 진행 | `PetitParisGameFlow` | Intro → Playing → Completed. 재시작은 씬을 다시 불러온다. **게임 중 일시정지·재시작 메뉴는 없다** |

## 5. 성능 구조 (잠재 위험만, 측정값 없음)

- **좋은 점**: 정적 리스트(`FootstepHazard.Active`, `CrumbPickup.Active`)로 판정하고 물리 Trigger를 쓰지 않는다. 런타임 `Instantiate`는 바게트 보너스 부스러기(`FallingHazard.SpawnBonusCrumb`, 최대 6개)에만 있다 (*처음 초안에는 "없음"으로 잘못 적었고, P3 교차 검증에서 바로잡음*). 효과음은 Start에서 미리 만든다. 손목 패널은 0.15초 간격으로 갱신하고 `StringBuilder`를 쓴다(`ToString()` 할당은 남아 있음).
- **위험 후보**
  - 실시간 조명: Directional(Hard Shadow) 1개 + 주방 Point Light 2개(그림자 없음). URP Forward에서 주방 구간의 추가 조명 비용
  - 환경 오브젝트 약 400개 이상을 기본 도형으로 구성(Static Batching에 의존). MaterialPropertyBlock을 쓰는 렌더러는 SRP Batcher에서 빠진다
  - Mobile_RPAsset의 기본값(HDR 켜짐, MSAA 꺼짐, 그림자 거리 50m)은 메뉴 5번을 실행해야 바뀐다. **선택 사항이라 빌드에 빠질 수 있다**
  - 투명 오버드로: 비네트, 블롭 그림자, 안전 구역 표시, HUD 배경, 힌트
  - 72fps 달성 여부는 **미측정**

## 6. 구조·협업 관점 메모

- 씬을 코드로 만들어서 재현성이 높다. 하지만 `PetitParisBuildKit.cs`(1,124줄)와 `PetitParisLevelBuilder.cs`(727줄)에 좌표가 매직 넘버로 흩어져 있어, 레벨 디자인을 바꾸려면 코드를 고치고 씬을 다시 생성해야 한다.
- 문서와 코드가 어긋난 곳
  - 루트 `README.md`: 목적지가 "센강 쪽 출구"이고, 조작표에 달리기(왼손 그립)가 없다 → 실제 목적지는 주방 치즈
  - `docs/README.md` 1-3절의 레벨 설명이 주방 구간 추가 이전 내용이다
  - `RatScaleConfig` 툴팁은 "걷기 0.15"인데 실제 기본값은 0.2
- `Assets/Samples/XR Interaction Toolkit/3.3.2`가 설치된 XRI 3.6.1과 버전이 다르다
- `com.coplaydev.unity-mcp`가 git URL에 버전 고정 없이 들어가 있어, 다른 PC에서 받으면 다른 버전이 설치될 수 있다
- 자동 테스트가 없다 (Test Framework만 설치됨). `PetitParisValidator`가 씬 점검 역할을 한다

## 7. 분석 범위

| 구분 | 파일 |
|---|---|
| 전부 읽음 | Player/* (XROriginGuard 포함), Input/RatInputReader, Core/RatScaleConfig·RatComfortSettings, Gameplay/RatDangerDetector·RatRespawn·CheckpointManager·RatSafeZone·FootstepHazard·PedestrianPath·PedestrianAwareness·ChaseEvent·BroomSweep·PetitParisGameFlow·CrumbCollection·CrumbPickup, UI/RatHud·RatStatusPanel, Core/PetitParisText(상수) |
| 설정값만 확인 | FallingHazard, BicycleHazard, PigeonFlock, RatGuideArrow, RatObjectiveMarker, RatHintSign, ScreenFader |
| 샘플링 | PetitParisLevelBuilder(게임플레이 배치 60~240행, 부스러기 배치, 주방 일부), PetitParisGameplayKit(Awareness), PetitParisBuildKit(조명, MakeOverlay) |
| 읽지 않음 | RatScaleRig, RatDesktopLook, RatDebugPanel, RatBlobShadow, RatStoryTrigger, PetitParisAudio 본문, ParisKit, Textures, Validator, ProjectSetup, SceneBuilder, 셰이더 |
