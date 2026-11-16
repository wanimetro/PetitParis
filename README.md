# PETIT PARIS

작은 쥐의 눈높이(12cm)로 파리 골목을 탐험하는 Meta Quest VR 게임 (Quest 2 / Quest 3, Android APK 직접 설치).
사람들의 거대한 발걸음·자전거·빗자루·떨어지는 바게트를 피해
카페 테라스 → 빵집 앞 → 좁은 골목 → 센강 강변 → **비스트로 주방**으로 들어가 **식탁 위 치즈**에 닿으면 도착이다.
가는 길에 빵 부스러기와 황금 크루아상(3개)을 모으면 별점(★1~3)이 올라간다.

| 항목 | 내용 |
|---|---|
| 엔진 | Unity 6000.6.9f1 (URP 17.6) |
| XR | OpenXR + XR Interaction Toolkit 3.6.1 (OpenXR·XR Management 패키지는 에디터에서 설치: `docs/QUEST2_ISSUE_RESOLUTION.md` 6절) |
| 기기 | Meta Quest 2 (Touch 컨트롤러) 기준으로 준비 중. Quest 3도 같은 빌드 (앉아서 플레이 가능) |
| 상태 | 블록아웃 프로토타입. PC 에디터에서 기능 확인, **Quest 실기기 미검증** |

## 시작하기
1. Unity Hub에서 이 폴더를 Unity **6000.6.9f1**로 연다 (Android Build Support 모듈 필요).
2. `docs/QUEST2_ISSUE_RESOLUTION.md` 6절 "Quest 2 빌드 순서"대로 OpenXR을 설정하고 메뉴 `PetitParis > 0, 3, 4`를 실행한다.
3. Quest 2를 USB로 연결하고 Build And Run (같은 문서 6-3).

## 조작 (Quest 2/3 Touch 컨트롤러 · PC)
| Quest | PC | 동작 |
|---|---|---|
| 왼쪽 스틱 | WASD / 방향키 | 이동 |
| 오른쪽 스틱 | , . | 30도 스냅 회전 |
| 왼쪽 그립 누르고 있기 | 왼쪽 Shift | 달리기 |
| 왼쪽 스틱을 밀면서 오른쪽 트리거 | 이동하며 클릭 / 오른쪽 Shift | 질주 (짧고 빠르게) |
| A | Space | 점프 (시작 안내 닫기, 메뉴에서 편안함 모드 전환) |
| B | C | 웅크리기 (느리지만 사람이 덜 알아챔) |
| X | Tab / H | 일시 정지 메뉴: 조작법·규칙·편안함 모드 |
| 왼쪽 ≡ 길게 | F5 | 눈높이 다시 맞추기 |
| - | 마우스 오른쪽 드래그 | 둘러보기 (헤드셋이 없을 때만) |

## 문서
- `docs/QUEST2_ISSUE_RESOLUTION.md`: AI 리뷰 이슈 수정 보고서, Quest 2 테스트 계획과 빌드 순서
- `docs/NEXT_STEPS.md`: 다음 작업과 알려진 문제
- `docs/README.md`: 개발 가이드, `docs/STAGE1.md`: 쥐 시점 설계, `docs/CREDITS.md`: 에셋 출처
