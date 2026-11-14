# PETIT PARIS — Meta Quest 3 VR

작은 쥐가 되어 파리 몽마르트르풍 골목을 지나 센강까지 가는 1인칭 VR 게임 (3~5분).
Unity 6000.3.9f1, URP 17.3, XR Interaction Toolkit 3.3.2, Input System 1.18, OpenXR (Meta Quest Support).

- 개발 가이드·테스트·빌드 순서: `docs/README.md` (작업 전에 읽는다)
- 1단계 설계(쥐 시점·캘리브레이션): `docs/STAGE1.md`
- 에셋 출처: `docs/CREDITS.md`
- **다음 작업과 알려진 문제: `docs/NEXT_STEPS.md` (로컬 Claude Code + Unity MCP로 이어서 작업. 먼저 읽는다)**

## 구조
- 코드와 에셋은 모두 `Assets/PetitParis/`. 씬은 에디터 메뉴 `PetitParis > 1. Sandbox / 3. Game`으로 생성한다. 씬 YAML을 직접 손으로 고치지 않는다. 씬 구성을 바꿀 때는 `Editor/PetitParisBuildKit.cs`, `PetitParisLevelBuilder.cs`를 수정한다.
- 좌표계: 월드는 실제 크기(미터). 쥐 크기는 `RatScaleConfig.trackingScale`(Rat Tracking Space 스케일)로만 표현한다. 속도·충돌체·거리 값은 모두 월드 미터로 다룬다.
- 이동 경로는 하나: `RatLocomotionController` → LocomotionMediator → XRBodyTransformer → CharacterController. XRI ContinuousMove/Gravity/Jump Provider를 추가하지 않는다. 회전은 SnapTurnProvider만 쓴다.
- 카메라 Transform은 TrackedPoseDriver만 쓴다. Roll, Pitch, Head Bob은 넣지 않는다. 순간 시점 이동(캘리브레이션, 리스폰)에는 ScreenFader 페이드를 쓴다.
  - PC 대체 모드(헤드셋 추적 없음)에서는 `RatDesktopLook`이 마우스(오른쪽 버튼 드래그)로 XR Origin Yaw와 Rat Tracking Space Pitch를 바꾼다. 카메라 자체는 여기서도 건드리지 않는다 (장치가 없어도 TrackedPoseDriver가 카메라 자세를 계속 덮어쓰기 때문). 헤드셋 추적이 들어오면 자동으로 원래대로 돌아간다.
- 외형: 해 질 녘 파리, 애니메이션 영화 같은 따뜻한 스타일라이즈드 톤 (실사 아님, Quest 성능 우선). 질감은 `Editor/PetitParisTextures.cs`가 코드로 생성, 건물·소품은 `Editor/PetitParisParisKit.cs`.
- 특정 영화 캐릭터(예: 라따뚜이의 레미)나 영화 이미지는 쓰지 않는다. 등장인물은 오리지널 디자인(웨이터, 셰프 등).

## 작업 규칙
- Quest 3에 필요 없는 패키지는 추가하지 않는다 (Meta XR SDK 전체, XR Hands 등). 패키지 클래스는 설치된 버전의 API를 확인한 뒤 사용한다.
  - 클라우드 세션에서는 Unity 패키지 서버가 막혀 있다. 대신 `https://github.com/needle-mirror/<패키지명>` 미러의 해당 버전 태그(예: `com.unity.xr.interaction.toolkit` 3.3.2)를 shallow clone해서 소스로 확인한다. 기억에 의존해 API 이름을 쓰지 않는다.
- 화면 텍스트는 한국어. 문장은 `PetitParisText`에 const로 추가한다 (한글 폰트 글자 사전 채우기에 쓰임). 폰트는 나눔고딕(OFL).
- 에셋·음원·이미지는 무료이면서 라이선스가 확인된 것만 쓰고 `docs/CREDITS.md`에 기록한다. 후보는 먼저 사용자에게 알린다.
- 로컬 세션에서는 Unity MCP로 직접 Play·스크린샷·Console을 확인하며 작업한다. 화면을 확인하지 않은 변경은 "미검증"으로 표시한다.
- 사용자 PC에서 Quest Link가 안 될 수 있다. 에디터 테스트는 XR Device Simulator, 실기 테스트는 Android 빌드로 한다.
- Unity 에디터나 실기기에서 확인하지 않은 내용은 "미검증"으로 표시한다. 테스트 완료라고 주장하지 않는다.
- 변경 후에는 메뉴 `PetitParis > 4. Validate Scene`으로 점검할 수 있게 검증 항목도 함께 갱신한다.
