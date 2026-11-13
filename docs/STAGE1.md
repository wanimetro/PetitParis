# PETIT PARIS — 1단계: XR 세팅 + 쥐 1인칭 시점

> **갱신 안내**: 2단계 이후 메뉴 이름이 바뀌었다. 크기 테스트 씬은 `PetitParis → 1. Build Sandbox Scene` (PetitParisSandbox.unity), 게임 씬은 `3. Build Game Scene` (PetitParisVR.unity), 검증은 `4. Validate Scene`이다. 최신 전체 안내는 `docs/README.md`를 본다.

목표: Meta Quest 3에서 쥐 눈높이(12cm)로 테스트 공간을 바라본다. 머리 추적은 그대로 유지한다.

> 상태: **코드 작성 완료, Unity 에디터·Quest 실기기 검증 전.**
> 클라우드 환경에서 작성했기 때문에 Unity로 컴파일하거나 실행해 보지 못했다. 아래 "직접 할 일"을 순서대로 진행하고, Console 에러가 나면 전문을 붙여 달라.

## 1. 설계 요약

- **방식 A (플레이어 축소)**. 월드는 실제 파리 크기(미터)로 만들고, XR 리그 안의 `Rat Tracking Space`만 `0.1`배로 줄인다.
  - 머리 회전은 1:1, 머리 이동은 1/10로 반영된다. 시차(IPD)도 함께 줄어 세상이 약 10배 크게 보인다. 시차가 실제로 줄어드는지는 실기기에서 확인해야 한다.
  - `XR Origin`과 `Camera Offset`은 스케일 1을 유지한다. 그래서 CharacterController, 이동 속도, XROrigin 계산은 모두 월드 미터 하나의 좌표계로 다룬다.
- **카메라 Transform은 TrackedPoseDriver만 쓴다.** 우리 코드는 카메라를 매 프레임 고정하지 않는다.
- **캘리브레이션**: `RatEyeCalibrator`가 캘리브레이션 순간에만 `Rat Tracking Space`의 위치와 Yaw를 바꾼다. 지금 눈 위치를 XR Origin 바로 위 12cm로 옮기는 방식이다. 그래서 앉든 서든, 키가 크든 작든 같은 눈높이가 된다. 시점이 순간 이동하므로 페이드 아웃/인을 함께 쓴다.
  - 실행 시점: 시작할 때 자동, 왼손 **Menu(≡) 1초**, 왼손 **Y 1초**, 키보드 **F5**, Quest 재중심(Meta 버튼 길게) 때 자동
- **모든 수치의 기준은 `Assets/PetitParis/Settings/RatScaleConfig.asset` 하나다.** Inspector에서 체감 환산값도 함께 보여준다.

## 2. 추가·생성된 파일

| 경로 | 내용 |
|---|---|
| `Assets/PetitParis/Scripts/PetitParis.Runtime.asmdef` | 런타임 어셈블리 (Input System, XR Core Utils, TMP 참조) |
| `Scripts/Core/RatScaleConfig.cs` | 스케일 체계 ScriptableObject (배율, 눈높이, 충돌체, 클리핑, 속도) |
| `Scripts/Core/ScreenFader.cs` | 카메라 앞 Quad 페이드 |
| `Scripts/Player/RatScaleRig.cs` | Config 값을 리그에 적용 (스케일, CharacterController, 클리핑) |
| `Scripts/Player/RatEyeCalibrator.cs` | 눈높이·방향 캘리브레이션 |
| `Scripts/Player/XROriginGuard.cs` | XR Origin 중복 방지 |
| `Scripts/Input/RatInputReader.cs` | 입력 에셋 활성화, 액션 누락 검사, 값 읽기 |
| `Scripts/Diagnostics/RatDebugPanel.cs` | 왼손 손목 디버그 패널 (실기기 확인용) |
| `Shaders/ScreenFade.shader` | 페이드 셰이더 (URP, 스테레오 대응) |
| `Settings/PetitParisInput.inputactions` | Tracking 맵 + Rat 맵 |
| `Editor/PetitParisSceneBuilder.cs` | 메뉴 `PetitParis > 1. Build Sandbox Scene` |
| `Editor/PetitParisProjectSetup.cs` | 메뉴 `PetitParis > 0. Apply Quest Android Player Settings` |
| `Editor/PetitParisValidator.cs` | 메뉴 `PetitParis > 4. Validate Scene` (읽기 전용 점검) |
| `Editor/RatScaleConfigEditor.cs` | Config Inspector 체감값 표시 |

메뉴 1번을 실행하면 생성되는 것: `Scenes/PetitParisVR.unity`, `Prefabs/XR Origin (Rat).prefab`, `Settings/RatScaleConfig.asset`, `Materials/PP_*.mat`

(이 단계는 원래 LesMiserables3D 레포 안에서 작업했고, 이후 PetitParis 전용 레포로 옮겼다.)

## 3. Unity에서 직접 할 일 (클릭 순서)

### 3-0. 준비
1. GitHub Desktop: PetitParis 레포를 Clone하고 **Fetch origin → Pull**
2. Unity Hub → Installs → 6000.3.9f1 → ⚙ → **Add modules** → **Android Build Support**(하위 OpenJDK, Android SDK & NDK Tools 포함) 설치. 이미 있으면 건너뛴다.
3. 프로젝트를 연다 → 컴파일이 끝나면 **Console에 빨간 에러가 없는지** 확인한다. 에러가 있으면 전문을 복사해서 알려 달라.

### 3-1. XR Plug-in Management + OpenXR (패키지는 여기서 설치)
4. **Edit → Project Settings → XR Plug-in Management**
   - "Install XR Plugin Management" 버튼이 보이면 누른다.
5. **Android 탭**(안드로이드 아이콘) → Plug-in Providers에서 **OpenXR** 체크
   - OpenXR 패키지가 자동 설치된다. Unity가 이 에디터 버전에 맞는 버전을 고른다.
   - 같은 화면에 **Meta Quest feature group**이 보이면 체크한다.
6. **Windows(PC) 탭**은 OpenXR을 **체크하지 않는다.** 헤드셋 없이 하는 에디터 테스트는 XR Device Simulator를 쓴다. Quest Link가 되는 PC라면 나중에 켜도 된다.
7. **XR Plug-in Management → OpenXR → Android 탭**
   - **Enabled Interaction Profiles**의 `+` → **Meta Quest Touch Plus Controller Profile** 추가
   - 같은 방법으로 **Oculus Touch Controller Profile**도 추가 (예비용)
   - **OpenXR Feature Groups**에서 **Meta Quest** 체크 → 오른쪽 목록의 **Meta Quest Support** 체크
   - Meta Quest Support 옆 ⚙ → Target Devices에 **Quest 3**가 있으면 체크
   - Render Mode는 기본값(Single Pass Instanced / Multi-view)을 유지한다
8. **File → Build Profiles** → **Android** 선택 → **Switch Platform** (시간이 걸린다)
9. 메뉴 **PetitParis → 0. Apply Quest Android Player Settings** → 대화상자 확인 → **적용**
10. **XR Plug-in Management → Project Validation** → Android 탭 → 남은 항목에 **Fix All**
    - Fix All이 무엇을 바꿨는지(목록 문구) 알려 주면 다음 단계에 반영한다.

### 3-2. 씬 만들기
11. 메뉴 **PetitParis → 1. Build Sandbox Scene (조작 테스트)**
    - 저장할지 묻는 창이 뜨면 열려 있던 씬을 저장하거나 Don't Save를 고른다. 새 씬만 생성되고 기존 씬은 바뀌지 않는다.
12. 메뉴 **PetitParis → 4. Validate Scene** → Console에서 `FAIL 0`인지 확인한다. WARN은 내용을 읽어 본다.
13. 확인이 끝나면 GitHub Desktop에서 생성된 파일(`Assets/PetitParis/...`, `Assets/XR/...`, `ProjectSettings/...`, `Packages/manifest.json`)을 커밋한다.

### 3-3. (에디터 테스트용) XR Device Simulator
14. **Window → Package Manager** → XR Interaction Toolkit → **Samples** 탭 → **XR Device Simulator** → **Import**
15. **Edit → Project Settings → XR Plug-in Management → XR Interaction Toolkit** → **Use XR Device Simulator in scenes**(또는 "XR Interaction Simulator") 체크. "In Editor Only"는 그대로 둔다.

## 4. 씬 계층 구조와 설정값

```
PetitParisVR
├ Directional Light            Light: Directional, Intensity 1.2, Color (1,0.96,0.88), Shadows Hard, Rot (50,-35,0)
├ Environment_Greybox          (전부 Static: Batching/Occluder/Occludee, TMP 라벨만 제외)
│  ├ Street
│  │  ├ Ground_Cobblestone     Box 12 × 0.1 × 12 m, 윗면 y=0
│  │  ├ Sidewalk_12cm          Box 12 × 0.12 × 3 m, z 4~7 (보도 턱 = 쥐 눈높이)
│  │  ├ BuildingWall           Box 12 × 3 × 0.3 m, z 7.15
│  │  └ SideWall_Left          Box 0.3 × 3 × 7 m, x -3.15
│  ├ Cafe
│  │  ├ CafeChair_A            (0.6, 0, 1.2) 좌판 높이 45cm, 42×42cm, 등받이 상단 86cm
│  │  ├ CafeTable              (1.5, 0, 1.75) 상판 지름 60cm, 높이 75cm
│  │  ├ CafeChair_B            (1.5, 0, 2.4) 180° 회전
│  │  └ Label_Chair
│  ├ HumanReference_175cm      (-1.5, 0, 2.4) 신발 28×11×9cm, 다리, 몸통, 팔, 머리 (전부 충돌체 있음)
│  ├ Label_Human
│  └ ScaleReferences
│     ├ Cube_10cm / Cube_50cm / Cube_1m (+ 라벨)
│     └ HeightRuler_2m         (-0.35, 0, 0.6) 10cm 간격 눈금, 빨간 선 = 목표 눈높이 12cm
├ Gameplay
│  └ SpawnPoint (0,0,0)        └ SpawnMarker (노란 판, 충돌체 없음)
└ XR Origin (Rat)              (0,0,0), Scale 1   ← 프리팹 연결
   │  XROrigin                 Camera=Main Camera, Origin Base=자기 자신, Camera Floor Offset=Camera Offset,
   │                           Requested Tracking Origin Mode=Floor, Camera Y Offset=0
   │  CharacterController      Radius 0.035, Height 0.13, Center (0,0.0685,0), Skin 0.0035,
   │                           Step Offset 0.02, Slope 50, Min Move 0
   │  RatScaleRig              Config=RatScaleConfig, Tracking Space, Body, Eye Camera 연결
   │  RatInputReader           Actions=PetitParisInput
   │  RatEyeCalibrator         Auto On Start ✔, Timeout 4s, Settle 0.3s, Desktop 0.5s,
   │                           Recalibrate On Recenter ✔, Fade Out 0.2s / In 0.35s
   │  XROriginGuard
   └ Camera Offset             (0,0,0), Scale 1 (높이는 XROrigin이 관리, Floor 모드 = 0)
      └ Rat Tracking Space     (0, 0.12, 0) ← 캘리브레이션이 바꿈, Scale 0.1
         ├ Main Camera         Tag MainCamera
         │  │ Camera           Clear Solid Color (0.62,0.74,0.86), Near 0.005, Far 80, HDR off, MSAA on, Target Eye Both
         │  │ AudioListener
         │  │ TrackedPoseDriver Rotation And Position, Update And Before Render,
         │  │                  Position=Tracking/HeadPosition, Rotation=Tracking/HeadRotation, State=Tracking/HeadTrackingState
         │  └ Screen Fader     Quad, PP_ScreenFade, 그림자 off, ScreenFader(Start Opaque ✔), local z 0.2 / scale 1.6
         ├ Left Controller     (-0.18,-0.35,0.3) 추적 전 기본 위치, TrackedPoseDriver = Tracking/Left*
         │  ├ Paw Visual       Sphere 5×3.5×7 (추적 공간 cm), 충돌체 없음
         │  └ Wrist Debug Panel (0,0.1,-0.03), Rot (45,0,0)
         │     ├ Background    Quad 0.23×0.15, PP_PanelBackground(Unlit)
         │     └ Text          TextMeshPro 0.09pt, 0.22×0.14, RatDebugPanel (갱신 0.2s)
         └ Right Controller    (0.18,-0.35,0.3), TrackedPoseDriver = Tracking/Right*
            └ Paw Visual
```

### RatScaleConfig 기본값

| 항목 | 값 (월드) | 체감 (배율 ×10) |
|---|---|---|
| trackingScale | 0.1 | 세상이 10배 |
| targetEyeHeight | 0.12 m | 사람 눈 1.2m 높이에서 보는 크기감 |
| bodyRadius / bodyHeight | 0.035 / 0.13 m | 쥐 몸 |
| stepOffset | 0.02 m | 2cm 턱 넘기 (12cm 보도 턱은 못 넘음) |
| nearClip / farClip | 0.005 / 80 m | |
| walkSpeed / dashSpeed | 0.15 / 0.45 m/s | 1.5 / 4.5 m/s (다음 단계에서 사용) |

### 입력 매핑 (PetitParisInput.inputactions)

| 액션 | Quest 3 Touch Plus | 키보드 (헤드셋 없을 때) | 1단계 사용 |
|---|---|---|---|
| Tracking/Head* | `<XRHMD>/centerEyePosition·Rotation·trackingState` | — | ✔ |
| Tracking/Left*, Right* | `<XRController>{Left/RightHand}/devicePosition·deviceRotation·trackingState` | — | ✔ |
| Rat/Move | 왼쪽 스틱 `{Primary2DAxis}` | 방향키 | 표시만 |
| Rat/Turn | 오른쪽 스틱 | `,` / `.` | 표시만 |
| Rat/Dash | 오른쪽 트리거 `{TriggerButton}` | 오른쪽 Shift | 표시만 |
| Rat/Jump | A `{RightHand}{PrimaryButton}` | Space | 표시만 |
| Rat/Crouch | B `{RightHand}{SecondaryButton}` | C | 표시만 |
| Rat/Interact | 왼쪽 트리거 | F | 표시만 |
| Rat/Calibrate | 왼쪽 Menu 1초 Hold, Y 1초 Hold | F5 | ✔ |

키보드 키는 XR Device Simulator의 단축키(WASD, Q/E 등)와 겹치지 않게 골랐다. 이동이 들어가는 단계에서 다시 점검한다.

## 5. 테스트 방법

### Play Mode (헤드셋 없음, 시뮬레이터 꺼짐)
1. PetitParisVR을 열고 Play한다.
2. 화면이 0.5초쯤 검다가 밝아진다. Console에 `캘리브레이션 #1: 실제 머리 높이 0.00 m → 쥐 눈높이 12.0 cm`가 찍히는지 본다.
3. Game 뷰에서 확인할 것: 정면의 의자 다리, 보도 턱(12cm), 자의 **빨간 눈금이 화면 세로 중앙(눈높이)**에 오는지. 사람 신발이 눈 바로 아래에 보이는지.
4. F5를 누르면 페이드 아웃/인과 함께 `캘리브레이션 #2`가 찍힌다.
5. Scene 뷰에서 `XR Origin (Rat)`을 선택하면 노란 점(목표 눈높이)과 초록 캡슐(몸 충돌체)이 보인다.

### Play Mode (XR Device Simulator)
1. 3-3을 끝낸 뒤 Play한다.
2. 시뮬레이터 안내에 따라 HMD를 회전·이동한다. 회전은 그대로, 이동은 1/10로 반영되는지 확인한다.
3. 시뮬레이터로 HMD를 위로 올린 뒤 F5 → 다시 12cm로 돌아오는지 확인한다. 앉은 자세와 선 자세를 흉내 내는 테스트다.
4. 컨트롤러 버튼을 눌렀을 때 손목 패널 값(Move/Turn/Dash/Jump/Crouch/Use)이 바뀌는지 확인한다.

### Quest 3 실기기
1. Quest 개발자 모드를 켜고 USB-C 데이터 케이블로 연결 → 헤드셋에서 "USB 디버깅 허용"
2. **File → Build Profiles → Android → Run Device**에서 Quest 선택 → **Build And Run**. APK는 `Builds/` 폴더에 저장한다 (git 제외 폴더).
3. 헤드셋에서 확인할 것:
   - [ ] 시작이 검은 화면 → 페이드 인. 손목 패널 `HMD P+R`
   - [ ] **크기감**: 의자가 건물처럼, 사람 신발이 차처럼 크게 느껴지는가 ← 방식 A의 핵심 검증
   - [ ] 자의 빨간 12cm 선이 정면 눈높이에 있는가. 패널 `Eye height ≈ 12 cm`
   - [ ] 고개 회전은 자연스럽고, 몸을 앞으로 숙이면 조금만 움직이는가 (머리 이동 1/10)
   - [ ] 앉아서 왼손 Menu 1초 → 페이드 후 12cm. 일어서서 다시 Menu 1초 → 같은 12cm
   - [ ] Meta 버튼을 길게 눌러 재중심 → 자동 재캘리브레이션 (패널 `Calibrated: Nx` 증가)
   - [ ] 스틱, 트리거, A/B 입력이 패널에 표시되는가 (Touch Plus 바인딩 확인)
   - [ ] 패널 FPS가 72 근처인가
   - [ ] 멀미가 나는 순간이 있는가

## 6. 검증 상태

| 항목 | 상태 |
|---|---|
| C# 문법 | 확인함: Roslyn 컴파일러로 문법 오류 없음 |
| 스크립트 간 참조·타입 | 확인함: Unity API를 흉내 낸 스텁으로 컴파일 성공. 실제 Unity 컴파일은 아님 |
| 실제 Unity 컴파일, 씬 생성 | **미검증** (Unity 에디터 없음) |
| XROrigin·TrackedPoseDriver 직렬화 필드 이름 | 레포의 XRI 3.3.2 샘플 프리팹과 대조함 |
| OpenXR 설치, Touch Plus 프로필 | **미검증** (사용자가 클릭으로 설치) |
| Quest 실기기 추적, 크기감, 캘리브레이션, 입력, FPS | **미검증** |

## 7. 예상되는 문제와 해결

| 증상 | 원인 / 해결 |
|---|---|
| Console에 컴파일 에러 | 실제 API와 다른 부분이 있을 수 있다. 에러 전문을 보내 주면 수정한다 |
| Project Settings에 Android 탭이 없음 | Android Build Support 모듈 미설치 → 3-0의 2번 |
| 시작 후 계속 검은 화면 | `PP_ScreenFade` 셰이더/머티리얼 문제 또는 캘리브레이션 예외. Console 확인. 임시로 `Screen Fader` 오브젝트를 끄면 진행할 수 있다 |
| 세상이 커 보이지 않음 (눈높이만 낮은 느낌) | 스케일된 부모가 양안 시차에 적용되지 않는 경우. 실기기에서만 알 수 있다. 이러면 방식 B(월드 확대)로 전환을 검토한다. Config 기반이라 영향 범위는 리그로 한정된다 |
| 그림자가 각지거나 깨짐 | `Mobile_RPAsset` Shadow Distance가 50m라 작은 물체에 해상도가 부족함. 6~10m로 줄이면 좋아진다. 프로젝트 전체 설정이라 승인 후 진행한다 |
| 컨트롤러가 움직이지 않음 / 버튼 값 0 | OpenXR Android 탭의 Interaction Profile 누락 (3-1의 7번) |
| Menu 길게 누르기가 안 됨 | Y 1초 또는 F5를 쓴다. 결과를 알려 주면 바인딩을 조정한다 |
| 시작 직후 캘리브레이션이 2번 됨 | XROrigin이 Floor 모드를 설정하면서 재중심 이벤트가 오는 경우. 동작에는 문제없다 |
| 물체에 얼굴을 바짝 대면 뚫려 보임 | near clip 5mm. 머리 충돌 페이드는 이동 단계에서 추가한다 |
| 몸 충돌체가 머리를 따라오지 않음 | 1단계는 정지 상태라 의도된 동작이다. 이동 단계에서 XRI LocomotionMediator/XRBodyTransformer로 연결한다 |
| 손목 패널 글자가 작음 | `Text`의 Font Size(0.09)와 Rect를 키운다 |
| 패널이 영어 | 한글 TMP 폰트 에셋이 아직 없다. UI 단계에서 Noto Sans KR(OFL)로 만들고 CREDITS에 기록한다 |
