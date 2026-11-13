# PETIT PARIS — 개발 가이드 (이동 · 발걸음 회피 · 블록아웃 게임 · 점검)

> **상태: 코드와 씬 생성 도구 작성 완료. Unity 에디터와 Quest 3 실기기 검증은 아직 하지 않았다.**
> 클라우드 환경이라 Unity를 실행할 수 없었다. C# 문법과 스크립트 간 참조는 Roslyn 컴파일러로 확인했지만, Unity API는 제가 만든 스텁을 기준으로 확인한 것이라 실제 Unity 컴파일과는 다르다.
> 씬은 에디터 메뉴로 생성한다. 아래 순서대로 실행하고, Console에 에러가 나면 전문을 붙여 달라.

1단계 문서(쥐 시점, 캘리브레이션): `docs/STAGE1.md`

---

## 1. 무엇이 만들어졌나

### 1-1. 쥐 이동 시스템
- **실제 이동은 `RatLocomotionController`(XRI `LocomotionProvider`) 하나뿐이다.** 걷기, 질주, 점프, 중력을 합산해 프레임마다 `XROriginMovement` 1개를 큐에 넣는다.
  - 적용은 XRI가 맡는다: `LocomotionMediator` → `XRBodyTransformer` → `CharacterController.Move`. 그래서 벽과 물체를 뚫지 않는다.
- **역할 분담**
  - `RatDashController`, `RatJumpController`, `RatCrouchController`는 상태와 속도만 계산한다. 스스로 움직이지 않는다.
  - 회전은 XRI `SnapTurnProvider`가 맡는다 (오른쪽 스틱만, 30도, 뒤돌기 끔).
  - Comfort Vignette는 XRI 샘플의 `TunnelingVignette`를 쓴다. `RatComfortApplier`는 질주 중에만 "Moving" 신호를 보내는 Provider로, 이동은 큐에 넣지 않는다.
- **넣지 않은 것**: XRI의 `ContinuousMoveProvider`, `GravityProvider`, `JumpProvider`. 같은 일을 두 번 하게 되기 때문이다. 검증 메뉴가 이런 Provider가 섞이면 FAIL로 표시한다.
- **편안함**
  - 이동 방향은 카메라 Yaw만 사용한다. 카메라 Transform을 건드리지 않으므로 Roll, Pitch, Head Bob이 없다.
  - 체감 속도 상한 5 m/s.
  - 설정 에셋: `RatComfortSettings.asset`

| 입력 (Quest 3 Touch Plus) | 동작 | 담당 |
|---|---|---|
| 왼쪽 스틱 | 이동 (스틱 세기 = 속도, 데드존 0.15) | RatLocomotionController |
| 오른쪽 스틱 좌우 | 30도 스냅 회전 | XRI SnapTurnProvider |
| 오른쪽 트리거 | 순간 질주 (이동 입력 필요, 0.45초, 쿨다운 1.2초) | RatDashController |
| 왼손 그립 누르고 있기 | 달리기 (걷기 속도 × 1.8, 체감 속도 상한 적용) | RatLocomotionController |
| A | 점프 (지면에서만, 끌 수 있음) | RatJumpController |
| B | 웅크리기 전환 (몸 폭 70%, 속도 50%, 머리·카메라 그대로) | RatCrouchController |
| 왼쪽 메뉴(≡) / Y 1초 길게 | 눈높이 다시 맞추기 | RatEyeCalibrator |

### 1-2. 발걸음 회피
| 스크립트 | 역할 |
|---|---|
| `PedestrianPath` | Waypoint 반복 보행 (Loop / PingPong). 기본 도형 몸, 다리, 신발을 절차적으로 움직인다. 착지할 때 3D 발소리를 낸다. 발 그림자와 착지 예고 원을 표시한다 |
| `FootstepHazard` | 위험 판정 박스 (Trigger, 밀어내지 않음). 아무 물체에나 붙일 수 있다 (신발, 자전거) |
| `RatDangerDetector` | 매 프레임 쥐 캡슐과 위험 박스 사이의 거리를 계산한다. 이 거리로 위험도를 정하고, 닿으면 실패 처리한다. 안전 구역 안이면 실패하지 않는다 |
| `RatSafeZone` | 안전 구역 (Trigger Collider). 의자 아래, 벤치 아래, 벽 틈. **아무 오브젝트에 BoxCollider와 이 컴포넌트를 붙이면 재사용된다** |
| `RatRespawn` | 페이드 아웃 → 가까운 체크포인트로 XR Origin 순간이동 → 눈높이 재정렬 → 페이드 인 → 1.5초 무적 |
| `CheckpointManager` | 반경 안에 들어오면 활성화. 실패한 위치에서 가장 가까운 활성 체크포인트를 고른다 |
| `BicycleHazard` | 왕복하는 자전거. 출발할 때 벨, 달리는 동안 바퀴 소리(3D), 바닥 그림자 |

- **3D 공간 음향**: 모든 발소리와 자전거 소리는 `spatialBlend 1`(완전 3D), 로그 감쇠, 도플러 끔이다. 거리 단위는 월드 미터(체감 ×10).
- **Quest 3에서의 한계**: Unity 기본 패닝이라 좌우 구분은 잘 되지만 앞뒤 구분은 약하다. Meta XR Audio SDK(HRTF)를 쓰면 좋아지지만, 패키지 최소화 원칙에 따라 추가하지 않았다.

### 1-3. 블록아웃 게임 (PetitParisVR.unity, 3~5분)
```
z  -0.4  시작: 카페 벽 틈 (안전 구역, 조작 안내 패널)
z 0 ~ 7  공간1 카페 테라스: 테이블 3세트(의자 6개 아래 안전), 보행자 A가 벽쪽 ↑ / 바깥쪽 ↓ 루프
z 7 ~13  공간2 빵집 앞 보도: 보행자 B 지그재그 왕복, 벤치 아래 안전, 상자·종이컵·화분·간판
z 13~23  공간3 좁은 골목: z 17 가로 골목을 자전거가 왕복 (양 끝에서 2.5초 정지)
z 23.8   목적지: 골목 출구, 난간 틈으로 센강 → 완료 화면 → A 버튼으로 처음부터 다시
체크포인트: 시작 → 테라스 끝 의자 아래 → 빵집 벤치 아래 → 자전거 길 앞 벤치 아래
```
- **안내**: 시작 지점의 조작 안내 패널(조금 움직이면 사라짐), 장소에 놓인 짧은 안내 표지(가까이 가면 나타남), 다음 목표 위의 작은 금색 마름모
- **상태 표시**: 왼손 손목 3~4줄 (체크포인트 n/4, 시간, 안전·위험, 질주 상태, 짧은 메시지)
- **그래픽**: 기본 도형과 단색 머티리얼, 실시간 조명은 Directional 1개, NPC 그림자는 Blob 셰이더, 환경은 Static Batching, 포스트 프로세싱 없음

---

### 1-4. 파리 외형·안내 업데이트
- **해 질 녘 파리 (외부 에셋 없음)**
  - 코드로 만든 질감: 포석, 석회암, 회벽, 아연 지붕, 나무, 라탄, 대리석, 줄무늬 차양, 창문 불빛, 강물 불빛
  - 오스만 양식 4~5층 건물: 창틀, 코니스, 철제 발코니, 덧창, 제라늄 꽃상자, 망사르드 지붕과 지붕창, 테라코타 굴뚝
  - 가게 정면: 카페, 빵집, 꽃집, 서점. 빛나는 쇼윈도, 간판, 줄무늬 차양
  - 소품: 테라스 줄 전구, 파리식 가로등, 모리스 기둥, 파란 거리 표지판
  - 원경: 노을 하늘(Skybox), 라벤더빛 안개, 센강, 강 건너 불 켜진 지붕선과 돔, 금빛 에펠탑
- **사진 배경(선택)**: `Assets/PetitParis/ThirdParty/Backdrops/`에 이름이 `ParisBackdrop`인 이미지를 넣고 메뉴 3을 다시 실행하면 강 건너 맨 뒤에 세운다. 라이선스가 확인된 사진만 넣고 CREDITS에 기록한다.
- **내비게이션 화살표** (`RatGuideArrow`): 쥐 발 앞 바닥에 금색 화살표가 떠서 정해진 안내 경로를 따라 다음 방향을 가리킨다.
- **보행자 의상**: 테라스는 카페 웨이터(조끼, 나비넥타이, 긴 앞치마), 빵집 앞은 셰프(셰프 모자, 체크 바지, 앞치마, 콧수염). 오리지널 디자인이다.
- **쥐 앞발**: 컨트롤러 위치에 분홍 앞발(손가락 4개, 발톱, 털 손목)이 보인다.
- **PC 모드**
  - 마우스 **오른쪽 버튼을 누른 채** 움직이면 시점이 돈다 (헤드셋이 없을 때만).
  - 이동은 **WASD**나 방향키, 달리기는 **왼쪽 Shift**, 질주는 **마우스 왼쪽 클릭**(또는 오른쪽 Shift)이다.
  - XR Device Simulator를 켜면 WASD가 시뮬레이터와 겹친다. 그때는 방향키를 쓴다.

## 2. Unity에서 할 일 (순서대로)

### 처음 한 번 (STAGE1.md의 3-0 ~ 3-1과 같음)
1. GitHub Desktop **Pull** → Unity를 열고 Console에 빨간 에러가 없는지 확인한다.
2. **Edit → Project Settings → XR Plug-in Management**
   - Android 탭에서 **OpenXR** 체크 (OpenXR 패키지가 자동 설치된다)
   - OpenXR → Android 탭 → Interaction Profiles에 **Meta Quest Touch Plus Controller Profile**과 **Oculus Touch Controller Profile** 추가
   - Feature Groups에서 **Meta Quest** 체크 → **Meta Quest Support** 체크
3. **File → Build Profiles → Android → Switch Platform**
4. 메뉴 **PetitParis → 0. Apply Quest Android Player Settings** → 적용
5. **XR Plug-in Management → Project Validation → Fix All**
6. (선택, 권장) 메뉴 **PetitParis → 5. (선택) Quest URP 최적화 적용**
   - HDR 끔, MSAA 4x, Render Scale 1.0, 그림자 거리 10m
   - Android 품질 단계만 바뀐다. 적용하지 않아도 게임은 동작한다.
7. (에디터 테스트용) Package Manager → XR Interaction Toolkit → Samples → **XR Device Simulator Import**
   → Project Settings → XR Interaction Toolkit → **Use XR Device Simulator in scenes** 체크

### 씬 만들기
8. 메뉴 **PetitParis → 1. Build Sandbox Scene (조작 테스트)** → `Assets/PetitParis/Scenes/PetitParisSandbox.unity`
9. 메뉴 **PetitParis → 3. Build Game Scene (PetitParisVR)** → `Assets/PetitParis/Scenes/PetitParisVR.unity` (빌드 목록 맨 앞)
10. 각 씬을 연 상태에서 메뉴 **PetitParis → 4. Validate Scene** → Console에서 `FAIL 0`인지 확인한다.
    - WARN이 나오면 문구를 확인한다. 특히 "비네트 Provider 목록"과 "XRI 버전" 항목.
11. 생성된 파일을 커밋한다: `Assets/PetitParis/**`, `Assets/XR/**`, `ProjectSettings/**`, `Packages/**`

> 1단계에서 만든 `PetitParisVR` 테스트 씬이 있다면, 9번에서 게임 씬으로 덮어쓴다. 크기 테스트 내용은 샌드박스 씬(8번)으로 옮겼다.

### Inspector에서 조정할 값
| 에셋 / 컴포넌트 | 주요 값 (기본) |
|---|---|
| `Settings/RatScaleConfig` | 배율 0.1, 눈높이 0.12m, **걷기 0.15 m/s**, **질주 0.45 m/s**, 몸 반지름 0.035m |
| `Settings/RatComfortSettings` | 회전 30°, 질주·점프 on, 질주 비네트 on, 걷기 비네트 off, 체감 속도 상한 5 m/s |
| `XR Origin (Rat)/Locomotion/Rat Movement` | RatLocomotionController (가속 1.2, 감속 1.8, 데드존 0.15, 체감 중력 on), RatDashController (0.45s, 가속 0.08s, 감속 0.15s, 쿨다운 1.2s), RatJumpController (높이 0.035m, 끌 수 있음), RatCrouchController (Toggle, 속도 0.5, 폭 0.7) |
| `Pedestrians/*` (PedestrianPath) | 속도 0.8~0.9 m/s, 보폭 0.6, 발 들기 0.14, 일시정지, 시작 지연, 발소리 거리 0.3~10m |
| 신발 `Foot_L/R` (FootstepHazard) | 경고 반경 0.4m, 판정 여유 0 |
| `Vehicles/Bicycle_CrossStreet` | 속도 2.2 m/s, 끝에서 정지 2.5s, 벨·바퀴 소리 |
| 안전 구역 `SafeZone_*` (RatSafeZone) | 이름, 웅크려야 안전 여부, BoxCollider 크기 |

---

## 3. 테스트 방법

### 3-1. 이동 테스트 — 샌드박스 씬 (XR Device Simulator 또는 Quest)
| # | 확인 | 방법 | 기대 결과 |
|---|---|---|---|
| 1 | 전진 | 왼쪽 스틱 앞으로 (키보드 ↑) | 바라보는 수평 방향으로 부드럽게 출발하고 멈춘다. 위·아래를 봐도 수평으로만 이동 |
| 2 | 방향 전환 | 왼쪽 스틱 좌우 (←/→) | 옆걸음 (Strafe) |
| 3 | 스냅 회전 | 오른쪽 스틱 좌/우 (`,` / `.`) | 30도씩, 0.5초 간격. 왼쪽 스틱으로는 회전하지 않음 |
| 4 | 질주 | 스틱을 민 채 오른쪽 트리거 (오른쪽 Shift) | 약 0.45초 빠르게 이동 → 감속 → 손목 패널에 "질주 대기" 1.2초. 스틱을 놓고 누르면 질주하지 않음 |
| 5 | 질주 벽 충돌 | z=4의 12cm 보도 턱으로 질주 | 통과하지 않고 즉시 정지 (디버그 패널 Dash 상태 → Cooldown) |
| 6 | 카메라 흔들림 | 질주 중 시야 | 흔들림·기울어짐 없음. 시야 가장자리만 어두워짐 (비네트) |
| 7 | 점프 | 3cm 턱 앞에서 A (Space) | 걸어서는 못 올라가고, 점프하면 올라감. 공중에서 A는 무시 |
| 8 | 웅크리기 | 6cm 틈 앞에서 B (C) | 서 있으면 막히고, 웅크리면 통과. 틈 안에서 B → "좁아서 일어설 수 없어요" → 나오면 자동으로 일어섬 |
| 9 | 중복 없음 | 메뉴 4. Validate Scene | `이동 Provider: RatLocomotionController, SnapTurnProvider, RatComfortApplier`, FAIL 0. 디버그 패널 Speed가 RatScaleConfig 걷기 속도(15 cm/s)를 넘지 않음 |
| + | 안전 구역 | 의자 아래로 들어감 | 바닥 초록 표시가 진해지고, 손목에 "안전 · 의자 아래" |
| + | 밟힘 | 보행자 신발에 닿음 | 실패음 → 0.25초 페이드 → 체크포인트에서 재시작. 날아가거나 밀리지 않음 |

### 3-2. 게임 테스트 — PetitParisVR 씬
1. Play → 검은 화면 → 페이드 인. 벽 틈 안에서 동쪽을 보고 있고, 앞에 조작 안내 패널이 보인다.
2. 틈 밖으로 나가면 패널이 사라지고 금색 마름모가 첫 체크포인트(테라스 끝 의자 아래)를 가리킨다.
3. 보행자 A의 발소리가 커지면 의자 아래로 숨는다 → "잘 숨었어요!" 메시지.
4. 테라스 끝 의자 아래에 도착 → 체크포인트 소리, 손목 "체크포인트 2/4".
5. 빵집 앞: 보행자 B 사이를 질주로 통과 → 벤치 아래 체크포인트.
6. 골목: 벤치 아래에서 자전거 벨과 바퀴 소리를 듣고, 멈춘 틈에 가로 골목을 건넌다.
7. 출구 도착 → 완료 패널(시간, 다시 시작 횟수) → A 버튼 → 처음부터 다시.
8. 일부러 밟히거나 자전거에 치여서, 가장 가까운 활성 체크포인트에서 재시작되는지 확인한다.

### 3-3. Quest 3 빌드·설치
1. Quest: 개발자 모드 켜기 (휴대폰 Meta Horizon 앱 → 기기 → 개발자 모드) → USB-C 데이터 케이블 연결 → 헤드셋에서 **USB 디버깅 허용**
2. Unity **File → Build Profiles → Android**
   - Scene List 맨 위가 `PetitParis/Scenes/PetitParisVR`인지 확인
   - **Run Device**에서 Quest 3 선택 (안 보이면 Refresh)
3. **Build And Run** → APK 저장 위치는 `Builds/PetitParis.apk` (git 제외 폴더) → 설치 후 자동 실행
4. 다시 실행할 때: 헤드셋 앱 라이브러리 → 필터 "알 수 없는 출처" → **PETIT PARIS**
5. 헤드셋에서 확인할 것
   - 크기감 (의자가 건물처럼 보이는지)
   - 손목 패널 글자가 한글로 나오는지
   - 발소리 방향
   - 질주 비네트
   - 72fps 유지 (오른손 디버그 패널은 게임 씬에서 꺼져 있다. 성능 측정은 OVR Metrics Tool 사용)

---

## 4. 점검 결과 (요청 7개 항목)

### ① Console 오류
- **첫 실제 Unity 컴파일 결과 (6000.3.9f1)**: `RatLocomotionController.cs(70,18): error CS0246` 1건 → XRI 3.3.2에 없는 `XRBodyTranslation`을 써서 생긴 에러였다. `XROriginMovement`로 고쳤다 (수정 후 재컴파일은 **미검증**).
- 같은 Console의 `Android SDK Build-tools component is not found`는 코드 문제가 아니다. 해당 에디터에 Android SDK 모듈이 설치되지 않았다는 뜻이다.
- **대신 한 것**: Roslyn 컴파일러로 PetitParis 스크립트 34개를 컴파일해 에러 0을 확인했다.
  - 문법 확인
  - Unity·XRI API 스텁 기준 확인: 타입, 메서드, 스크립트 간 참조
- 실제 Unity 컴파일 결과는 사용자가 확인해야 한다.

### ② 설치 패키지 버전 ↔ 코드 API
설치된 버전 (`Packages/packages-lock.json`): XRI **3.3.2**, XR Core Utils **2.5.3**, Input System **1.18.0**, URP **17.3.0**, Unity **6000.3.9f1**.
OpenXR과 XR Management는 아직 설치되지 않았고, 코드는 이 둘에 의존하지 않는다.

| API | 확인 방법 | 상태 |
|---|---|---|
| `XROrigin` 필드 (m_Camera, m_OriginBaseGameObject, m_CameraFloorOffsetObject, m_RequestedTrackingOriginMode, m_CameraYOffset) | 레포의 XRI 3.3.2 샘플 프리팹 YAML과 대조 | 확인 |
| `TrackedPoseDriver` 필드 (m_PositionInput 등, m_UseReference, m_Reference) | 샘플 프리팹 YAML | 확인 |
| `SnapTurnProvider` 필드 (m_TurnAmount, m_DebounceTime, m_EnableTurnAround, m_LeftHandTurnInput.m_InputSourceMode, m_InputActionReference) | 샘플 프리팹 YAML | 확인 |
| `XRBodyTransformer` 필드 (m_XROrigin, m_UseCharacterControllerIfExists), `LocomotionMediator`(직렬화 필드 없음), `LocomotionProvider.m_Mediator` | 샘플 프리팹 YAML | 확인 |
| `TunnelingVignetteController.m_LocomotionVignetteProviders[].m_LocomotionProvider / m_Enabled` | XRI 3.3.2 소스 (needle-mirror) | 확인 (처음엔 `m_Locomotion`으로 잘못 써서 수정함) |
| `mediator.xrOrigin`, 네임스페이스 `Locomotion.Movement` | 샘플 스크립트 DynamicMoveProvider | 확인 |
| `LocomotionProvider.TryStartLocomotionImmediately / TryEndLocomotion / TryQueueTransformation / locomotionState`, `LocomotionState`, `XROriginMovement.motion`, `SnapTurnProvider.turnAmount / debounceTime`, 네임스페이스 `Locomotion.Turning` | XRI 3.3.2 소스 (needle-mirror) | 확인 (처음엔 존재하지 않는 `XRBodyTranslation`을 써서 컴파일 에러 → 수정함) |
| `TMP_FontAsset.CreateFontAsset(Font, int, int, GlyphRenderMode, int, int, AtlasPopulationMode, bool)`, `TryAddCharacters` | TMP 3.2.0-pre.15 소스 (ugui 2.0에 포함된 TMP와 같은 계열) | 확인 |
| 어셈블리 이름 (Unity.InputSystem, Unity.XR.CoreUtils, Unity.XR.Interaction.Toolkit) | 샘플 asmdef와 대조 | 확인 |
| Unity.TextMeshPro 어셈블리 이름 | TMP 소스 asmdef + Unity 실제 컴파일(런타임 어셈블리 통과) | 확인 |

### ③ Input Action 연결
- **에셋**: 액션 16개. 코드에서 참조하는 이름과 정확히 일치하고, 누락이나 미사용 액션이 없다 (스크립트로 대조).
- **씬 연결은 빌더가 자동으로 한다**
  - TrackedPoseDriver 3개 → Tracking/*
  - SnapTurn 오른손 → Rat/Turn, 왼손 → 사용 안 함
  - RatInputReader → 에셋 전체 활성화
- **실행할 때**: `RatInputReader`가 누락된 액션 이름을 한 번에 에러로 출력한다.
- **씬 점검**: 메뉴 4번이 액션별 바인딩까지 출력한다.
- **바인딩 방식**: XR 바인딩은 `<XRController>{LeftHand}/{Primary2DAxis}` 같은 usage 기반이라 Touch Plus 프로필과 시뮬레이터에서 모두 동작하도록 했다. Touch Plus 실기기 입력 확인은 **미검증**이다.
- **겹침 회피**: 프로젝트 전역 `InputSystem_Actions`(`Player/Move`가 양손 스틱에 바인딩됨)는 읽지 않는다.

### ④ XR Origin과 CharacterController 충돌 여부
| 경로 | 무엇을 바꾸나 | 충돌 여부 |
|---|---|---|
| TrackedPoseDriver | Main Camera·컨트롤러의 로컬 위치·회전 | 다른 코드는 카메라를 쓰지 않음 |
| RatEyeCalibrator | Rat Tracking Space (캘리브레이션 순간에만) | 눈을 XR Origin 바로 위로 옮겨서 CharacterController 위치와 일치시킨다 |
| RatLocomotionController → XRBodyTransformer → CC.Move | XR Origin 위치 | **유일한 이동 경로** |
| SnapTurnProvider → XRBodyTransformer | XR Origin Yaw | 회전만 함 |
| RatRespawn | XR Origin 위치 (화면이 검을 때) | 이동 큐를 멈추고 CC를 끈 상태에서만 바꾼다 → CC와 다투지 않음 |
| RatCrouchController | CC 반지름 | XRI는 높이와 중심만 다룬다고 판단. 실제 동작은 **미검증** |

- 확인된 위험 하나: XRI `CharacterControllerBodyManipulator`가 이동할 때 CC 높이를 머리 높이(약 0.12m)로 맞출 수 있다. RatScaleConfig의 0.13m보다 1cm 낮아지는 정도라 동작에는 문제가 없다.

### ⑤ Quest 3 성능 위험
| 항목 | 위험 | 대응 |
|---|---|---|
| Mobile_RPAsset: HDR 켜짐, MSAA 꺼짐, Render Scale 0.8, 그림자 거리 50m | 대역폭 낭비, 계단 현상, 흐림, 작은 물체 그림자 해상도 부족 | 메뉴 5번(선택)으로 수정 |
| 실시간 그림자 (Directional 1개, Hard) | GPU 비용 중간 | 72fps가 안 나오면 빌더의 `SetupLighting(true)`를 false로 바꾼다 (NPC 그림자는 Blob이라 유지됨) |
| 환경 오브젝트 약 400개 | 드로우콜 | Static Batching과 SRP Batcher로 묶인다 |
| MaterialPropertyBlock (깃발, 안전 구역, 그림자) | 해당 렌더러만 SRP Batcher에서 빠짐 | 수십 개 수준이라 영향 작음 |
| Comfort Vignette | 질주 중 반투명 반구 오버드로 | 질주(0.45초) 중에만 켜짐 |
| 한글 동적 폰트 | 런타임에 새 글자를 래스터화하면 잠깐 끊길 수 있음 | 게임 문장 글자는 빌드 전에 미리 채움 |
| 효과음 생성 | 첫 사용 때 몇 ms | Start에서 생성 |
| TMP 패널 갱신 | 문자열 생성으로 GC | 0.15~0.2초 간격으로만 갱신 |

- **72fps 달성 여부는 미검증**이다.

### ⑥ 미검증 항목
- 실제 Unity 컴파일, 씬 생성 메뉴 실행 결과, Console
- OpenXR 설치와 Project Validation 결과
- Quest 3에서의 머리 추적, 크기감(시차 스케일), 한글 표시, Touch Plus 버튼, 비네트 위치(near clip 5mm 근처), 3D 음향 방향감, 72fps
- XRI 내부 동작: CharacterControllerBodyManipulator가 CC 반지름·높이를 어떻게 다루는지, TunnelingVignetteController가 Provider 상태에 반응하는지
- XR Device Simulator 키(WASD, Q/E 등)와 이 게임 키보드 키(방향키, `,` `.`, Space, C, 오른쪽 Shift)가 겹치는지
- 게임 난이도와 플레이 시간 (3~5분 목표, 블록아웃 수치는 추정)

### ⑦ 발견한 문제와 수정 (이번 작업에서)
| 문제 | 원인 | 수정 |
|---|---|---|
| 1단계 테스트 씬과 게임 씬 이름이 같음 | 기획상 게임 씬이 PetitParisVR | 테스트용은 PetitParisSandbox로 분리 |
| 1단계 RatInputReader로는 "한 번 눌림"을 알 수 없음 | IsPressed만 있음 | WasPressedThisFrame 기반 `*PressedThisFrame` 추가 |
| 1단계 리그에 이동 경로가 없어 CC가 머리를 따라가지 않음 | 1단계 범위 밖 | XRBodyTransformer(UseCharacterController)와 Mediator 추가 |
| 한글 폰트 없음 (CLAUDE.md 규칙: 화면 텍스트는 한국어) | 프로젝트에 LiberationSans만 있음 | 나눔고딕(OFL) 추가, TMP 동적 폰트 자동 생성, CREDITS 기록 |
| 질주 충돌 검사가 자기 몸에 걸릴 수 있음 | 플레이어 CC도 Collider | XR Origin을 Ignore Raycast 레이어에 두고 검사 마스크에서 제외 |
| 원기둥 기본 CapsuleCollider가 납작한 원판에서 구로 커짐 | 프리미티브 특성 | Convex MeshCollider로 교체 |

---

## 5. 예상되는 문제와 해결

| 증상 | 해결 |
|---|---|
| `TryQueueTransformation`, `locomotionState`, `turnAmount` 관련 컴파일 에러 | XRI API 차이. 에러 전문을 보내 주면 수정한다 |
| 검증에서 "비네트 Provider 항목 구조가 예상과 다름" | Main Camera/TunnelingVignette의 Locomotion Vignette Providers에 `+` → Locomotion Provider 칸에 `XR Origin (Rat)/Locomotion/Comfort` 드래그 |
| 이동이 전혀 안 됨 | Console에 RatLocomotionController 참조 에러가 있는지 확인. 검증 메뉴의 Mediator 항목 확인 |
| 이동이 두 배 빠름 | 다른 Move Provider가 씬에 있음 (검증 메뉴가 FAIL로 표시) → 삭제 |
| 한글이 □로 나옴 | `UI/Fonts/NanumGothicBold SDF` 에셋을 선택 → Inspector에서 Atlas Population Mode가 Dynamic인지 확인 → 메뉴 3번 다시 실행 |
| 발소리가 너무 작거나 큼 | PedestrianPath의 Footstep Volume과 Audio Max Distance 조정 |
| 너무 어려움 | 보행자 Walk Speed를 낮추고, FootstepHazard Hit Padding을 -0.01, 자전거 Pause At Ends를 늘린다 |
| 멀미 | RatComfortSettings에서 vignetteOnWalk 켜기, 질주 속도와 maxPerceivedSpeed 낮추기, enableJump 끄기 |
