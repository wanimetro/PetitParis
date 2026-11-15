# P2 박태현 — 가상 사용자 테스트 결과
> 정적 코드 분석 기반 시뮬레이션. 실제 플레이 관찰이 아님.
> 페르소나: 30세 백엔드 개발자, Quest 3 보유, Half-Life: Alyx·Moss 등 VR 경험 많음. VR 관례·경계 테스트·구조(테스트 3) 담당.
> 기준: 레포 `/home/claude/petitparis` (review/ai-qa-2026-11-15). 경로는 `Assets/PetitParis/` 기준 상대 경로(`Scripts/...`, `Editor/...`)이고, 그 밖의 파일은 레포 루트 기준이다.
> 단위: 월드 m. 체감 값 = 월드 ÷ `trackingScale` 0.1 = ×10.

## 1. 요약 (5줄 이내)
1. 이동 관례(이동 경로 하나, 스냅 30°/0.5초, 카메라를 직접 움직이지 않음, 순간 이동마다 페이드, 재중심 대응)는 VR 경험자 기준으로도 정석이다. 박태현은 끝까지 갈 가능성이 높다 [추정].
2. 대신 "VR다운 손맛"은 거의 없다. Haptic이 전혀 없고, 쥐 앞발은 보이기만 할 뿐 아무것도 할 수 없다(Interact 액션은 바인딩만 있고 읽는 코드가 없다) [코드 확인].
3. 잠재 버그 두 개: ① `RatRespawn.Died` 구독 순서에 따라 빗자루 추격 이벤트가 다시 준비되지 않을 수 있다. ② 캘리브레이션 페이드와 리스폰 페이드가 서로를 취소해서, 순간 이동이 화면이 다 어두워지기 전에 보일 수 있다 [코드 확인 / 실제 발생 여부는 확인 불가].
4. 멀미: 비네트는 질주 0.45초 동안에만 켜진다. 달리기(체감 3.6 m/s)와 널빤지에서 떨어질 때(체감 9m, 약 1.4초)는 보호 장치가 없다 [코드 확인].
5. 구조: 코드로 씬을 만드는 방식은 재현성이 좋다. 하지만 OpenXR이 manifest에 없고, 비네트가 Samples/XRI 3.3.2 경로에 묶여 있고, 문서 수치가 오래됐고, 쓰지 않는 패키지와 에셋이 있어서 처음 클론한 협업자가 Quest 빌드까지 가기 어렵다 [코드 확인].

## 2. 체험 기록 (단계별, 시간 순)
| 단계 | 페르소나의 행동·생각 | 시스템 반응 (근거 파일:행) | 태그 |
|---|---|---|---|
| 0. 시작 직후 | 헤드셋을 쓰고 의자에 앉은 채 시작. "처음에 까만 화면, 괜찮다." | 화면이 처음부터 검은 상태로 시작 → HMD 추적이 0.3초 안정되면 눈높이 0.12m로 캘리브레이션 → 0.35초 페이드 인 (`Scripts/Player/RatEyeCalibrator.cs:94-103`, `Scripts/Core/ScreenFader.cs:265,287`) | [코드 확인] |
| 0-1. 첫 시야 | "앉아 있어도 정말 바닥에 붙은 시점이네. 손은 사람 손 크기로 보이네." | Tracking Space 0.1배, 앞발 모델은 추적 공간 단위라 실제 손 크기로 보인다 (`Editor/PetitParisBuildKit.cs:286-293`) | [코드 확인] / 체감은 [추정] |
| 0-2. 조작 안내 | 안내 패널을 끝까지 읽는다. "그립이 달리기? 보통은 스틱 클릭인데." | 패널은 시작 지점 0.53m 앞, 높이 0.25m 고정. 눈(0.12m)에서 패널 중심까지 올려다보는 각도 = atan((0.25-0.12)/0.53) ≈ 13.8° (`Editor/PetitParisLevelBuilder.cs:203`). 문구 (`Scripts/Core/PetitParisText.cs:14-22`) | [코드 확인] |
| 0-3. 관례 점검 | 바로 오른쪽 스틱을 툭툭 친다. "30°, 디바운스 0.5초. 뒤로 돌기는 없네." | SnapTurn 30°, 0.5초, TurnAround 끔, 왼쪽 스틱 회전 꺼짐 (`Editor/PetitParisBuildKit.cs:384-403`, `Scripts/Core/RatComfortSettings.cs:169-172`) | [코드 확인] |
| 0-4. 메뉴 버튼 | 습관대로 왼쪽 ≡를 짧게 누른다. 아무 일도 없다. 1초 넘게 누르면 화면이 0.2초 어두워졌다 밝아진다. "일시정지 메뉴가 아니라 재정렬이구나." | Calibrate = 왼쪽 Menu 또는 Y를 1초 길게 (`Assets/PetitParis/Settings/PetitParisInput.inputactions` Calibrate 바인딩 2개, Hold duration=1). 일시정지·재시작 메뉴는 없음 (`Scripts/Gameplay/PetitParisGameFlow.cs:105-133`) | [코드 확인] |
| 0-5. Meta 버튼 재중심 | Quest 재중심(Meta 길게)을 해 본다. | `trackingOriginUpdated` 이벤트 → 0.5초 안에 다시 들어오는 이벤트는 무시 → 페이드를 끼고 다시 캘리브레이션 (`Scripts/Player/RatEyeCalibrator.cs:206-228`). 시작 페이드가 끝난 뒤에 구독한다 (`:105`) | [코드 확인] / Quest에서 이벤트가 실제로 오는지는 [확인 불가] |
| 0-6. 손으로 만져 보기 | 앞발로 벽과 빵 부스러기를 집으려고 그립과 트리거를 눌러 본다. "잡히는 게 없네. 트리거를 눌렀더니 질주가 나간다." | 그립은 달리기, 오른쪽 트리거는 질주, 왼쪽 트리거는 Interact인데 읽는 코드가 없다 (`Scripts/Input/RatInputReader.cs:67,74`. 사용처는 디버그 패널 `Scripts/Diagnostics/RatDebugPanel.cs:92`뿐). Interactor 없음. 부스러기는 몸이 닿으면 자동으로 줍는다 (`Scripts/Gameplay/CrumbCollection.cs:92-108`) | [코드 확인] |
| 0-7. Haptic 확인 | 벽에 부딪히고 질주해 본다. "진동이 하나도 없네." | `SendHapticImpulse` 같은 호출이 없다 (Scripts 전체 grep 결과 0건) | [코드 확인] |
| 1. 경계 테스트: 벽 틈 | 시작 지점 뒤 벽 틈(폭 0.4m 영역, 체감 4m)에 들어갔다 나온다. | 쥐 지름 0.07m. 틈은 x -3.4~-3.0, 천장 0.25m (`Editor/PetitParisLevelBuilder.cs:303-308`). 끼일 이유가 없다. 안전 구역이라 손목 패널에 "안전 · 벽 틈"이 뜬다 | [코드 확인] |
| 1-1. 테라스 | 화살표를 따라가다 웨이터 A를 보고 일부러 정면에 서 본다. "!" 소리가 나고 0.9초 뒤 쿵쿵 다가온다. 달리기(그립)로 도망친다. | 발견: 반경 1.0m, 시야 ±65°, 경고음, HUD 메시지 (`Scripts/Gameplay/PedestrianAwareness.cs:155-183`). 추격 0.55 m/s, 3.5초 (`:43-44`). 계산은 7절 | [코드 확인] / 결과는 [추정] |
| 1-2. 말풍선 | 머리 위 "!"를 찾으려고 고개를 든다. "목이 아프네. 아래쪽 HUD에 같은 글이 있네." | 말풍선 높이 2.15m. 0.7m 거리에서 올려다보는 각도 = atan((2.15-0.12)/0.7) ≈ 71° (`Editor/PetitParisGameplayKit.cs:130`). 같은 내용이 HUD에도 표시됨 | [코드 확인] |
| 2. 빵집 | 힌트("발 사이를 질주로 빠져나가요")를 읽고 질주 + 스틱으로 셰프 B 발 사이를 지나간다. 바게트 그림자를 보고 피한다. | 질주는 스틱 0.3 이상 + 지면 위일 때만 (`Scripts/Player/RatDashController.cs:91-110`). 바게트 경고 그림자, 간격 2.6초 (NEXT_STEPS 1-2) | [코드 확인] / 성공 여부 [추정] |
| 2-1. 질주 연타 | 트리거를 연타한다. "쿨다운이 손목에 숫자로 나오네." | 쿨다운 1.2초 동안 입력 무시. 손목 패널에 "질주 대기 n.n초" (`Scripts/UI/RatStatusPanel.cs:73-77`) | [코드 확인] |
| 2-2. 질주 중 벽 | 벽을 보고 질주한다. 체감 5 m/s에서 0으로 즉시 멈춘다. "튕기지 않는 건 좋은데 탁 멈추네." | CapsuleCast로 정면 60° 안의 벽을 감지 → `Stop(true)`, `planarVelocity = 0` (`Scripts/Player/RatLocomotionController.cs:177-181,258-285`) | [코드 확인] |
| 3. 골목: 자전거 | 벨 소리를 듣고 자전거가 끝에 섰을 때 건넌다. | 2.2 m/s, 양 끝에서 2.5초 정지, 가로 골목 폭 1.2m (`Scripts/Gameplay/BicycleHazard.cs:20-25`, `Editor/PetitParisLevelBuilder.cs:359-360`) | [코드 확인] / 성공 [추정] |
| 3-1. 빗자루 추격 | z 18.6에서 음악과 "웨이터가 빗자루를 들고 쫓아와요!"가 나온다. 화살표는 계속 북쪽(웨이터 쪽)을 가리킨다. 0.6m 안에 들어와야 보이는 "팔레트 밑에 숨어요!" 표지를 보고 팔레트로 들어간다. | 경로 노드 (-0.5,18.2)→(-0.5,21.0)는 빗자루 웨이터 경로(z 21.7~22.1)를 지난다 (`Editor/PetitParisLevelBuilder.cs:218-219,75-77`). 화살표는 추격과 위험을 고려하지 않는다 (`Scripts/UI/RatGuideArrow.cs` 전체에 danger/chase 참조 없음) | [코드 확인] |
| 3-2. 일부러 죽어 보기 | 개발자 습관으로 빗자루에 일부러 맞아 본다. 체크포인트(CP3)로 돌아가 다시 트리거에 들어간다. | 추격 이벤트가 다시 준비되는지는 `Died` 구독 순서에 달려 있다 (5-4절) | [코드 확인] / 실제 결과 [확인 불가] |
| 3-3. 캘리브레이션 중 사망 | 추격 중 Y를 1초 누른다(실수 또는 테스트). | 캘리브레이션 페이드 중에도 입력과 위험 판정이 계속된다 → 이때 죽으면 두 페이드가 서로를 취소한다 (5-5절) | [코드 확인] |
| 4. 센강 | 난간 틈으로 강에 떨어져 보려고 한다. | 난간 0.45m, 틈 x -1.0~0.0에는 보이지 않는 벽 0.4m. 양옆 벽 1.0m (`Editor/PetitParisLevelBuilder.cs:376-381`). 점프 높이 0.035m로는 못 넘는다 → killHeight -1에 닿을 길이 없다 | [코드 확인] |
| 5. 주방 입구 | 뒷문으로 들어가자마자 요리사 통로(z 22.2)다. 요리사가 동쪽 끝(x -2.3)에서 1.2초 멈출 때 널빤지 쪽으로 간다. | 문 x -3.4~-2.5, 통로 z 22.2, 요리사 발견 1.1m, 추격은 z 21.8~22.6 안에서만 (`Editor/PetitParisLevelBuilder.cs:84-94,577`) | [코드 확인] / 타이밍 [추정] |
| 5-1. 널빤지 | 폭 0.15m(체감 1.5m), 난간 없는 21° 경사를 오른다. 고개를 돌리며 걷다가 마지막 널빤지(0.6→0.9m)에서 옆으로 떨어진다. | 경사 = atan(0.3/0.8) ≈ 20.6°, 폭 0.15 (`Editor/PetitParisLevelBuilder.cs:566-570,691-692`). 이동 방향 = 카메라 Yaw (`Scripts/Player/RatLocomotionController.cs:244-251`). 낙하 피해 없음, 공중 비네트 꺼짐 (`Scripts/Core/RatComfortSettings.cs:186`). 낙하 시간 계산은 6절 | [코드 확인] / 떨어지는지는 [추정] |
| 5-2. 다시 오르기 | 바닥부터 다시 오른다. "죽지는 않는데 처음부터네." | 실패 처리 없이 바닥에 착지. 체크포인트로 이동하지 않는다 | [코드 확인] |
| 6. 치즈 | 도마를 건너 식탁 위 치즈에 닿는다. 완료 패널이 눈앞 0.3m(체감 3m)에 뜬다. | 도착 구역 1.0×0.2×1.0m (`Editor/PetitParisLevelBuilder.cs:114-119`). 패널은 깊이 검사 없이 그린다 (`Scripts/Gameplay/PetitParisGameFlow.cs:166-175`) | [코드 확인] |
| 7. 재시작 | 1.5초 뒤 A를 눌러 처음부터 다시 한다. "중간에 재시작할 방법은 없네." | 완료 상태에서만 A로 씬을 다시 불러온다 (`Scripts/Gameplay/PetitParisGameFlow.cs:128-131,201-206`) | [코드 확인] |

## 3. 테스트 1 결과: 사용자 흐름
| 질문 | 판정 | 근거 |
|---|---|---|
| 시작 방법을 찾는가 | 예 | 시작하면 바로 조작할 수 있고, 안내 패널이 정면 13.8° 위에 있다 (`Editor/PetitParisLevelBuilder.cs:203`). 0.35m 움직이거나 30초가 지나면 사라진다 (`Scripts/Gameplay/PetitParisGameFlow.cs:41-42,114-118`). 다시 볼 방법은 없지만 박태현은 한 번 읽으면 기억한다 [추정] |
| 상호작용 대상을 발견하는가 | 부분 | 부스러기·체크포인트·힌트·화살표는 잘 보인다. 하지만 "손으로 상호작용할 대상"이 하나도 없어서, 박태현은 처음에 그립·트리거로 집으려다 의도와 다른 동작(달리기·질주)을 하게 된다 [코드 확인: Interact를 읽는 코드 없음] |
| 어떤 입력을 해야 하는지 아는가 | 예 | 안내 패널에 모든 입력이 나온다 (`Scripts/Core/PetitParisText.cs:16-21`). 단 "Y 길게 = 재정렬"은 빠져 있다(바인딩은 있음). 그립 = 달리기는 관례와 달라 한 번은 헷갈린다 [추정] |
| 현재 상태를 인식하는가 | 부분 | 손목 패널(안전/위험/질주 대기/웅크림/일어설 수 없음)은 정보가 충분하다 (`Scripts/UI/RatStatusPanel.cs:61-85`). 하지만 손목을 봐야만 보이고, Haptic이 없어서 위험 신호는 소리와 그림자뿐이다 |
| 성공·실패를 구분하는가 | 예 | 실패하면 실패음 + 0.25초 페이드 + HUD 메시지, 체크포인트에서는 차임, 도착하면 완료 패널. 메시지가 한 칸이라 죽을 때 어떤 문구가 보일지는 구독 순서에 따라 달라진다 (8절 #15) |
| 다음 행동을 스스로 정하는가 | 부분 | 화살표·목표 마름모·힌트가 있다. 하지만 빗자루 추격 중에는 화살표가 웨이터 쪽을 가리키고, 숨을 곳 표지는 0.6m 안에 들어가야 보인다 (`Scripts/UI/RatHintSign.cs:17-20,38`) |
| 종료 방법을 찾는가 | 부분 | 완료 후 "A 버튼 처음부터 다시"는 명확하다. 게임 중 일시정지·재시작·종료 메뉴는 없고, 왼쪽 메뉴 버튼은 재정렬에 쓰인다. Quest 시스템 메뉴로 나가야 한다 |

## 4. 테스트 2 결과: 기능·예외 상황
| 항목 | 코드상 결과 | 문제 여부 | 근거 | 태그 |
|---|---|---|---|---|
| 질주 연타 | Ready 상태에서 그 프레임에 눌렸을 때만 시작. 쿨다운 1.2초 동안 무시 | 없음 | `Scripts/Player/RatDashController.cs:71-88` | [코드 확인] |
| 점프 연타 | 버퍼 0.12초, 쿨다운 0.25초, 공중에서는 다시 점프 불가 | 없음 | `Scripts/Player/RatJumpController.cs:191-208` | [코드 확인] |
| 웅크리기 연타 | 누를 때마다 Toggle. 일어설 공간이 없으면 `wantsToStand`가 되고 공간이 생기면 자동으로 일어선다. 대기 중에 B를 다시 눌러도 "계속 웅크리기"로 취소할 수 없다 | 경미 | `Scripts/Player/RatCrouchController.cs:281-310` | [코드 확인] |
| 웅크린 채 질주 | 일어설 공간이 있으면 자동으로 일어서서 질주. 없으면 아무 피드백 없이 질주가 안 나간다 | 경미 | `Scripts/Player/RatDashController.cs:97-100` | [코드 확인] |
| 웅크린 채 점프 | 무시. 피드백 없음 | 경미 | `Scripts/Player/RatJumpController.cs:197` | [코드 확인] |
| 추격 중 웅크리기 | 웅크리기는 발견(Idle → Noticed)에만 영향(반경 ×0.45). 추격 중에는 효과가 없고, 오히려 속도가 0.5배(0.1 m/s)가 되고 달리기가 꺼져 잡힌다 | 있음 (UX) | `Scripts/Gameplay/PedestrianAwareness.cs:155-164`, `Scripts/Player/RatLocomotionController.cs:164-166` | [코드 확인] |
| 리스폰 중 입력 | 스틱·질주·점프는 잠김. 점프는 `!inputLocked &&` 조건 때문에 버퍼도 쌓이지 않는다. 웅크리기 Toggle과 스냅 회전은 계속 동작한다 → 페이드 인 뒤 웅크린 상태이거나 방향이 체크포인트 방향과 다를 수 있다 | 경미 | `Scripts/Gameplay/RatRespawn.cs:83-107`, `Scripts/Player/RatLocomotionController.cs:213`, `Scripts/Player/RatCrouchController.cs:277-297` | [코드 확인] |
| 캘리브레이션 중 사망 | 캘리브레이션은 입력과 위험 판정을 잠그지 않는다. 사망하면 `ScreenFader.Fade`의 version 비교 때문에 서로의 페이드가 일찍 끝난다 → 화면이 덜 어두운 상태에서 순간 이동할 수 있다 (5-5절) | **있음** | `Scripts/Core/ScreenFader.cs:327-347`, `Scripts/Player/RatEyeCalibrator.cs:142-150`, `Scripts/Gameplay/RatRespawn.cs:85-109` | [코드 확인] / 체감 [추정] |
| 리스폰 중 캘리브레이션 (Y·메뉴 길게, 재중심) | `Calibrate()`는 자기 `busy`만 확인하고 `respawn.IsRespawning`은 확인하지 않는다 → 위와 같은 페이드 충돌 | **있음** | `Scripts/Player/RatEyeCalibrator.cs:136-140` | [코드 확인] |
| 뒤에서 오는 추격자 | 발소리 3D(0.3~10m). Unity 기본 패닝이라 앞뒤 구분이 약하다(문서에 명시). 시각 경고는 손목 패널 "위험!"과 HUD 메시지뿐. Haptic 없음 | 있음 (UX) | `docs/README.md:47-48`, `Scripts/UI/RatStatusPanel.cs:66-67` | [코드 확인] |
| 머리 위 말풍선 | 0.7m 거리에서 올려다보는 각도 71°. 같은 내용을 HUD에 표시해 보완 | 경미 | `Editor/PetitParisGameplayKit.cs:130` | [코드 확인] |
| 재시작 | 완료 후 A만 가능(1.5초 뒤). 씬을 다시 불러온다. 정적 리스트는 OnDisable에서 제거하고, Domain Reload는 켜져 있다(`EnterPlayModeOptions` 0) | 없음 | `Scripts/Gameplay/PetitParisGameFlow.cs:128-131`, `Scripts/Gameplay/FootstepHazard.cs:47-55`, `ProjectSettings/EditorSettings.asset:27-28` | [코드 확인] |
| 트래킹 중단 | TrackedPoseDriver `IgnoreTrackingState=false` → 마지막 자세를 유지. 게임 로직은 계속 진행. 재중심 이벤트가 오면 다시 캘리브레이션 | 경미 | `Editor/PetitParisBuildKit.cs:371-380` | [코드 확인] / Quest 동작은 [확인 불가] |
| 컨트롤러 연결 해제 | 스틱이 0이 되어 멈춘다. 앞발은 마지막 위치에 굳는다. 알림·일시정지 없음. 보행자는 계속 움직여서 밟힐 수 있다 | 있음 | `Scripts/Input/RatInputReader.cs:139-149` (값만 읽음, 연결 상태를 확인하지 않음) | [코드 확인] |
| Quest 시스템 메뉴(포커스 잃음) | `OnApplicationFocus/Pause` 처리와 `timeScale` 변경이 없다 → 메뉴를 연 동안 게임이 계속 진행될 수 있다 | 있음 | Scripts 전체 grep 결과 0건 | [코드 확인] / 실제 Quest 동작은 [추정] |
| 동일 기능 중복 실행 | 캘리브레이션 중복 → `busy`로 막음. Kill 중복 → `IsRespawning`, 무적 1.5초로 막음. 재시작 중복 → `restarting`. 추격 이벤트 중복 → `armed` | 없음 (페이드 충돌 제외) | `Scripts/Player/RatEyeCalibrator.cs:138`, `Scripts/Gameplay/RatRespawn.cs:72`, `Scripts/Gameplay/PetitParisGameFlow.cs:129` | [코드 확인] |
| 추격자가 이미 쫓는 중에 이벤트 시작 | `ForceChase`는 `IsHunting`이면 무시. 이벤트는 음악을 켜지만 추격 시간은 9초가 아니라 원래 3.5초 | 경미 | `Scripts/Gameplay/PedestrianAwareness.cs:167-171`, `Scripts/Gameplay/ChaseEvent.cs:212-224` | [코드 확인] |

## 5. 페르소나별 추가 항목 결과

### 5-1. VR 관례 점검
| 항목 | 현재 | 경험자 평가 | 태그 |
|---|---|---|---|
| 이동 | 왼쪽 스틱, 데드존 0.15, Head Yaw 기준 (`Scripts/Player/RatLocomotionController.cs:38,244-251`) | 관례대로. 컨트롤러 기준 이동 옵션은 없음 | [코드 확인] |
| 달리기 = 왼손 그립 (누르고 있기) | 보통은 왼쪽 스틱 클릭(Sprint)이다. 그립은 대부분 게임에서 "잡기"라서 처음에 반사적으로 눌렀다가 달리게 된다. 스틱 클릭은 비어 있다 | 관례와 다름 (견딜 만함) | [추정] |
| 질주 = 오른쪽 트리거 | 트리거는 보통 "사용·발사"라 의미가 맞지 않지만, 상호작용이 없는 게임이라 충돌은 없다 | 괜찮음 | [추정] |
| 점프 A / 웅크리기 B | A 점프는 표준. B 웅크리기 Toggle도 흔한 방식 | 좋음 | [코드 확인] |
| 캘리브레이션 = 왼쪽 메뉴 길게 (+ Y 길게) | 왼쪽 ≡는 대부분 게임에서 일시정지 메뉴다. Y 길게 바인딩은 안내 패널에 없다 → Y를 누른 채 있다가 갑자기 페이드가 일어날 수 있다 | 관례와 다름 | [코드 확인] |
| 스냅 회전 30° / 0.5초 | 표준값. Smooth Turn·각도 변경은 게임 안에서 고를 수 없다 (에셋 `RatComfortSettings`로만) | 좋음 (옵션 부족) | [코드 확인] |
| 비네트 적용 범위 | 질주 중에만 (`Scripts/Core/RatComfortSettings.cs:184-186`, `Scripts/Player/RatComfortApplier.cs:242-249`). 걷기·달리기(체감 3.6 m/s)·공중(낙하 체감 9m)에는 없음. 게다가 프리팹이 XRI 3.3.2 샘플에 의존 (`Editor/PetitParisPaths.cs:27-28`). 샘플이 없으면 WARN만 남기고 비네트 없이 빌드된다 (`Editor/PetitParisBuildKit.cs:408-418`, `Editor/PetitParisValidator.cs:128`) | 부족 | [코드 확인] |
| Quest 재중심 | `XRInputSubsystem.trackingOriginUpdated` 구독, 0.5초 디바운스, 페이드를 끼고 눈높이·Yaw 재정렬 (`Scripts/Player/RatEyeCalibrator.cs:206-228`) | 잘 되어 있음. 리스폰과 겹치는 경우만 문제 | [코드 확인] |
| 손(쥐 앞발)이 할 수 있는 일 | 없음. 보이기만 하고, 왼손 손목 상태 패널을 들고 다닌다 (`Editor/PetitParisBuildKit.cs:186-191`). Interact(왼쪽 트리거)는 바인딩만 있다 | 아쉬움 큼 (Moss와 비교하면 특히) | [코드 확인] |
| Haptic | 없음 | 아쉬움 큼. 발이 내려올 때, 들켰을 때, 질주가 벽에 막혔을 때, 부스러기를 주웠을 때 짧은 진동만 있어도 앞뒤 구분이 약한 소리를 보완한다 | [코드 확인] |
| 디버그 패널 | 게임 씬은 `BuildRig(..., false)`로 숨긴다 (`Editor/PetitParisLevelBuilder.cs:245`, `Editor/PetitParisBuildKit.cs:263-265`) | 좋음 | [코드 확인] |

### 5-2. 경계 테스트
| 시도 | 코드상 결과 | 태그 |
|---|---|---|
| 벽 틈·좁은 틈에 끼기 | 쥐 반지름 0.035(웅크리면 0.0245), skin 0.0035. 확인한 틈(벽 틈 0.4m, 의자 다리 사이 약 0.33m, 팔레트 블록 사이 0.2m, 팔레트·조리대 밑 높이 0.16m 대 몸 높이 0.13m) 중 끼일 만큼 좁은 곳은 없다. 질주 판정은 `stepOffset`만큼 아래를 빼고 캡슐을 쏘므로 낮은 턱에서 질주가 멈추지 않는다 (`Scripts/Player/RatLocomotionController.cs:268`) | [코드 확인] (모든 틈을 확인하지는 않음) |
| 웅크린 채 일어설 수 없는 상황 | 폭 0.049~0.07m인 틈에서만 생긴다. 레벨 빌더 샘플에서 그런 틈은 찾지 못했다. 생기면 손목 패널에 "좁아서 일어설 수 없어요", 공간이 생기면 자동으로 일어선다 (`Scripts/UI/RatStatusPanel.cs:80-81`) | [코드 확인] / 실제 틈 존재 여부 [확인 불가] |
| 질주 중 벽 | 정면 60° 안이면 즉시 정지(0.5 → 0 m/s, 한 프레임). 스치는 벽이면 계속 가다가 실제 속도가 명령 속도의 25% 밑으로 떨어지면 정지. 튕기지 않고 카메라도 밀리지 않는다 | [코드 확인] |
| 점프로 못 오르는 곳 오르기 | 점프 0.035m + stepOffset 0.02m → 오를 수 있는 턱은 약 5cm까지로 추정. 상자 0.3m, 팔레트 0.19m, 난간 0.45m는 불가. 테라스 경계석은 0.015m라 걸어서 넘는다 (`Editor/PetitParisLevelBuilder.cs:290`). 읽은 범위에서는 **점프가 꼭 필요한 지형을 찾지 못했다** → 점프는 공략 요소라기보다 장식에 가깝다 | [코드 확인] (샘플링 범위) / [추정] |
| 맵 밖으로 떨어지기 (killHeight -1) | 강물(위 면 -0.9, 충돌체 없음) 아래로 떨어지면 판정되지만, 난간과 보이지 않는 벽(0.4m)과 양옆 1.0m 벽이 막는다 (`Editor/PetitParisLevelBuilder.cs:376-386`). 정상 플레이로는 닿지 않는다. 판정 기준은 XR Origin Y (`Scripts/Gameplay/RatRespawn.cs:63-67`) | [코드 확인] / 다른 가장자리는 [확인 불가] |
| 주방 널빤지 위 | 폭 0.15m(체감 1.5m), 난간 없음, 경사 20.6°. slopeLimit 50°라 오를 수 있다(NEXT_STEPS 1-2에서 `CharacterController.Move`로 확인됨). 옆으로 떨어지면 낙하 피해 없이 바닥(최대 0.9m 아래)에 내려온다. 첫 널빤지 아래쪽(x -2.15, z 21.5)은 요리사 추격 범위 끝(z 21.8)에서 0.3m라, 발을 0.2m 내딛고 보폭만큼 앞서 디디면 닿을 수 있다 (`Scripts/Gameplay/PedestrianPath.cs:369-375`) | [코드 확인] / 닿는지는 [추정] |

### 5-3. 테스트 3: 구조·협업 (개발자 시각)
**Script 역할 명확성 — 좋음**
- 런타임 36개 파일이 Core / Input / Player / Gameplay / UI / Diagnostics로 나뉘어 있고, 대부분 100~300줄이다. 이동은 `RatLocomotionController` 하나가 합산하고, Dash/Jump/Crouch는 상태만 계산한다. 주석에 책임과 금지 사항(다른 Provider 추가 금지 등)이 적혀 있다 [코드 확인].
- 반면 에디터 쪽 `PetitParisBuildKit.cs`(1,124줄)에는 리그 생성·카페 소품·폰트·오버레이·대화상자 자동화가 함께 들어 있다. `PetitParisLevelBuilder.cs`(727줄)는 좌표 매직 넘버로 가득하다. 체크포인트 좌표를 경로 노드에 다시 손으로 적었다(`Editor/PetitParisLevelBuilder.cs:99-103`와 `:215-219`의 (-1.5,0,6.08), (-2.7,0,10.0), (-1.35,0,15.0), (-1.9,0,23.9)) → 한쪽만 고치면 화살표가 어긋난다 [코드 확인].
- 참조 연결이 `[SerializeField]` + `FindAnyObjectByType`/`GetComponent` 대체 방식으로 섞여 있다. 빌더가 연결하니 동작은 하지만, 손으로 씬을 고치는 사람은 어떤 것이 필수인지 알기 어렵다. Validator가 일부를 보완한다 [코드 확인].

**불필요하거나 위험한 Script·Asset·Package**
| 항목 | 상태 | 판단 | 근거 |
|---|---|---|---|
| `Assets/Samples/XR Interaction Toolkit/3.3.2` (11MB) | XRI 3.6.1과 버전이 다르다. **TunnelingVignette 프리팹 때문에 지울 수 없다** (경로가 코드에 고정됨) | 3.6.1 샘플로 다시 임포트하고 경로 상수를 바꾸거나, 비네트 프리팹을 `Assets/PetitParis/`로 복사해 독립시킨다 | `Editor/PetitParisPaths.cs:27-28` |
| `Assets/InputSystem_Actions.inputactions` | **프로젝트 전역 액션으로 등록되어 있다** (`EditorBuildSettings` `com.unity.input.settings.actions` → 이 에셋 GUID 052faaac…). 전역 액션은 런타임에 자동으로 켜진다. 코드 주석은 "읽지 않는다"고 한다 | 읽지 않으니 동작 충돌은 없겠지만 혼란의 원인이다. 전역 액션 등록을 해제하거나 `PetitParisInput`을 전역으로 지정한다 | `ProjectSettings/EditorBuildSettings.asset`, `Assets/InputSystem_Actions.inputactions.meta:2`, `Scripts/Input/RatInputReader.cs:12` |
| `com.unity.visualscripting`, `com.unity.collab-proxy`, `com.unity.multiplayer.center` | 사용처 없음 (기본 템플릿) | 제거 후보. CLAUDE.md의 "Quest 3에 필요 없는 패키지는 추가하지 않는다" 원칙과도 맞는다 | `Packages/manifest.json` |
| `com.unity.test-framework` | 테스트 코드 없음 | 남겨 두고 EditMode 테스트(입력 액션 이름, Validator 핵심 항목)를 추가하는 편이 낫다 | — |
| `com.coplaydev.unity-mcp` (git URL) | 태그·커밋 고정 없음. `packages-lock.json`에도 hash가 없다 → PC마다 다른 버전이 설치될 수 있다 | `#v<버전>` 또는 커밋 해시로 고정 | `Packages/manifest.json:3`, `Packages/packages-lock.json:3-7` |
| **OpenXR / XR Management** | **manifest에 없다.** 문서에 따르면 각자 XR Plug-in Management에서 체크해 설치하고, Interaction Profile을 직접 추가해야 한다. `ProjectSettings/XRSettings.asset`에도 로더 설정이 없다 | Quest 빌드 설정이 레포에 담기지 않아 협업자마다 결과가 다를 수 있다. 설치하고 설정 파일까지 커밋할 것을 권한다 | `docs/README.md:85-87,176` |
| 쓰지 않는 코드 | `PedestrianAwareness.AnyNoticed` static event (구독자 없음), `Rat/Interact` 액션, `PetitParisText.HintGoal`("센강 쪽 출구", 사용처 없음) | 정리 | `Scripts/Gameplay/PedestrianAwareness.cs:73`, `Scripts/Core/PetitParisText.cs:28` |

**Scene/Prefab 의존성 (코드로 씬 생성)**
- 장점: 씬 YAML 충돌이 없고, 누구나 메뉴 한 번으로 같은 씬을 얻는다. 리뷰는 C# diff로 하면 된다. Validator와 짝을 이룬다 [코드 확인].
- 단점: ① 레벨을 조정할 때마다 코드 수정 → 재생성(재생성하면 손으로 고친 것은 사라진다). ② 디자이너나 아트 담당이 씬 뷰에서 바로 고칠 수 없다. ③ NEXT_STEPS 3·4절의 에셋 교체 계획(프리팹 배치)과 부딪힌다. ④ `EditorBuildSettings`가 레포에 없는 씬(GUID 17b35d6b…)을 가리켜서, 메뉴 3을 실행하기 전에는 빌드 목록이 깨져 있다 [코드 확인]. ⑤ Prefab도 생성물(`Prefabs/` 폴더 없음)이라 리그 구조가 코드에만 있다.
- 제안: 좌표·경로·체크포인트를 ScriptableObject(LevelLayout) 하나로 빼서, 빌더는 그 데이터를 읽어 배치만 하게 한다. 에셋을 교체할 즈음 "생성 후 손으로 편집하는 씬"으로 넘어갈 시점을 정한다.

**명명 규칙**
- 대체로 일관적이다: `Rat*`(플레이어), `PetitParis*`(전역·에디터), `Pedestrian*`, 네임스페이스 `PetitParis` / `PetitParis.EditorTools`, asmdef 2개 [코드 확인].
- 예외: `ChaseEvent`, `BroomSweep`, `CrumbPickup`, `ScreenFader`, `XROriginGuard`처럼 접두어 없는 일반 이름이 섞여 있다(충돌 위험은 네임스페이스로 해결됨). 문제는 크지 않다.
- CLAUDE.md 규칙 위반: 화면에 나오는 한국어 일부가 `PetitParisText` 밖에 있다. 안전 구역 이름("벽 틈", "종이 상자 안", "벤치 아래" 등)은 손목 패널에 표시되고, "바게트"·"빗자루"는 위험물 이름이다 (`Editor/PetitParisLevelBuilder.cs:308,491`, `Editor/PetitParisBuildKit.cs:505`, `Editor/PetitParisGameplayKit.cs:171,263`, `Scripts/Gameplay/FallingHazard.cs:266`). 이 중 '벽·틈·종·벤'은 폰트를 미리 채울 때 쓰는 글자 목록(`PetitParisText.AllCharacters`)에 없다 → 처음 표시될 때 Dynamic 아틀라스에 런타임으로 추가된다(끊김 가능) [코드 확인 / 끊김은 추정].

**README·docs와 코드 불일치**
| 문서 | 내용 | 실제 |
|---|---|---|
| `README.md` (루트) | 목적지 "센강 쪽 출구", 조작표에 달리기(왼손 그립) 없음 | 주방 치즈, 달리기 있음 |
| `docs/README.md:110` | 걷기 0.15, 질주 0.45 m/s | 0.2 / 0.55 (`Scripts/Core/RatScaleConfig.cs:303,306`) |
| `docs/README.md:133` | "Speed가 걷기 15 cm/s를 넘지 않음" | 20 cm/s, 달리기를 쓰면 36 cm/s |
| `Scripts/Core/RatScaleConfig.cs:302` 툴팁 | "0.15 = 체감 1.5 m/s" | 기본값 0.2 |
| `Scripts/Player/RatLocomotionController.cs:73`, `Editor/PetitParisBuildKit.cs:383` 주석 | "XRI 3.3.2" 기준 | 설치된 버전 3.6.1 (`docs/README.md:175`도 API 확인이 이전 버전 기준이라고 명시) |
| 안내 패널 | 캘리브레이션 = 왼쪽 메뉴 길게 | Y 길게도 동작 (`docs/README.md:34`에는 있음) |

**Build 절차**
- 문서(`docs/README.md` 2절)에 순서가 있다: OpenXR 체크 → Interaction Profile → 메뉴 0 → Fix All → (선택) 메뉴 5 → 메뉴 1·3 → 메뉴 4. 단계가 많고 일부(OpenXR, 메뉴 5 URP 최적화)는 레포 밖 상태에 의존하거나 선택 사항이라, 빠뜨려도 빌드는 성공해서 실수를 알아채기 어렵다 [코드 확인]. Validator가 빌드 타깃·Vulkan·Min API·씬 목록은 확인하지만 OpenXR 로더와 URP 최적화 적용 여부는 확인하지 않는다 (`Editor/PetitParisValidator.cs:247-260` 부근).
- 제안: "Quest 빌드 준비" 메뉴 하나로 0·5·3·4를 순서대로 실행하고, OpenXR 로더·Interaction Profile 확인을 Validator에 추가한다.

**협업 유지보수성**
- 좋음: 문서가 자세하고, "미검증" 표기가 정직하고, Validator가 중복 Provider·참조 누락·시작 시야를 점검한다.
- 위험: 자동 테스트가 없다. 이벤트 구독 순서 같은 런타임 버그는 Validator로 잡을 수 없다(5-4절). 레벨 데이터가 코드에 흩어져 있다. 패키지 고정이 안 되어 있다.

### 5-4. 이벤트 구독 순서 의존성 (ChaseEvent · PedestrianAwareness · RatRespawn.Died)
`RatRespawn.Died`를 구독하는 곳: `ChaseEvent.OnDied`(`Scripts/Gameplay/ChaseEvent.cs:189`), `PedestrianAwareness.OnRatDied`(`Scripts/Gameplay/PedestrianAwareness.cs:95`), `PetitParisGameFlow.OnDied`(`:86`), `CrumbCollection.OnDied`(`:83`), `FallingHazard.OnRatDied`(`:117`). 모두 각자의 `OnEnable`에서 구독하므로, 호출 순서는 씬 로드 때 오브젝트가 활성화되는 순서를 따른다. Unity는 이 순서를 보장하지 않는다.

**버그 경로 (빗자루 추격 중 사망)** [코드 확인]
1. `RespawnRoutine` → `Died?.Invoke(reason)` (`Scripts/Gameplay/RatRespawn.cs:81`)
2. **PedestrianAwareness가 먼저 호출되면**: 상태가 Chasing → `StopChase(false)` → `ChaseEnded?.Invoke()` (`Scripts/Gameplay/PedestrianAwareness.cs:103-108,197-210`)
3. → `ChaseEvent.OnChaseEnded`: `running = false`. 이때 `armed`는 그대로 false (`Scripts/Gameplay/ChaseEvent.cs:226-232`)
4. → 이어서 `ChaseEvent.OnDied`: `if (!running) return;` 때문에 바로 빠져나간다 → **`armed = true`가 실행되지 않는다** (`:234-241`)
5. 결과: 다시 도전해도 빗자루 추격 이벤트가 시작되지 않는다(긴장 음악·메시지 없음). 게임 진행은 막히지 않지만, 설계 의도("죽으면 다시 준비")가 깨진다.

**ChaseEvent가 먼저 호출되면** 의도대로 다시 준비된다. 빌더는 `Pedestrians` 루트를 `Gameplay/Events`보다 먼저 만든다 (`Editor/PetitParisLevelBuilder.cs:64,96,155`). 활성화 순서가 생성 순서를 따른다면 버그 경로를 탈 가능성이 높다. 하지만 NEXT_STEPS 1-2에는 "죽으면 다시 준비 — 동작"이라고 되어 있으므로, 그 Play에서는 순서가 유리했을 수 있다 → **실제 순서는 [확인 불가]. 순서에 따라 결과가 달라지는 취약한 구조라는 점은 [코드 확인]**.

**부수 문제** [코드 확인]: HUD 메시지가 한 칸(`PostMessage`)이라, 사망할 때 GameFlow("밟힐 뻔했어요")와 CrumbCollection("빵 부스러기 n개를 떨어뜨렸어요") 중 나중에 호출된 쪽만 남는다. FallingHazard는 `Kill()` 뒤에 자기 메시지를 다시 써서 항상 이긴다 (`Scripts/Gameplay/FallingHazard.cs:266-267`).

**제안**
- `ChaseEvent.OnChaseEnded`에서 `respawn.IsRespawning`이면 `armed = true`로 처리한다. 또는 `ChaseEnded`에 이유(Lost / RatDied / Timeout)를 넘긴다.
- 또는 `RatRespawn`이 `Died`를 부르기 전에 이벤트 매니저 쪽 처리를 먼저 하도록 순서를 명시한다(예: `DiedEarly`/`Died` 두 단계, `[DefaultExecutionOrder]`만으로는 구독 순서가 보장되지 않음).
- 메시지는 우선순위 큐로 바꾸거나, 사망 메시지 하나로 합친다("밟혔어요 · 부스러기 3개 잃음").

### 5-5. 페이드 경쟁 (캘리브레이션 ↔ 리스폰)
`ScreenFader.Fade`는 새 페이드가 시작되면 이전 코루틴을 `yield break`로 끝낸다 (`Scripts/Core/ScreenFader.cs:329,340`). 호출한 쪽은 그것이 "페이드 완료"인지 "취소"인지 구분하지 못한다.
- **캘리브레이션 페이드 아웃 중 사망**: Respawn `Fade(1, 0.25)` 시작 → Calibrate의 페이드가 취소됨 → Calibrate는 바로 `CalibrateImmediate` 후 `Fade(0, 0.35)` 시작 → **Respawn의 페이드 아웃이 취소됨** → Respawn은 화면이 덜 어두운 상태에서 순간 이동하고, 0.25초 대기하는 동안 화면이 밝아진다 → 순간 이동 장면이 보인다 [코드 확인 / 체감 추정].
- **리스폰 중 Y·메뉴 길게 또는 재중심**: Calibrate가 리스폰 상태를 확인하지 않아서 같은 경쟁이 생긴다 (`Scripts/Player/RatEyeCalibrator.cs:136-140`).
- 제안: 전환 잠금 하나(`RatTransitionLock`)를 두어 캘리브레이션과 리스폰이 서로를 기다리게 하고, 캘리브레이션 중에는 `SetInputLocked`와 무적을 적용한다.

## 6. 멀미·편안함 평가
| 요소 | 값·계산 | 평가 | 태그 |
|---|---|---|---|
| 걷기 | 0.2 m/s → 체감 2.0 m/s. 가속 1.2 m/s²(체감 12 m/s²) → 0.2/1.2 ≈ 0.17초 만에 도달 | 무난 (빠른 가속이 오히려 vection 시간을 줄인다) | [코드 확인] |
| 달리기 | 0.2 × 1.8 = 0.36 m/s → 체감 3.6 m/s, 비네트 없음 | 경험자는 괜찮다. 입문자에게는 비네트 옵션이 필요할 수 있다 | [코드 확인] / [추정] |
| 질주 | 0.55 → 상한 5 × 0.1 = 0.5 m/s(체감 5 m/s)에서 잘림, 0.45초, 비네트 켜짐. 이동 거리 ≈ 0.5 × (0.45 − 0.04 − 0.075) + 램프 구간 ≈ 0.19m(체감 1.9m) | 짧고 방향이 고정되어 좋다 | [코드 확인] / 거리는 근사 |
| 질주 중 벽 | 0.5 → 0, 한 프레임 | 멈추는 쪽은 멀미가 덜하지만 "탁" 걸리는 느낌이 있다. 0.05~0.1초 감속을 권한다 | [코드 확인] |
| 점프 | 도약 속도 = √(2 × 0.981 × 0.035) ≈ 0.262 m/s, 체공 = 2 × 0.262 / 0.981 ≈ 0.53초(체감 35cm 점프) | 자연스럽다 | [코드 확인] |
| 널빤지 낙하 (최대 0.9m) | 중력 0.981, 최대 낙하 속도 1 m/s. t₁ = 1/0.981 ≈ 1.02초, d₁ = ½ × 0.981 × 1.02² ≈ 0.51m, 나머지 0.39m ÷ 1 = 0.39초 → **약 1.41초 동안 체감 9m 낙하**, 비네트 없음 | 이번 빌드에서 가장 멀미를 일으키기 쉬운 장면 [추정]. 공중 비네트를 켜거나 널빤지 가장자리에 턱을 단다 | [코드 확인] |
| 스냅 회전 | 30°, 0.5초, 순간 회전 | 표준 | [코드 확인] |
| 카메라 | Roll/Pitch/Head Bob 없음, TrackedPoseDriver만 사용 | 매우 좋음 | [코드 확인] |
| 페이드 | 캘리브레이션 0.2/0.35초, 리스폰 0.25 + 0.25 + 0.4초 | 좋음. 단 5-5절의 경쟁이 있다 | [코드 확인] |
| 리스폰 재정렬 | `CalibrateImmediate`로 정면을 체크포인트 방향에 맞춘다(검은 화면 동안) | 좋음 | [코드 확인] |
| HUD | 앞 0.24m, 아래 0.10m, 18°를 넘게 벗어나면 천천히 따라온다 (`Scripts/UI/RatHud.cs:23-28`) | 머리에 고정하지 않아 좋다. 경험자에게는 손목 패널과 HUD가 중복으로 느껴질 수 있다 | [코드 확인] |
| 비둘기, 발 | 발을 0.14m 들어 올림(체감 1.4m). 쥐 눈 0.12m 바로 위로 지나간다 | 공포 연출로는 좋다. 갑자기 시야를 덮는 장면은 [확인 불가] | [코드 확인] |

종합: 박태현(멀미 내성 높음)은 문제없이 완주한다 [추정]. 하지만 데모 부스의 다른 사람들을 생각하면 "낙하 비네트"와 "달리기 비네트 옵션"이 빠진 게 아쉽다.

## 7. 추격 난이도 평가 (계산 포함)
기본값: 보행자 발견 1.0m(요리사 1.1, 빗자루 0.9), 시야 ±65°, 반응 0.9초, 추격 0.55 m/s(요리사·빗자루 0.6), 회전 110°/s, 추격 3.5초, 숨은 지 1.2초면 놓침, 쿨다운 7초 (`Scripts/Gameplay/PedestrianAwareness.cs:34-53`, `Editor/PetitParisLevelBuilder.cs:78-93`).
밟히는 거리(근사) ≈ 멈추는 거리 0.22 + 발 내딛기 0.2 + 발 반폭 약 0.05 + 쥐 반지름 0.035 ≈ **0.5m** (`Scripts/Gameplay/PedestrianPath.cs:85-88`) [추정: 발 박스 크기는 읽지 않음].

| 상황 | 계산 | 결과 | 태그 |
|---|---|---|---|
| 1.0m에서 들킨 뒤 **걸어서** 곧장 도망 | 반응 시간 동안 이동 ≈ 0.2 × 0.9 − 가속 손실 0.017 ≈ 0.16 → 간격 1.16m. 좁혀지는 속도 0.55 − 0.2 = 0.35 → (1.16 − 0.5) / 0.35 ≈ 1.9초 < 3.5초 | **잡힌다** | [추정] |
| **달려서** 곧장 도망 | 반응 시간 동안 ≈ 0.36 × 0.9 − 0.054 ≈ 0.27 → 간격 1.27m. 좁혀지는 속도 0.19 → (1.27 − 0.5) / 0.19 ≈ 4.0초 > 3.5초 | 막다른 곳만 없으면 **아슬아슬하게 벗어난다** | [추정] |
| 달리기 + 질주 반복 | 주기 1.65초(질주 0.45 + 쿨다운 1.2): 0.19 + 0.36 × 1.2 ≈ 0.62m → 평균 ≈ 0.38 m/s | 달리기만 할 때보다 약간 낫다. 질주는 직선 탈출보다 옆으로 피해 회전 110°/s를 이용할 때 효과가 크다 | [추정] |
| 0.7m에서 들키고 가만히 있음 | NEXT_STEPS 1-2 실측 약 1.4초 | 잡힌다 | NEXT_STEPS 인용 |
| 웅크린 채 접근 | 발견 반경 1.0 × 0.45 = 0.45m. 이미 밟히는 거리(0.5m) 안이라, 들키는 순간 피하기 어렵다. 하지만 사람 앞을 0.45m보다 멀리 지나가면 들키지 않는다 | 몰래 지나가는 공략은 성립한다 | [추정] |
| 빗자루 이벤트 | 트리거 중심 (-0.5,18.6) → 팔레트 (-1.38,19.25): √(0.88² + 0.65²) ≈ 1.09m, 달리면 약 3.0초. 웨이터(z 21.7~22.1)에서 팔레트까지 약 2.5m ÷ 0.6 ≈ 4.2초 + 반응 0.9초 = 5.1초 | **숨을 곳을 알면 쉽다.** 단 화살표가 웨이터 쪽을 가리키고 표지는 0.6m 안에서만 보여서, 처음 하는 사람은 화살표를 따라가다 정면으로 마주칠 수 있다 | [코드 확인] / [추정] |
| 주방 요리사 | 추격 범위가 z 21.8~22.6으로 묶여 있다. 식탁·상자 위로 올라가면 안전 | 통로만 건너면 된다. 널빤지 아래쪽이 범위 끝에서 0.3m라 위험하다 | [코드 확인] |
| 자전거 | 끝(x +7)에서 골목 입구(x ≈ -0.5)까지 7.5m ÷ 2.2 ≈ 3.4초 + 가속 약 0.4초 + 정지 2.5초. 위험한 줄을 건너는 데 0.5m ÷ 0.36 ≈ 1.4초 | 소리를 들으면 충분히 건넌다 | [추정] |

박태현 수준에서는 "달리기로 거리 벌리기 → 추격 범위 밖이나 안전 구역" 공략을 1~2번 죽고 나서 찾는다. 판정이 불공정하다고 느낄 만한 곳은 ① 발이 내려오는 중인지와 상관없이 닿기만 하면 죽는 판정(프로젝트 분석 4절)과 ② 화살표가 위험 쪽을 가리키는 빗자루 구간이다 [추정].

## 8. 발견한 문제 목록
| # | 제목 | 분류 | 제안 우선순위 | 근거 | 태그 | 개선 제안 |
|---|---|---|---|---|---|---|
| 1 | 빗자루 추격 이벤트가 다시 준비되는지가 `Died` 구독 순서에 달려 있음 | 기능 | P1 | `Scripts/Gameplay/ChaseEvent.cs:189,226-241`, `Scripts/Gameplay/PedestrianAwareness.cs:95,103-108,209` | [코드 확인] / 발생 [확인 불가] | `OnChaseEnded`에서 리스폰 중이면 다시 준비, 또는 ChaseEnded에 이유를 넘김 |
| 2 | 캘리브레이션과 리스폰 페이드가 서로를 취소 → 순간 이동이 보일 수 있음. 캘리브레이션 중에는 입력·위험 판정이 잠기지 않음 | 기능/멀미 | P1 | `Scripts/Core/ScreenFader.cs:327-347`, `Scripts/Player/RatEyeCalibrator.cs:136-150`, `Scripts/Gameplay/RatRespawn.cs:85,109` | [코드 확인] | 전환 잠금 하나로 직렬화. 캘리브레이션 중 입력 잠금·무적 |
| 3 | 빗자루 추격 중 화살표가 웨이터 쪽을 가리킴 (숨을 곳 표지와 반대) | UX | P1 | `Editor/PetitParisLevelBuilder.cs:218-219`, `Scripts/UI/RatGuideArrow.cs` | [코드 확인] | 추격 중에는 가장 가까운 안전 구역을 가리키거나 화살표를 숨김 |
| 4 | OpenXR/XR Management가 manifest와 설정에 없음 → 클론해도 Quest 빌드가 재현되지 않음 | 구조 | P1 | `Packages/manifest.json`, `ProjectSettings/XRSettings.asset`, `docs/README.md:85-87,176` | [코드 확인] | 패키지와 XR 설정 파일을 커밋하고, Validator에 OpenXR 로더·Profile 확인 추가 |
| 5 | Haptic 전혀 없음 | UX | P2 | Scripts grep 결과 0건 | [코드 확인] | 발 착지(거리 비례), 들킴, 사망, 줍기, 질주 막힘에 짧은 진동 |
| 6 | 앞발이 할 수 있는 일이 없음. Interact 액션은 죽은 바인딩 | UX | P2 | `Scripts/Input/RatInputReader.cs:67,74`, `Assets/PetitParis/Settings/PetitParisInput.inputactions` | [코드 확인] | 부스러기를 앞발로 줍거나 손을 흔들어 비둘기를 쫓는 등 손을 쓰는 상호작용 1~2개. 안 쓸 거면 바인딩 제거 |
| 7 | 비네트가 질주에만 적용됨. 낙하(체감 9m, 1.41초)·달리기에는 없음. 게임 안 편안함 설정 없음 | 멀미 | P2 | `Scripts/Core/RatComfortSettings.cs:184-186`, `Scripts/Player/RatComfortApplier.cs:242-245` | [코드 확인] | 공중 비네트 기본 켜기, 시작 화면에 편안함 프리셋 |
| 8 | 비네트가 Samples/XRI 3.3.2 경로에 고정. 샘플이 없으면 WARN만 내고 비네트 없이 빌드 | 구조 | P2 | `Editor/PetitParisPaths.cs:27-28`, `Editor/PetitParisBuildKit.cs:408-418`, `Editor/PetitParisValidator.cs:128` | [코드 확인] | 프리팹을 프로젝트 폴더로 옮기고 Validator를 FAIL로 |
| 9 | 널빤지 폭 0.15m, 난간 없음. 아래쪽은 요리사 추격 범위에 가까움 | UX/멀미 | P2 | `Editor/PetitParisLevelBuilder.cs:566-570,691-692`, `Editor/PetitParisGameplayKit.cs:308-320` | [코드 확인] / 결과 [추정] | 폭 0.2~0.25, 낮은 턱 추가, 첫 널빤지 위치를 통로에서 떨어뜨림 |
| 10 | 포커스를 잃었을 때(Quest 메뉴)·컨트롤러 연결이 끊겼을 때 일시정지가 없음 | 기능 | P2 | Scripts grep 결과 `OnApplicationFocus`·`timeScale` 0건 | [코드 확인] / Quest 동작 [추정] | `OnApplicationFocus(false)`에서 `timeScale = 0` + 입력 잠금 |
| 11 | 게임 중 일시정지·재시작 메뉴가 없음. 왼쪽 메뉴 버튼을 재정렬이 차지함 (관례상 일시정지) | UX | P2 | `Scripts/Gameplay/PetitParisGameFlow.cs:105-133`, inputactions Calibrate | [코드 확인] | 짧게 누르면 일시정지(재시작·재정렬·조작 다시 보기), 길게 누르면 재정렬 |
| 12 | 달리기 = 그립(관례상 잡기), 스틱 클릭은 비어 있음 | UX | P3 | inputactions Run | [코드 확인] / 체감 [추정] | 왼쪽 스틱 클릭 바인딩을 추가(그립 유지 가능) |
| 13 | Y 길게 = 재정렬이 안내 패널에 없음 → 실수로 페이드가 일어날 수 있음 | UX | P3 | inputactions Calibrate(Y), `Scripts/Core/PetitParisText.cs:21` | [코드 확인] | 안내에 추가하거나 Y 바인딩 제거 |
| 14 | 웅크리기 쓸모가 적음: 웅크려야 하는 안전 구역 0개, 추격 중 효과 없음(오히려 느려짐), 대기 중인 일어서기를 취소할 수 없음 | UX | P3 | `Editor/PetitParisLevelBuilder.cs:308,481-482,491,619,664`(모두 `false`), `Scripts/Player/RatCrouchController.cs:283-310` | [코드 확인] | 웅크려야 지나갈 수 있는 틈이나 웅크려야 숨는 구역을 1곳 이상 만듦 |
| 15 | 사망 메시지가 한 칸에 덮어쓰기라 구독 순서에 따라 바뀜 | UX | P3 | `Scripts/Gameplay/PetitParisGameFlow.cs:219-228`, `Scripts/Gameplay/CrumbCollection.cs:150`, `Scripts/Gameplay/FallingHazard.cs:266-267` | [코드 확인] | 메시지 합치기 또는 우선순위 |
| 16 | 문서·주석 수치가 오래됨 (걷기 0.15/질주 0.45, 루트 README 목적지·달리기, "XRI 3.3.2") | 구조 | P3 | `docs/README.md:110,133`, `README.md`, `Scripts/Core/RatScaleConfig.cs:302`, `Scripts/Player/RatLocomotionController.cs:73` | [코드 확인] | 수치를 문서에 적지 말고 Validator 출력으로 대체하거나 일괄 갱신 |
| 17 | 쓰지 않는 패키지·에셋 (visualscripting, collab-proxy, multiplayer.center, 전역 InputSystem_Actions, AnyNoticed, HintGoal) | 구조 | P3 | `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset`, `Scripts/Gameplay/PedestrianAwareness.cs:73` | [코드 확인] | 정리. 전역 액션 등록 해제 |
| 18 | unity-mcp git URL 버전 고정 없음 | 구조 | P3 | `Packages/manifest.json:3`, `Packages/packages-lock.json:3-7` | [코드 확인] | 태그·커밋으로 고정 |
| 19 | 레벨 좌표 중복(체크포인트 ↔ 경로 노드), 매직 넘버 | 구조 | P3 | `Editor/PetitParisLevelBuilder.cs:99-103,215-219` | [코드 확인] | 경로 노드가 체크포인트 Transform을 참조하게 하거나, LevelLayout 데이터로 분리 |
| 20 | 화면 문자열 일부가 PetitParisText 밖에 있고, 폰트에 미리 채워지지 않은 글자가 있음 | 구조/성능 | P3 | `Editor/PetitParisLevelBuilder.cs:308,491`, `Editor/PetitParisBuildKit.cs:505,948,982` | [코드 확인] / 끊김 [추정] | 안전 구역 이름·위험물 이름을 const로 옮김 |
| 21 | 질주가 벽에 막히면 한 프레임에 0으로 멈춤 | 멀미/UX | P3 | `Scripts/Player/RatLocomotionController.cs:177-181` | [코드 확인] | 0.05~0.1초 감속 + 진동 |
| 22 | 점프가 꼭 필요한 지형을 찾지 못함 (점프가 장식에 가까움) | UX | P3 | `Scripts/Player/RatJumpController.cs:169`, 레벨 빌더 샘플 | [추정] | 4~5cm 턱이나 틈 건너기 등 점프로 지름길을 내는 곳 1~2개 |

## 9. 좋았던 점
- **이동 경로 하나 + Validator로 강제**: 다른 Move/Gravity/Jump Provider가 섞이면 FAIL. VR 프로젝트에서 흔한 "이동 두 배 빨라짐" 버그를 구조적으로 막았다 (`Scripts/Player/RatLocomotionController.cs:6-22`, `Editor/PetitParisValidator.cs:87-89`) [코드 확인].
- **카메라를 직접 움직이지 않음**: Roll·Pitch·Head Bob 없음. 순간 이동은 항상 Tracking Space만 바꾸고 페이드를 낀다 (`Scripts/Player/RatEyeCalibrator.cs:155-204`) [코드 확인].
- **앉든 서든 같은 쥐 눈높이**, 재중심 이벤트에 자동 대응 (`Scripts/Player/RatEyeCalibrator.cs:206-228`) [코드 확인]. 경험자에게도 "제대로 만들었다"는 인상을 준다 [추정].
- **충돌 처리가 튕기지 않음**: 위험 판정은 물리로 밀어내지 않고 거리 계산만 한다. 순간 이동할 때는 CharacterController를 끄고 `SyncTransforms`를 부른다 (`Scripts/Gameplay/RatDangerDetector.cs:239`, `Scripts/Gameplay/RatRespawn.cs:116-125`) [코드 확인].
- **입력 처리가 꼼꼼함**: `WasPressedThisFrame`, 점프 코요테 0.1초·버퍼 0.12초, 질주 방향 고정, 스틱이 없으면 질주 불가, 질주 쿨다운 표시 [코드 확인].
- **리스폰 안전장치**: 입력 잠금 → 이동 큐 정지 → 한 프레임 대기 → 순간 이동 → 상태 초기화 → 무적 1.5초 (`Scripts/Gameplay/RatRespawn.cs:76-113`) [코드 확인].
- **XROriginGuard**로 샘플 리그를 실수로 끌어다 놓는 사고를 막음 [코드 확인].
- **게임 씬에서는 디버그 패널을 숨김**, 개발 중에는 오른쪽 손목에서 바로 확인 [코드 확인].
- **정직한 문서**: "미검증" 표기, 알려진 문제, 로컬 검증 로그 [코드 확인].

## 10. 인터뷰
> 박태현의 말투로 쓴 가상 응답. 근거는 위 체험 기록이다.

**1. 처음 실행했을 때 무엇을 해야 한다고 생각했는가?**
"앞에 뜬 패널을 보고 '치즈 찾아가는 거구나' 했어요. 그다음엔 습관대로 스냅 각도부터 확인하고, 메뉴 버튼을 눌러 보고, 벽에 붙어 봤죠. 패널에 조작이 다 적혀 있어서 시작은 어렵지 않았어요."

**2. 가장 이해하기 어려웠던 부분은 무엇인가?**
"손이요. 앞발이 보이니까 당연히 뭔가 집을 수 있을 줄 알고 그립을 눌렀는데 달리기가 되더라고요. 왼쪽 트리거는 눌러도 아무 반응이 없고요. 디버그 패널을 보면 Use 입력은 들어오는데 쓰는 데가 없어요. 손이 왜 있는지 끝까지 이해하지 못했어요."

**3. 다음 행동을 결정하기 어려웠던 순간이 있었는가?**
"빗자루 웨이터요. 음악이 깔리고 '숨을 곳으로!'라고 하는데, 바닥 화살표는 계속 앞으로, 그러니까 웨이터 쪽을 가리켜요. 팔레트 표지는 거의 코앞에 가야 보이고요. 화살표를 믿어야 할지 메시지를 믿어야 할지 1~2초 멈칫했어요."

**4. 예상과 실제 동작이 달랐던 부분은 무엇인가?**
"세 가지요. 첫째, 왼쪽 메뉴 버튼이 일시정지가 아니라 재정렬이에요. 길게 누르니까 화면이 까매져서 놀랐어요. 둘째, 쫓길 때 웅크리면 숨겨질 줄 알았는데 느려지기만 해서 바로 밟혔어요. 웅크리기는 들키기 전에만 효과가 있더라고요. 셋째, 개발자 버릇으로 빗자루에 일부러 맞고 다시 들어갔는데, 추격이 다시 시작됐는지는… 코드상으로는 구독 순서에 달려 있어서 장담을 못 하겠어요."

**5. 사용하면서 불편하거나 불안했던 순간은 무엇인가?**
"주방 널빤지요. 체감 1.5m 폭에 난간이 없는데, 고개를 돌리면 이동 방향도 같이 돌아가니까 옆으로 떨어지기 쉬워요. 떨어지면 체감 9m를 1.4초 동안 비네트도 없이 떨어져요. 저는 괜찮은데 입문자는 여기서 헤드셋 벗을 것 같아요. 그리고 진동이 하나도 없어서, 뒤에서 발이 내려오는 걸 소리로만 판단해야 하는 게 불안했어요."

**6. 어떤 피드백이 더 명확했으면 좋겠는가?**
"진동이 1순위예요. 발이 가까이 내려올 때 약하게, 들켰을 때 짧게, 죽을 때 강하게만 줘도 앞뒤 구분 문제가 상당히 해결돼요. 그리고 질주가 벽에 막혔을 때, 웅크린 채 점프나 질주가 안 될 때 왜 안 되는지 알려 주는 신호가 있으면 좋겠어요. 지금은 손목을 봐야만 알 수 있어요."

**7. 사용을 중단하고 싶었던 순간이 있었는가?**
"중단까지는 아니에요. 판정이 크게 불공정하다고 느낀 곳은 없었고, 기술적으로 궁금한 게 많아서 끝까지 했어요. 다만 널빤지에서 두 번 떨어져서 바닥부터 다시 오를 때는 좀 지겨웠어요."

**8. 다시 사용한다면 어떤 경험을 기대하는가?**
"쥐 시점 스케일 표현은 진짜 신선해요. 앞발로 부스러기를 집거나 비둘기를 쫓는 것처럼 손을 쓰는 순간이 한두 개만 있어도 'VR이라서 하는 게임'이 될 것 같아요. 그리고 일시정지 메뉴에서 비네트나 회전 방식을 고를 수 있으면 친구한테도 권할 수 있겠어요."

**후속 질문 (체험 기록 기반)**

**F1. (2-0-6) 그립을 처음 눌렀을 때 정확히 무엇을 기대했고, 실제로는 어떻게 됐나?**
"벽에 붙은 빵 부스러기를 앞발로 집으려고 그립을 쥐었어요. 그런데 몸이 앞으로 확 빨라졌어요. 스틱을 살짝 밀고 있었거든요. 패널에 '왼쪽 그립 누르고 있기 달리기'라고 적혀 있긴 했는데, 손이 먼저 나갔어요. 부스러기는 그냥 몸으로 지나가니까 주워지더라고요."

**F2. (2-3-1) 화살표와 메시지가 엇갈렸을 때 결국 무엇을 따라갔나? 그 판단은 몇 초 걸렸나?**
"처음 1초는 화살표를 따라 북쪽으로 갔어요. 웨이터가 보여서 멈췄고, 그때 왼쪽 앞에 '팔레트 밑에 숨어요!'가 떠서 그쪽으로 꺾었어요. 계산해 보면 팔레트까지 달려서 3초쯤이고 웨이터는 5초 넘게 걸리니까 여유는 있었어요. 그런데 처음 하는 사람이 화살표만 믿고 계속 직진하면 정면으로 만나요."

**F3. (2-3-3) Y를 길게 누른 건 의도한 건가? 그때 무엇이 보였나?**
"반은 실수, 반은 테스트였어요. Y에 뭐가 있나 해서 누르고 있었더니 화면이 어두워졌다 밝아졌어요. 그게 쫓기는 중이었으면… 코드를 보면 그사이에 밟히면 페이드가 꼬여서 순간 이동이 살짝 보일 수 있어요. 직접 본 건 아니라 단정은 못 하지만, 저라면 리스폰 중에는 캘리브레이션을 막겠어요."

**F4. (2-5-1) 널빤지에서 떨어진 건 어느 구간이었고, 왜 떨어졌다고 생각하나?**
"마지막 널빤지, 0.6m에서 0.9m로 올라가는 구간이요. 식탁 위 치즈를 보려고 고개를 왼쪽으로 돌렸는데, 이동이 시선 기준이라 몸도 그쪽으로 틀어졌어요. 4cm쯤 여유밖에 없으니 바로 떨어졌죠. 컨트롤러 기준 이동 옵션이 있었으면 안 떨어졌을 것 같아요."

**F5. (2-0-4) 일시정지 메뉴가 없다는 걸 알았을 때, 실제로 쉬거나 나가고 싶으면 어떻게 할 생각이었나?**
"Meta 버튼으로 시스템 메뉴를 열었겠죠. 그런데 그동안 게임이 멈추는지는 모르겠어요. 코드에 포커스 처리가 없으니까, 메뉴를 보는 사이 요리사한테 밟혀 있을 수도 있어요. 학회 부스에서 진행자가 헤드셋을 넘겨받을 때도 그렇고요."
