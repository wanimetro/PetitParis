# PETIT PARIS — 다음 단계 (로컬 Claude Code + Unity MCP로 이어서 작업)

> 이 문서는 클라우드 세션(Unity 화면을 볼 수 없음)에서 정리한 인수인계 문서다.
> 이후 작업은 **사용자 PC의 Claude Code + Unity MCP**로 한다. Claude가 직접 Play하고, 스크린샷을 보고, Console을 읽으며 고칠 수 있다.
> 아래 내용은 모두 **Unity에서 미검증**이다.

## 0. 사용자 결정 사항 (2026-11-14)
- 작업 방식: 내 PC에서 Claude Code + Unity MCP
- 아트: 파리 느낌에 맞는 무료 에셋을 찾는다. 레포는 **Private**으로 바꾼다 (Asset Store 등 재배포 금지 에셋을 넣기 위해). Quest 3 최적화를 함께 한다.
- 재미 요소: **빵 부스러기 수집 + 점수**, **추격·긴박한 이벤트**, **마지막 구간은 식당 주방**

## 1. 로컬 작업 환경 만들기 (사용자가 한 번만)
1. GitHub에서 PetitParis 레포를 **Private**으로 바꾼다: Settings → General → Danger Zone → Change repository visibility → Private
2. PC에 **uv**를 설치한다 (Unity MCP 서버가 Python 3.10+를 uv로 실행). PowerShell:
   ```powershell
   powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
   ```
3. Unity로 PetitParis 프로젝트를 연다. `com.coplaydev.unity-mcp` 패키지는 이미 manifest에 들어 있다.
4. Unity 메뉴 **Window → MCP for Unity → Toggle MCP Window** (Ctrl+Shift+M)를 연다.
   - Python이나 uv가 없다고 나오면 **Window → MCP for Unity → Local Setup Window**에서 **Install UV Automatically**를 먼저 실행한다.
   - MCP 창의 **Client Configuration**에서 Client를 **Claude Code**로 고르고 **Configure**를 누른다 (또는 **Configure All Detected Clients**). 내부에서 `claude mcp add`를 실행한다.
   - "Claude CLI Path"를 못 찾으면 PowerShell에서 `where.exe claude`로 경로를 확인해 **Browse**로 지정한다.
   - **Connect** 버튼이 있으면 눌러 Unity 쪽 연결을 켠다.
5. PowerShell에서 프로젝트 폴더로 이동해 Claude Code를 실행한다.
   ```powershell
   cd "<PetitParis 폴더 경로>"
   claude
   ```
6. PowerShell에서 `claude mcp list`에 UnityMCP가 보이는지, Claude Code 안에서 `/mcp`를 입력해 connected인지 확인한다.
7. 첫 요청 예시:
   > "docs/NEXT_STEPS.md를 읽고, PetitParisVR 씬을 Play해서 스크린샷과 Console을 확인한 뒤 '알려진 문제'부터 고쳐줘."

## 1-1. 2026-11-14 클라우드에서 구현한 재미 요소 (Unity 미검증 — 컴파일은 참조 어셈블리로만 확인)
메뉴 **PetitParis > 3. Build Game Scene**을 다시 실행해야 씬에 반영된다.

| 기능 | 파일 | 확인할 것 |
|---|---|---|
| 빵 부스러기(1)·크루아상(3)·치즈(5)·황금 크루아상(10, 3개). 체크포인트 전에 밟히면 떨어뜨림. 별점 ★도착 ★60% ★황금 3개 | `CrumbPickup`, `CrumbCollection`, 배치는 `PetitParisLevelBuilder.PlaceCrumbs` | 줍기 반경(7~8.5cm), "+1" 글자 크기·방향, 완료 패널 별 표시 |
| 사람이 쥐를 알아챔: "!" → "앗, 쥐다!" → 발을 쥐 쪽으로 내딛으며 추격 → 숨으면 "어디 갔지?" | `PedestrianAwareness`, `PedestrianPath`(BeginChase/EndChase/PauseAndLook, stompReach) | 발견 거리 1m·시야 65°·추격 0.55m/s가 너무 어려운지, 말풍선(높이 2.15m)이 쥐 시점에서 보이는지 |
| 빗자루 웨이터 추격 이벤트 + 긴장 음악. 팔레트 밑·종이 상자 안에 숨기 | `ChaseEvent`, `BroomSweep` | 골목 z 18.6에서 시작, 9초. 빗자루 머리 판정 |
| 빵집 앞 떨어지는 바게트 (그림자 예고 1.1초, 떨어진 자리에 보너스 부스러기) | `FallingHazard` | 예고 시간·간격 2.6초가 공정한지 |
| 비둘기 떼 (가까이 가면 날아오름, 위험 없음) | `PigeonFlock` | 강변 2무리, 차도 1무리 |
| 마지막 구간: 비스트로 주방. 뒷문 → 널빤지(약 21°) → 상자 0.3/0.6/0.9m → 도마 다리 → 식탁 위 치즈(목적지) | `PetitParisLevelBuilder.BuildKitchen` | 쥐가 널빤지를 실제로 올라가는지 (CharacterController slopeLimit 50°, 폭 15cm), 요리사 동선 |
| 시야 아래 안내판 (점수 + 메시지, 천천히 따라옴) | `RatHud` | PC 화면에서 보이는 위치(눈 아래 5.5cm, 앞 26cm) |
| 이야기 메시지 (센강, 주방 입구) | `RatStoryTrigger` | |
| 쥐 속도 상향: 걷기 0.2, 질주 0.55 (월드 m/s) | `RatScaleConfig`, 빌더가 기존 에셋도 올림 | 멀미 여부 |

## 1-2. 로컬 Unity 검증 결과 (2026-11-14, Unity 6000.6.9f1 + MCP, PC 모드 에디터 Play)
쥐를 스크립트로 각 지점에 옮기고(`RatRespawn.TeleportTo`), 상태 변화를 Console 로그로 기록하며 확인했다. 실제 조작(키보드·컨트롤러)으로 처음부터 끝까지 플레이한 것은 아님. Quest 실기기는 **미검증**.

| 기능 | 결과 | 고친 것 |
|---|---|---|
| 빵 부스러기 줍기·점수·HUD | 동작 | "+1" 글자가 주운 자리(눈앞 몇 cm)에 떠서 화면을 덮음 → 눈앞 25cm에 띄움 (`CrumbCollection.PopupAnchor`) |
| HUD | 동작 | 화살표와 같은 자리(앞 0.26m, 아래 0.055m)라 겹침 → 앞 0.24m, 아래 0.10m (`RatHud`) |
| 사람이 알아챔 → 추격 → 밟힘 | 동작 (0.7m에서 들키면 약 1.4초 만에 밟힘) | - |
| 빗자루 웨이터 추격 이벤트 | 동작 (음악, 빗자루 사망, 죽으면 다시 준비) | - |
| 떨어지는 바게트 | 동작 (그림자 예고, 약 2.6초 간격, 보너스 부스러기, 바게트 사망) | - |
| 비둘기 | 다가가면 날아오름 | - |
| 주방 오르기 | 바닥 → 널빤지 → 상자 0.3/0.6/0.9 → 도마 → 식탁 위까지 막힘 없음 (`CharacterController.Move`로 확인) | - |
| 도착(치즈) | **안 됐음** → 고침 | 치즈 바퀴 충돌체에 막혀 서쪽에서 닿으면 도착 구역 밖 → 구역을 바퀴 중심에 두고 1.0m로 넓힘 |
| 완료 패널 | **안 보였음** → 고침 | 눈앞 패널 자리가 치즈 바퀴 속 → 패널을 깊이 검사 없이 맨 위에 그림 (`PetitParisBuildKit.MakeOverlay`). 맨 아래 두 줄 겹침도 고침 |

남은 관찰 (사용자 판단 필요):
- 사람 말풍선 높이 2.15m: 쥐 눈에서 0.7m 거리면 올려다보는 각도 약 71°라 PC 화면 밖. 같은 내용이 HUD에도 나온다
- HUD 메시지가 길면 PC 화면에서 오른쪽이 잘림 (HUD는 시선이 18° 넘게 벗어나야 따라옴 + 글자 상자 0.34m)
- 추격 난이도(1.4초)
- 자동화 팁: `PetitParisBuildKit.AssumeYes = true`로 두고 빌더·Validator를 호출하면 대화상자 없이 돈다 (MCP가 멈추지 않음). Play 중에는 코드를 고치지 않는다 (Play 중 재컴파일되면 `PedestrianPath` 발 상태가 사라져 NullReference가 난다)

## 2. 알려진 문제 (먼저 고칠 것)
| 증상 | 메모 |
|---|---|
| ~~화면 전체가 갈색 한 색으로 보임~~ (2026-11-14 고침, PC 에디터에서 확인) | 원인: 시작 지점이 카페 벽 틈(폭 30cm) **안쪽 25cm**라, 돌아보면 화면이 틈 벽으로 덮였다. 시작 지점을 틈 입구 바로 바깥(-2.85, 0, -0.4)으로 옮기고, 안내판은 시작 지점 기준 53cm 앞으로. Validator에 "시작 지점 시야 트임(8방향 중 3방향 이상 0.5m)" 점검 추가 |
| PC 모드 위아래 시점 | Tracking Space Pitch 방식으로 바꿨다 (커밋 6349fa0). 실제 동작 미확인 |
| 내비게이션 화살표 | 바라보는 방향 앞에 띄우고 경로 방향을 가리키게 바꿨다. 실제로 보이는지 미확인 |
| 해 질 녘 화면이 어두움 | 주변광을 올렸다. 스크린샷을 보며 조정 |
| 전체 품질 | 상자 기반 블록아웃이라 저퀄. 아래 3번 에셋으로 교체한다 |

## 3. 에셋 후보 (라이선스는 넣기 전에 페이지에서 다시 확인하고 CREDITS에 기록)
### 배경 (파리)
| 후보 | 가격 | 파이프라인 | 메모 |
|---|---|---|---|
| [Stylized Paris City](https://assetstore.unity.com/packages/3d/environments/stylized-paris-city-380450) | $39.99 | Built-in/URP/HDRP | 사용자가 원한 분위기. 2.9GB라 Quest용으로 필요한 부분만 쓰고 질감을 1K로 줄여야 한다. **확인된 유일한 URP 파리 팩** |
| [Stylized Paris Street](https://assetstore.unity.com/packages/3d/environments/stylized-paris-street-214033) | $34.99 | **HDRP 전용** | Quest(URP)에는 부적합 |
| [French Quarter Street Environment](https://assetstore.unity.com/packages/3d/environments/urban/french-quarter-street-environment-303625) | $69.99 | 확인 필요 | 비쌈 |
| Quaternius [LowPoly Buildings](https://opengameart.org/content/lowpoly-buildings-pack) / [Medieval Village](https://opengameart.org/content/lowpoly-medieval-village-pack) | 무료 CC0 | 무관 | 파리풍 아님. 지붕·창문을 바꿔 써야 한다 |
| Kenney [Modular Buildings](https://opengameart.org/content/building-kit) 등 | 무료 CC0 | 무관 | 파리풍 아님 |
| Poly Haven / ambientCG 질감·소품 | 무료 CC0 | 무관 | 포석·석회암·나무 질감, 소품 |

- **무료로 파리 느낌이 제대로 나는 팩은 검색에서 찾지 못했다.** Asset Store에서 Free + URP 필터로 "Paris", "French", "European town"을 직접 찾아본다.
- 무료 공유 사이트에 올라온 유료 팩(예: unityassets4free 등)은 쓰지 않는다.

### 사람 (셰프·웨이터·행인)
| 후보 | 가격 | 메모 |
|---|---|---|
| Quaternius 캐릭터 + [Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html) | 무료 CC0 | 걷기 애니메이션 포함. 셰프 모자·앞치마는 소품으로 붙인다 |
| [Chef Kit - Poly Style (itch.io)](https://camisadostudios.itch.io/chef-kit-poly-style) | 가격·라이선스 미확인 | Unity Humanoid 리그 셰프 4종 |
| [Cooks - Animated Male and Female (Asset Store)](https://assetstore-fallback.unity.com/packages/3d/characters/humanoids/humans/cooks-animated-male-and-female-characters-282054) | 가격 미확인 | 애니메이션 포함 |

- 사람 모델을 넣으면 `PedestrianPath`는 몸 이동과 방향만 맡고, 신발 `FootstepHazard`는 Animator의 발 뼈(`HumanBodyBones.LeftFoot/RightFoot`)에 붙인다. 발소리는 발 높이가 가장 낮아지는 순간이나 애니메이션 이벤트로 낸다. 바닥 그림자·착지 예고 표시도 발 뼈를 따라가게 바꾼다.

## 4. 재미 요소 설계 (사용자 선택)
### 4-1. 빵 부스러기 수집 + 점수
- `CrumbPickup` 컴포넌트: 크루아상·바게트·치즈 조각. 쥐 몸이 반경 안에 들어오면 수집 (거리 판정, `RatDangerDetector`처럼)
- 위험한 곳(발 사이, 자전거 길 한가운데)에 큰 조각, 안전한 곳에 작은 조각 → 위험과 보상
- 수집하면 작은 소리, 조각이 쥐 쪽으로 빨려 들어가는 연출. 손목 패널에 개수 표시
- 도착하면 별점(★1~3): 시간, 수집 개수, 실패 횟수. 완료 패널에 표시
- 수집 상태는 체크포인트에 저장 (리스폰해도 이미 모은 건 유지)

### 4-2. 추격·긴박한 이벤트
- **빗자루 웨이터**: 테라스 끝 트리거에 들어가면 웨이터가 빗자루로 바닥을 쓸며 쥐 쪽으로 다가온다 (빠른 FootstepHazard + 넓은 빗자루 판정). 긴장 음악이 켜지고, 안전 구역에 들어가면 끝난다
- **떨어지는 바게트**: 빵집 앞에서 바게트가 떨어진다. 바닥에 커지는 그림자로 예고 → 질주로 피한다
- **비둘기 떼**: 다가가면 날아올라 시야를 가리는 연출 (위험은 아님, 분위기)
- 공통: 이벤트 시작과 끝에 짧은 음악·효과음. 카메라는 절대 흔들지 않는다 (멀미 방지)

### 4-3. 마지막 구간: 식당 주방
- 골목 끝을 센강 대신 비스트로 뒷문으로 바꾼다 (또는 센강을 본 뒤 주방으로 들어간다)
- 주방 내부: 반짝이는 타일 바닥(반사 프로브, 바닥만 URP Lit), 구리 냄비, 화덕 불빛, 체크 식탁보, 나무 의자
- 바쁜 셰프 1~2명이 오가는 동선 + 떨어지는 음식 이벤트. 목표는 선반 위의 치즈 (상자 → 의자 → 테이블로 점프해 올라감)
- 레퍼런스 분위기: 따뜻한 오후 햇살, 반짝이는 바닥, 애니메이션 영화 같은 톤 (특정 영화 캐릭터·이미지는 쓰지 않는다)

## 4-4. 배경 에셋 (클라우드에서는 에셋 사이트 접속이 막혀 있음 → 로컬에서 진행)
로컬 Claude는 인터넷이 되므로 아래 순서로 진행한다. 넣기 전에 후보·라이선스를 사용자에게 먼저 보여 준다.
1. Poly Pizza(poly.pizza)·Quaternius·Kenney에서 CC0/CC-BY 로우폴리 모델을 찾는다: 파리 건물 정면(발코니·망사르드 지붕), 카페 의자·테이블, 가로등, 차양, 바게트·크루아상, 셰프·웨이터 캐릭터
2. glTF/FBX를 `Assets/PetitParis/ThirdParty/<출처>/`에 넣고 `docs/CREDITS.md`에 작가·라이선스·URL 기록 (CC-BY는 작가 표기 필수)
3. `PetitParisLevelBuilder`의 건물 덩어리(`TexBoxXZ` + `FacadeDetails`)를 프리팹 배치로 바꾸되, 충돌용 상자(동선)는 그대로 둔다
4. Play → 스크린샷으로 쥐 시점(눈높이 12cm)에서 확인하며 조정
- 무료로 파리 느낌이 확실한 팩을 못 찾으면 Asset Store의 Stylized Paris City($39.99, URP 지원)가 가장 가까운 대안 (사용자 결정)

## 5. 작업 순서 제안 (로컬 세션)
1. 알려진 문제 수정 (Play + 스크린샷으로 확인)
2. 레포를 Private으로 바꾼 뒤 배경 에셋을 정하고 임포트 → 지금 동선(벽·의자·보행자 경로·체크포인트)에 맞춰 건물 교체. `PetitParisLevelBuilder`는 에셋 프리팹을 배치하도록 바꾸거나, 씬을 직접 편집하는 방식으로 전환할지 결정
3. 사람 모델 교체 (CC0 Quaternius부터)
4. 빵 부스러기 + 별점
5. 추격 이벤트
6. 식당 주방 구간
7. Quest 3 빌드, 72fps 확인 (OVR Metrics Tool), 최적화
