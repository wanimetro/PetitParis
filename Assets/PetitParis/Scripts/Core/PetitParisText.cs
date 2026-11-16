using System.Reflection;
using System.Text;

namespace PetitParis
{
    /// <summary>
    /// 화면에 나오는 한국어 문장 모음.
    /// 에디터 빌더가 이 문자열들로 한글 TMP 폰트 에셋에 글자를 미리 채워 넣는다 (Quest에서 글자 누락 방지).
    /// 새 문장을 추가할 때는 여기에 const로 추가한다.
    /// </summary>
    public static class PetitParisText
    {
        public const string Title = "PETIT PARIS";
        /// <summary>시작 안내 (Quest 2/3 Touch 컨트롤러 기준). A(PC: Space)를 눌러야 닫힌다.</summary>
        public const string IntroBody =
            "작은 쥐가 되어 비스트로 주방의 치즈를 찾아가세요\n빵 부스러기를 모으고, 사람 눈에 띄지 마세요!\n\n" +
            ControlsBody + "\n\n" +
            "<b>A 버튼 (PC: Space)  시작하기</b>";

        /// <summary>조작법 (시작 안내와 X 메뉴에서 같이 쓴다).</summary>
        public const string ControlsBody =
            "왼쪽 스틱  이동      오른쪽 스틱  30도 회전\n" +
            "왼쪽 그립 누르고 있기  달리기\n" +
            "왼쪽 스틱을 밀면서 오른쪽 트리거  질주 (짧고 빠르게)\n" +
            "A  점프      B  웅크리기 (느리지만 덜 들켜요)\n" +
            "X  조작법·편안함 모드      왼쪽 ≡ 길게  시점 다시 맞추기\n" +
            "<size=80%>PC: WASD 이동 · Shift 달리기 · 클릭 질주 · Space 점프 · C 웅크리기\n" +
            "Tab 메뉴 · 마우스 오른쪽 드래그 둘러보기</size>";

        public const string MenuTitle = "일시 정지 · 조작법";
        public const string MenuRules =
            "규칙: 사람 발·자전거·빗자루·바게트에 닿으면 체크포인트로 돌아가요\n" +
            "들키면 초록 표시가 있는 곳(의자·벤치·팔레트 밑)에 숨어요\n" +
            "★ 도착  ★ 빵 부스러기 60% 이상  ★ 황금 크루아상 3개";
        public const string MenuComfortOn = "편안함 모드: 켜짐 (느린 이동 · 비네트 강화 · 점프 끔)";
        public const string MenuComfortOff = "편안함 모드: 꺼짐";
        public const string MenuFooter = "A (PC: Space)  편안함 모드 전환      X (PC: Tab)  닫기";

        public const string HintFootsteps = "발소리가 커지면\n의자 아래로 숨어요";
        public const string HintHide = "초록 표시가 있는 의자 아래는\n안전해요";
        public const string HintBakery = "발이 내려오기 전에 지나가요\n스틱 + 오른쪽 트리거 = 질주";
        public const string HintBike = "자전거 소리를 듣고 건너요";
        public const string HintGoal = "센강 쪽 출구";
        public const string HintSeine = "센강이다!\n…어디선가 치즈 냄새가?";
        public const string HintKitchenDoor = "비스트로 뒷문\n안으로 들어가요";
        public const string HintKitchenClimb = "널빤지를 타고\n식탁 위 치즈까지!";
        public const string HintStart = "벽 틈은 안전해요\n여기서 천천히 연습해요";
        public const string HintAwning = "차양 밑엔 바게트가 안 떨어져요\n요리사 발은 조심!";
        public const string HintPallet = "팔레트 밑에 숨어요!\n웨이터가 지나갈 때까지";
        public const string HintCrumbs = "빵 부스러기를 모아요 (60% → ★)\n황금 크루아상 3개 → ★";
        public const string HintCrouchStealth = "B로 웅크리면 느리지만\n사람이 가까이 와야 알아채요";
        public const string SignBistro = "BISTROT\nLE PETIT FROMAGE";
        public const string SignKitchen = "CUISINE";
        public const string SignCafe = "카페";
        public const string SignBakery = "빵집";
        public const string SignCafeLatin = "CAFÉ";
        public const string SignBakeryLatin = "BOULANGERIE";
        public const string SignCafeName = "CAFÉ DE LA BUTTE";
        public const string SignFlowers = "FLEURS";
        public const string SignBooks = "LIBRAIRIE";
        public const string StreetName = "RUE DU PETIT-RAT\n18e Arr.";

        public const string StatusExplore = "탐험 중";
        public const string StatusDanger = "위험! 발걸음 주의";
        public const string StatusSafe = "안전";
        public const string StatusHidden = "숨는 중";
        public const string StatusCrouch = "웅크림";
        public const string StatusDashReady = "질주 가능";
        public const string StatusDashing = "질주!";
        public const string StatusRunning = "달리는 중";
        public const string StatusDashCooldown = "질주 대기";
        public const string StatusCannotStand = "좁아서 일어설 수 없어요";
        public const string StatusCheckpoint = "체크포인트";
        public const string StatusRespawning = "다시 시작";
        public const string StatusCrumbs = "빵";
        public const string StatusGolden = "황금";
        public const string StatusSpotted = "들켰다! 숨어요!";

        public const string MessageCheckpoint = "체크포인트 도착!";
        public const string MessageHidden = "잘 숨었어요! 발걸음이 지나가길 기다려요";
        public const string MessageDied = "앗! 체크포인트로 돌아갑니다";
        // 실패 원인별 메시지 (FootstepHazard.hazardName → 문장). 마지막 줄은 짧은 재도전 힌트
        public const string FailShoe = "사람 발에 밟혔어요!\n진한 그림자가 생기면 피해요";
        public const string FailBike = "자전거에 치였어요!\n벨 소리가 멀어진 뒤 건너요";
        public const string FailBroom = "빗자루에 쓸렸어요!\n팔레트 밑이나 상자 안에 숨어요";
        public const string FailBaguette = "바게트에 맞았어요!\n차양 밑으로 다니면 안전해요";
        public const string FailFell = "떨어졌어요!\n체크포인트로 돌아갑니다";
        public const string HazardShoe = "신발";
        public const string HazardBike = "자전거";
        public const string HazardBroom = "빗자루";
        public const string HazardBaguette = "바게트";
        public const string MessageFallRecovered = "떨어질 뻔! 올라가던 자리로 돌아왔어요";
        public const string MessageHideHintFormat = "숨을 곳: {0} (화살표 방향)";
        public const string MessagePaused = "일시 정지";
        public const string MessageComfortOn = "편안함 모드를 켰어요";
        public const string MessageComfortOff = "편안함 모드를 껐어요";
        /// <summary>안전 구역 이름 (빌더에서 쓰는 문자열. 폰트 미리 채우기용으로 여기 모아 둔다).</summary>
        public const string ZoneNames = "벽 틈 의자 아래 벤치 아래 팔레트 밑 종이 상자 안 조리대 밑 식탁 밑";
        public const string MessageFell = "떨어졌어요! 체크포인트로 돌아갑니다";
        public const string MessageCrumbsKept = "모은 빵 부스러기를 지켰어요!";
        public const string MessageCrumbsLostFormat = "빵 부스러기 {0}개를 떨어뜨렸어요";
        public const string MessageGolden = "황금 크루아상!";
        public const string MessageSpotted = "사람에게 들켰어요! 숨어요!";
        public const string MessageEscaped = "휴, 따돌렸어요!";
        public const string MessageChase = "웨이터가 빗자루를 들고 쫓아와요! 숨을 곳으로!";
        public const string MessageBaguette = "차양 위에서 바게트가 굴러떨어져요!\n차양 밑엔 안 떨어져요 (요리사 발은 조심)";
        public const string MessageBaguetteNearMiss = "아슬아슬! 바게트를 피했어요";
        public const string MessageBaguetteDone = "빵집 주인이 바게트 바구니를 치웠어요. 휴~";
        public const string MessageKitchen = "주방이다! 요리사 발밑을 조심해요";
        public const string MessageSeine = "센강이 보여요! …어디선가 치즈 냄새가 나요";

        public const string ShoutNotice = "!";
        public const string ShoutRat = "앗, 쥐다!";
        public const string ShoutLost = "어디 갔지?";
        public const string ShoutChef = "내 주방에 쥐라니!";
        public const string ShoutWaiter = "저리 가!";

        public const string CompleteTitle = "도착!";
        public const string CompleteBody = "치즈를 찾았어요! 작은 모험 성공!";
        public const string CompleteScore = "빵 부스러기";
        public const string CompleteGolden = "황금 크루아상";
        public const string CompleteStarRule = "★ 도착  ★ 부스러기 60% 이상  ★ 황금 크루아상 3개";
        public const string StarFull = "★";
        public const string StarEmpty = "☆";
        public const string CompleteTime = "걸린 시간";
        public const string CompleteDeaths = "다시 시작한 횟수";
        public const string CompleteRestart = "A 버튼  처음부터 다시";

        public const string Seconds = "초";
        public const string Extra = "0123456789:./!?·,()+-%  분초회개점…★☆";

        /// <summary>위 모든 문장의 글자를 합친다 (폰트 에셋 사전 채우기용).</summary>
        public static string AllCharacters()
        {
            var sb = new StringBuilder();
            foreach (var field in typeof(PetitParisText).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                    sb.Append((string)field.GetRawConstantValue());
            }
            return sb.ToString();
        }
    }
}
