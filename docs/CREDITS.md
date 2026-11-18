# CREDITS

| 에셋 | 출처 | 라이선스 | 위치 |
|---|---|---|---|
| 나눔고딕 Bold (NanumGothicBold.ttf) | NAVER Corporation, Ubuntu `fonts-nanum` 패키지 | SIL Open Font License 1.1 (전문: `Assets/PetitParis/UI/Fonts/NanumGothic-OFL.txt`) | `Assets/PetitParis/UI/Fonts/` |
| 효과음 (발소리, 자전거 바퀴·벨, 체크포인트, 실패, 도착, 줍기, 경고, 바람, 날갯짓, 빗자루, 추격 음악) | 외부 음원 없음. `PetitParisAudio.cs`가 실행 중 코드로 합성 | 자체 제작 | `Assets/PetitParis/Scripts/Core/PetitParisAudio.cs` |
| 배경음 (거리 소리)·배경 음악 (아코디언 왈츠 8마디) | 외부 음원 없음. 멜로디·화음은 이 프로젝트용으로 새로 작곡, `PetitParisAudio.cs`가 코드로 합성 | 자체 제작 | `PetitParisAudio.CityAmbience`, `MusetteLoop` |
| Comfort Vignette 프리팹 | Unity XR Interaction Toolkit Starter Assets 샘플 (프로젝트에 이미 포함, 빌더가 버전 폴더와 상관없이 이름으로 찾음) | Unity Companion License | `Assets/Samples/XR Interaction Toolkit/<버전>/Starter Assets/TunnelingVignette/` |
| 3D 모델 | 없음. 환경·NPC·쥐 앞발은 Unity 기본 도형과 코드로 만든 메시 | 자체 제작 | `Assets/PetitParis/Editor/PetitParisParisKit.cs`, `PetitParisBuildKit.cs` |
| 질감 (라탄, 줄무늬, 창문, 강물 등 아래 외부 질감으로 바꾸지 않은 것) | 외부 이미지 없음. `PetitParisTextures.cs`가 코드로 생성 | 자체 제작 | `Assets/PetitParis/Textures/Generated/` (빌더 실행 시 생성) |

## 외부 에셋 (2026-11-14 추가, 모두 무료)
레포는 Private. 다운로드 원본 zip은 레포에 넣지 않고, 쓰는 파일만 `Assets/PetitParis/ThirdParty/<출처>/`에 둔다. 질감은 가져올 때 최대 1K (`PetitParisThirdPartyImport`).

| 에셋 | 작가 | 라이선스 | 출처 URL | 위치 |
|---|---|---|---|---|
| 질감 `cobblestone_square` (차도 포석) | Rob Tuytel | CC0 | https://polyhaven.com/a/cobblestone_square | `ThirdParty/PolyHaven/cobblestone_square/` |
| 질감 `rectangular_paving` (보도·테라스) | Dimitrios Savva | CC0 | https://polyhaven.com/a/rectangular_paving | `ThirdParty/PolyHaven/rectangular_paving/` |
| 질감 `white_sandstone_blocks_02` (석회암 벽) | Rob Tuytel | CC0 | https://polyhaven.com/a/white_sandstone_blocks_02 | `ThirdParty/PolyHaven/white_sandstone_blocks_02/` |
| 질감 `yellow_plaster` (회반죽 벽) | Rob Tuytel | CC0 | https://polyhaven.com/a/yellow_plaster | `ThirdParty/PolyHaven/yellow_plaster/` |
| 질감 `painted_plaster_wall` (회반죽 벽) | Amal Kumar | CC0 | https://polyhaven.com/a/painted_plaster_wall | `ThirdParty/PolyHaven/painted_plaster_wall/` |
| 질감 `grey_roof_01` (회색 지붕) | Rob Tuytel | CC0 | https://polyhaven.com/a/grey_roof_01 | `ThirdParty/PolyHaven/grey_roof_01/` |
| 질감 `brown_planks_05` (나무) | Rob Tuytel | CC0 | https://polyhaven.com/a/brown_planks_05 | `ThirdParty/PolyHaven/brown_planks_05/` |
| 질감 `checkered_pavement_tiles` (주방 바닥) | Amal Kumar | CC0 | https://polyhaven.com/a/checkered_pavement_tiles | `ThirdParty/PolyHaven/checkered_pavement_tiles/` |
| Universal Base Characters (Standard): 남녀 몸체, 머리카락·눈썹 | Quaternius | CC0 | https://quaternius.com/packs/universalbasecharacters.html | `ThirdParty/Quaternius/UniversalBaseCharacters/` |
| Universal Animation Library (Standard): 걷기·대기 등 | Quaternius | CC0 | https://quaternius.com/packs/universalanimationlibrary.html | `ThirdParty/Quaternius/UniversalAnimationLibrary/` |
| Downtown City MegaKit (Standard, 일부): 슬레이트 지붕·지붕창, 코니스, 창틀, 문틀, 문 | Quaternius | CC0 (`License_Standard.txt`) | https://quaternius.com/packs/downtowncitymegakit.html | `ThirdParty/Quaternius/DowntownCityMegaKit/` |
| KayKit Restaurant Bits 1.0 (FREE): 주방 소품, 음식 | Kay Lousberg (www.kaylousberg.com) | CC0 (`License.txt`) | https://kaylousberg.itch.io/restaurant-bits | `ThirdParty/KayKit/RestaurantBits/` |
| 센강 실사 영상 (선택, 강 건너 배경) | Mixkit — **사용한 영상 제목을 적는다** | Mixkit Stock Video **Free** License (상업 이용 가능·출처 표기 의무 없음·원본 파일 재배포 금지. 영상마다 Free/Restricted 확인) | **사용한 영상 URL을 적는다** (mixkit.co/free-stock-video/…) | `ThirdParty/Backdrops/SeineVideo.mp4` (.gitignore, 레포에 안 올림) |
