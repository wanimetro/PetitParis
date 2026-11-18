# PETIT PARIS 🐭🥖

> 작은 쥐가 되어 파리의 거리를 지나 비스트로 주방의 치즈까지 — Meta Quest용 1인칭 VR 게임

플레이어는 **눈높이 약 12cm의 쥐**가 됩니다. 사람의 발은 집채만 하고, 카페 의자는 지붕이 되고, 바게트는 통나무처럼 굴러떨어집니다.
카페 테라스 → 빵집 앞 → 뒷골목 → 센강 강변 → **비스트로 주방**을 지나 **식탁 위 치즈**에 닿으면 도착입니다.

| 항목 | 내용 |
|---|---|
| 장르 | 1인칭 VR 탐험·회피 (플레이 약 3~5분, 앉아서 가능) |
| 기기 | Meta Quest 2 (Touch 컨트롤러) 기준, Quest 3도 같은 빌드. 헤드셋 없이 PC에서도 실행 가능 (키보드·마우스) |
| 엔진 | Unity 6000.6.9f1, URP 17.6 |
| 상태 | **블록아웃 프로토타입**. 기본 도형으로 만든 건물·사람. Quest 2 실기기에서 한 번 플레이 테스트, 그 뒤 수정 사항은 실기기 재확인 전 |

---

## 1. 게임 소개

### 핵심 경험
- **거대한 발걸음 피하기**: 사람 발이 내려올 자리에 그림자와 붉은 예고 원이 생기고, 3D 발소리로 방향을 알 수 있습니다.
- **들키면 도망치거나 숨기**: 사람이 쥐를 보면 "!" → "앗, 쥐다!" 하고 쿵쿵 밟으며 쫓아옵니다. 초록 표시가 있는 곳(의자·벤치·팔레트·상자 밑)에 숨으면 놓칩니다. 들키면 안내 화살표가 초록색으로 가장 가까운 숨을 곳을 가리킵니다.
- **수집과 별점**: 빵 부스러기(1점)·크루아상(3점)·치즈 조각(5점)·황금 크루아상(10점, 3개). 체크포인트에 닿기 전에 밟히면 그동안 모은 조각을 떨어뜨립니다.
  - ★ 도착 ★ 빵 부스러기 60% 이상 ★ 황금 크루아상 3개
- **체크포인트와 리스폰**: 7개. 실패하면 페이드 후 가까운 체크포인트에서 다시 시작합니다.

### 구간
| 구간 | 위험 / 이벤트 |
|---|---|
| 1. 카페 테라스 | 테라스를 도는 웨이터 (25초 뒤 출발 — 처음엔 조작 연습), 의자 아래 숨기 |
| 2. 빵집 앞 | 지그재그로 걷는 셰프, **차양 위에서 굴러떨어지는 바게트** (그림자 예고, 차양 밑은 바게트 안전) |
| 3. 뒷골목 | 가로지르는 **자전거**, **빗자루 웨이터 추격 이벤트**(긴장 음악) — 팔레트·와인 상자·빵 바구니·종이 상자에 숨기 |
| 4. 센강 강변 | 센강과 에펠탑 원경, 다가가면 날아오르는 **비둘기 떼** |
| 5. 비스트로 주방 | 오가는 요리사, **널빤지 → 상자 → 식탁**으로 올라가 치즈에 도착 |

---

## 2. 설치 방법

### 2-1. 준비물
- Unity Hub + **Unity 6000.6.9f1** (모듈: **Android Build Support** — OpenJDK, Android SDK & NDK Tools 포함)
- Meta Quest 2 (또는 3), USB-C 데이터 케이블
- Quest **개발자 모드**: 휴대폰 Meta Horizon 앱 → 기기 → 헤드셋 설정 → 개발자 모드 켜기 (Meta 개발자 계정 필요)

### 2-2. 프로젝트 받기
```bash
git clone https://github.com/wanimetro/PetitParis.git
```
Unity Hub → Add → 받은 `PetitParis` 폴더 → Unity 6000.6.9f1로 열기.

### 2-3. PC에서 바로 해 보기 (헤드셋 없이)
1. 메뉴 **PetitParis > 3. Build Game Scene (PetitParisVR)** — 씬 전체를 코드로 생성합니다
2. 메뉴 **PetitParis > 4. Validate Scene** — Console에서 FAIL 0개 확인
3. **Play** → Game 창을 한 번 클릭 → Space로 시작 안내 닫기

### 2-4. Quest 2에 설치 (Android APK, Quest Link 불필요)
> OpenXR·XR Plug-in Management 패키지는 저장소에 들어 있지 않습니다. 처음 한 번 에디터에서 설치하세요.

1. **Edit > Project Settings > XR Plug-in Management** → Install
2. **Android 탭(로봇 아이콘)** → **OpenXR** 체크 → **Meta Quest Support** 체크
3. **XR Plug-in Management > OpenXR** → Android 탭 → **Enabled Interaction Profiles** **+** → **Oculus Touch Controller Profile** (Quest 3도 쓰면 *Meta Quest Touch Plus Controller Profile* 추가)
4. Meta Quest Support 톱니바퀴 → Target Devices에서 **Quest 2** 체크
5. **XR Plug-in Management > Project Validation** → Android 탭 → **Fix All**
6. **File > Build Profiles** → **Android** → **Switch Platform**
7. 메뉴 **PetitParis > 0. Apply Quest Android Player Settings** (ARM64, IL2CPP, Vulkan, Min API 32, Linear)
8. 메뉴 **PetitParis > 3** → **PetitParis > 4** (FAIL 0개 확인)
9. Quest 2를 USB로 연결 → 헤드셋 안 **"USB 디버깅 허용 → 이 컴퓨터에서 항상 허용"**
10. **File > Build Profiles** → Android → **Run Device**: Refresh → **Oculus Quest 2** → **Build And Run** → 저장 위치 `Builds/PetitParis.apk`
11. 빌드가 끝나면 헤드셋에서 자동 실행. 다시 실행: 앱 목록 → 필터 **알 수 없는 출처** → PETIT PARIS

자세한 순서와 실기기 테스트 체크리스트: [`docs/QUEST2_ISSUE_RESOLUTION.md`](docs/QUEST2_ISSUE_RESOLUTION.md) 6절

---

## 3. 조작 방법

| 동작 | Quest 2 / 3 Touch 컨트롤러 | PC (키보드·마우스) |
|---|---|---|
| 이동 | 왼쪽 스틱 | WASD / 방향키 |
| 회전 (30도씩) | 오른쪽 스틱 좌우 | `,` `.` |
| 둘러보기 | 머리 돌리기 | 마우스 **오른쪽 버튼 누른 채** 드래그 (헤드셋이 없을 때만) |
| 달리기 | **왼쪽 스틱 꾹 누르기**(켜기/끄기, 멈추면 자동으로 꺼짐) 또는 왼손 그립 누르고 있기 | Caps Lock(켜기/끄기) 또는 왼쪽 Shift 누르고 있기 |
| 질주 (0.6초, 아주 빠르게) | **오른쪽 트리거** 또는 **오른손 그립** — 스틱 방향, 안 밀면 바라보는 방향. 발동하면 오른손 진동 + 바람 소리 | 마우스 왼쪽 클릭 / 오른쪽 Shift |
| 점프 | A | Space |
| 웅크리기 (느리지만 사람이 가까이 와야 알아챔) | B | C |
| 시작 안내 닫기 | A | Space |
| 일시 정지 메뉴 (조작법·규칙·편안함 모드) | **X** | Tab / H |
| 메뉴에서 편안함 모드 켜기/끄기 | A | Space |
| 눈높이 다시 맞추기 | 왼쪽 ≡ 버튼 길게 (또는 Y 길게) | F5 |
| 다시 시작 (도착 화면) | A | Space |

**편안함 모드** (멀미에 민감한 분): X 메뉴에서 A로 켭니다. 이동이 느려지고, 걸을 때도 시야 가장자리가 어두워지며(비네트), 점프가 꺼지고, 추격하는 사람도 느려집니다. 설정은 기기에 저장됩니다.

---

## 4. 사용 기술

| 분류 | 사용 |
|---|---|
| 엔진·렌더링 | Unity 6000.6.9f1, **URP 17.6** (Android는 `Mobile_RPAsset`: HDR 끔, MSAA 4x, Render Scale 1.0, 그림자 10m) |
| XR | **OpenXR** + Meta Quest Support, **XR Interaction Toolkit 3.6.1** (LocomotionMediator, XRBodyTransformer, SnapTurnProvider, TunnelingVignette), XR Core Utils 2.6.0 |
| 입력 | **Input System 1.20.1** (`PetitParisInput.inputactions`: Quest Touch + 키보드·마우스 같은 액션) |
| 텍스트 | TextMeshPro + 나눔고딕(OFL) 동적 폰트, 게임 문장 글자 미리 채우기 |
| 개발 도구 | Claude Code (클라우드 + 로컬), **Unity MCP** (`com.coplaydev.unity-mcp`) — AI가 에디터를 Play·스크린샷·Console 확인 |

### 설계 포인트
- **쥐 크기 = 추적 공간 배율 0.1**: 세계는 실제 크기(미터) 그대로이고, `XR Origin > Camera Offset > Rat Tracking Space(scale 0.1)` 아래에 카메라·컨트롤러를 둡니다. 머리를 10cm 움직이면 쥐는 1cm 움직입니다. 앉아서 시작해도 캘리브레이션으로 눈높이를 12cm에 맞춥니다.
- **이동 경로는 하나**: `RatLocomotionController` → LocomotionMediator → XRBodyTransformer → CharacterController. 걷기·달리기·질주·점프·중력을 한 곳에서 합산해 이동이 중복되지 않습니다.
- **멀미 방지**: 카메라를 코드로 흔들거나 기울이지 않습니다(TrackedPoseDriver만 사용). 순간이동(리스폰·낙하 복귀)은 페이드로, 빠른 이동·공중에서는 비네트. 큰 낙하는 바닥에 닿기 전 페이드 후 직전 자리로 복귀(`RatFallRecovery`).
- **씬을 코드로 생성**: `PetitParisLevelBuilder`가 메뉴 한 번으로 레벨·NPC·이벤트·UI를 만듭니다. 질감(`PetitParisTextures`)·효과음·배경 음악(`PetitParisAudio`)도 코드로 생성 — 외부 에셋 라이선스 걱정이 없습니다.
- **판정은 물리 대신 거리 계산**: 발·자전거·빗자루·바게트(`FootstepHazard`)와 쥐 캡슐의 거리를 매 프레임 계산 → 쥐를 밀거나 튕기지 않습니다.
- **사람 AI**: `PedestrianPath`(절차적 걸음, 발 착지 예고) + `PedestrianAwareness`(시야각·시선 검사로 발견 → 추격 → 놓침)
- **점검 자동화**: 메뉴 **PetitParis > 4. Validate Scene**이 리그·입력·게임 연결과 Quest 빌드 설정(OpenXR 로더, Touch 프로필, ARM64, URP 값 등)을 점검합니다.

### 폴더 구조
```text
Assets/PetitParis/
  Scripts/Core       설정(RatScaleConfig, RatComfortSettings), 소리·텍스트·페이드
  Scripts/Player     쥐 리그·이동·질주·점프·웅크리기·캘리브레이션·낙하 복귀·진동·PC 시점
  Scripts/Gameplay   위험물·안전 구역·체크포인트·리스폰·보행자·추격·바게트·비둘기·수집·게임 진행
  Scripts/UI         HUD·안내 화살표·배경음·손목 패널
  Editor             씬 빌더(LevelBuilder, BuildKit, ParisKit, GameplayKit), 질감 생성, Validator, Player 설정
  Settings           입력 액션
docs/                기획·개발 문서
```

---

## 5. 알려진 문제

> 아래 "실기기 확인 필요"는 코드는 고쳤지만 Quest 2에서 다시 확인하지 않았다는 뜻입니다.

### 빌드·설정
- **OpenXR·XR Management 패키지가 저장소(`Packages/manifest.json`)에 없습니다.** 2-4절대로 에디터에서 설치해야 하고, 설치 후 생기는 `Packages/manifest.json`, `Assets/XR/`, `ProjectSettings/` 변경을 커밋해야 다른 PC에서도 재현됩니다.
- Quest 2는 OpenXR **Oculus Touch Controller Profile**이 필요합니다 (Touch Plus 프로필만 켜면 Quest 2 컨트롤러가 동작하지 않음).
- 코드를 받은 뒤에는 **메뉴 3번으로 씬을 다시 만들어야** 바뀐 레벨·설정(편안함 설정 새 기본값, 쥐 속도 등)이 반영됩니다.
- Comfort Vignette는 XRI **Starter Assets 샘플**의 프리팹을 씁니다. 없으면 비네트 없이 동작하고 Validator가 경고합니다 (Package Manager > XR Interaction Toolkit > Samples > Starter Assets Import).

### Quest 2 실기기 테스트에서 발견 → 수정함 (실기기 확인 필요)
| 증상 | 원인 | 수정 |
|---|---|---|
| 질주가 잘 안 됨 | 트리거를 누른 한 프레임에 스틱·땅·쿨다운 조건이 모두 맞아야 했음 | 입력 0.25초 기억, 스틱 없으면 바라보는 방향, 오른손 그립 추가, 진동·소리 피드백 |
| 자전거 길이 검게 깜빡임 | 바닥판 두 개가 같은 높이로 겹쳐 깊이 충돌(z-fighting) + 너무 어두운 질감 | 겹침 제거, 밝은 질감·흰 점선, 바닥판 5mm |
| 빗자루 구간 숨을 곳 부족 | 3m 동안 숨을 곳 없음 | 와인 상자·빵 바구니 추가 (구간 안 4곳) |
| 소리가 거의 안 남 | 배경음·음악 없음, 3D 소리가 너무 빨리 작아짐 | 거리 배경음 + 아코디언 왈츠, 3D 소리 거리 3배 |

자전거 길 깜빡임이 남으면 카메라 near clip(현재 0.005m) 조정을 검토할 예정입니다.

### 게임플레이·UX (실기기 확인 필요)
- AI 코드 리뷰 P1 10개(추격 밸런스, 낙하 멀미, HUD 가독성, 실패 메시지, 조작 안내, 빗자루 이벤트 재시작 등)를 고쳤지만 **Quest 2에서 다시 확인하지 않았습니다.** → 25개 이슈와 상태: [`docs/QUEST2_ISSUE_RESOLUTION.md`](docs/QUEST2_ISSUE_RESOLUTION.md)
- **72fps 미측정**: OVR Metrics Tool로 측정 예정. 블록아웃이라 가볍지만 투명 오버레이(HUD·비네트)와 그림자가 변수입니다.
- **플레이 시간**: 경로 약 34m. 목표 3~5분을 넘을 수 있어 측정이 필요합니다.
- **입체감**: 추적 공간 0.1배라 눈 사이 거리도 쥐 크기로 줄어듭니다(의도된 크기감). 사람마다 체감이 다를 수 있습니다.
- 웅크리기는 발견 거리를 줄이는 효과뿐이고, 웅크려야만 지나가는 길은 아직 없습니다. 앞발로 물건을 집는 상호작용도 없습니다.
- 시작 안내는 A(Space)를 누를 때까지 이동을 잠급니다. 자동 테스트(MCP)는 `PetitParisGameFlow.CloseIntro()`를 먼저 호출해야 합니다.
- Quest 시스템 메뉴를 열었다 돌아오면 게임이 일시 정지 메뉴(X)를 연 채로 멈추고 소리도 멈춥니다. X로 닫으면 계속됩니다.

### 그래픽
- 건물·사람·비둘기가 **기본 도형 블록아웃**입니다. 무료(CC0 등) 에셋으로 교체할 예정입니다: Quaternius 캐릭터·애니메이션, KayKit Restaurant Bits, Poly Haven 질감 등. 후보와 순서는 [`docs/NEXT_STEPS.md`](docs/NEXT_STEPS.md) 4-4.
- URP 후처리(Volume)는 Quest 2에서 비용이 커서 쓰지 않거나 최소화해야 합니다.

---

## 6. AI 활용 개발

- **Claude Code (클라우드)**: Unity 없이 C# 코드·씬 빌더·문서 작성. Unity 참조 라이브러리로 컴파일만 먼저 확인하고, 실행하지 않은 것은 "미검증"으로 표시.
- **Claude Code + Unity MCP (로컬 PC)**: AI가 에디터를 직접 Play하고 스크린샷·Console을 보며 버그 수정 (갈색 화면, 치즈 도착 판정, 완료 패널 가림 등).
- **AI 코드 리뷰**: 가상 페르소나 5명 관점의 정적 분석으로 25개 이슈 발견 → P1 10개 우선 수정.
- **실기기 테스트 → AI 분석**: Quest 2 플레이 영상에서 프레임·소리 크기를 분석해 z-fighting, 무음, 질주 입력 문제를 찾아 수정.

---

## 7. 문서

| 문서 | 내용 |
|---|---|
| [`docs/QUEST2_ISSUE_RESOLUTION.md`](docs/QUEST2_ISSUE_RESOLUTION.md) | AI 리뷰 25개 이슈와 수정 내역·상태, Quest 2 빌드 순서와 테스트 체크리스트, 실기기 테스트 추가 이슈, 5분 발표 요약 |
| [`docs/NEXT_STEPS.md`](docs/NEXT_STEPS.md) | 다음 작업, 로컬 Claude Code + Unity MCP 설정, 에셋 후보 |
| [`docs/README.md`](docs/README.md) | 개발 가이드 |
| [`docs/STAGE1.md`](docs/STAGE1.md) | 쥐 시점·캘리브레이션 설계 |
| [`docs/CREDITS.md`](docs/CREDITS.md) | 에셋 출처와 라이선스 |

## 8. 크레딧·라이선스

- 폰트: 나눔고딕 Bold — NAVER Corporation, SIL Open Font License 1.1
- 3D 모델·질감·효과음·배경 음악: 외부 에셋 없이 코드로 생성 (자체 제작)
- Comfort Vignette: Unity XR Interaction Toolkit Starter Assets 샘플 (Unity Companion License)
- 특정 영화의 캐릭터·이미지는 쓰지 않습니다. 등장인물(웨이터·셰프)은 오리지널 디자인입니다.
