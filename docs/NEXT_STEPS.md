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
4. Unity 메뉴 **Window → MCP for Unity → Configure All Detected Clients**를 실행한다 (Claude Code 등록).
5. PowerShell에서 프로젝트 폴더로 이동해 Claude Code를 실행한다.
   ```powershell
   cd "<PetitParis 폴더 경로>"
   claude
   ```
6. Claude Code에서 `/mcp`를 입력해 Unity MCP가 connected인지 확인한다.
7. 첫 요청 예시:
   > "docs/NEXT_STEPS.md를 읽고, PetitParisVR 씬을 Play해서 스크린샷과 Console을 확인한 뒤 '알려진 문제'부터 고쳐줘."

## 2. 알려진 문제 (먼저 고칠 것)
| 증상 | 메모 |
|---|---|
| 화면 전체가 갈색 한 색으로 보임 (사용자 영상, 2026-11-14) | 카메라가 지형 밖이나 아래로 나가 하늘(Skybox) 아래쪽만 보이는 것으로 추정. `RatDesktopLook.SetPitch`(Tracking Space 회전과 눈 위치 보정), 낙하 후 리스폰, 캘리브레이션 순서를 Play 중 확인한다. 카메라 world 위치와 XR Origin 위치를 로그로 찍어 볼 것 |
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

## 5. 작업 순서 제안 (로컬 세션)
1. 알려진 문제 수정 (Play + 스크린샷으로 확인)
2. 레포를 Private으로 바꾼 뒤 배경 에셋을 정하고 임포트 → 지금 동선(벽·의자·보행자 경로·체크포인트)에 맞춰 건물 교체. `PetitParisLevelBuilder`는 에셋 프리팹을 배치하도록 바꾸거나, 씬을 직접 편집하는 방식으로 전환할지 결정
3. 사람 모델 교체 (CC0 Quaternius부터)
4. 빵 부스러기 + 별점
5. 추격 이벤트
6. 식당 주방 구간
7. Quest 3 빌드, 72fps 확인 (OVR Metrics Tool), 최적화
