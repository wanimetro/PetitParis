# PETIT PARIS — Meta Quest 3 VR

작은 쥐가 되어 파리 몽마르트르풍 골목을 지나 센강까지 가는 1인칭 VR 게임 (3~5분).
Unity 6000.3.9f1, URP 17.3, XR Interaction Toolkit 3.3.2, Input System 1.18, OpenXR (Meta Quest Support).

- 개발 가이드·테스트·빌드 순서: `docs/README.md` (작업 전에 읽는다)
- 1단계 설계(쥐 시점·캘리브레이션): `docs/STAGE1.md`
- 에셋 출처: `docs/CREDITS.md`

## 구조
- 코드와 에셋은 모두 `Assets/PetitParis/`. 씬은 에디터 메뉴 `PetitParis > 1. Sandbox / 3. Game`으로 생성한다. 씬 YAML을 직접 손으로 고치지 않는다. 씬 구성을 바꿀 때는 `Editor/PetitParisBuildKit.cs`, `PetitParisLevelBuilder.cs`를 수정한다.
- 좌표계: 월드는 실제 크기(미터). 쥐 크기는 `RatScaleConfig.trackingScale`(Rat Tracking Space 스케일)로만 표현한다. 속도·충돌체·거리 값은 모두 월드 미터로 다룬다.
- 이동 경로는 하나: `RatLocomotionController` → LocomotionMediator → XRBodyTransformer → CharacterController. XRI ContinuousMove/Gravity/Jump Provider를 추가하지 않는다. 회전은 SnapTurnProvider만 쓴다.
- 카메라 Transform은 TrackedPoseDriver만 쓴다. Roll, Pitch, Head Bob은 넣지 않는다. 순간 시점 이동(캘리브레이션, 리스폰)에는 ScreenFader 페이드를 쓴다.

## 작업 규칙
- Quest 3에 필요 없는 패키지는 추가하지 않는다 (Meta XR SDK 전체, XR Hands 등). 패키지 클래스는 설치된 버전의 API를 확인한 뒤 사용한다.
- 화면 텍스트는 한국어. 문장은 `PetitParisText`에 const로 추가한다 (한글 폰트 글자 사전 채우기에 쓰임). 폰트는 나눔고딕(OFL).
- 에셋·음원·이미지는 무료이면서 라이선스가 확인된 것만 쓰고 `docs/CREDITS.md`에 기록한다. 후보는 먼저 사용자에게 알린다.
- 사용자 PC에서 Quest Link가 안 될 수 있다. 에디터 테스트는 XR Device Simulator, 실기 테스트는 Android 빌드로 한다.
- Unity 에디터나 실기기에서 확인하지 않은 내용은 "미검증"으로 표시한다. 테스트 완료라고 주장하지 않는다.
- 변경 후에는 메뉴 `PetitParis > 4. Validate Scene`으로 점검할 수 있게 검증 항목도 함께 갱신한다.
