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
        public const string IntroBody =
            "작은 쥐가 되어 비스트로 주방의 치즈를 찾아가세요\n빵 부스러기를 모으고, 사람 눈에 띄지 마세요!\n\n" +
            "왼쪽 스틱  이동\n" +
            "오른쪽 스틱  30도 회전\n" +
            "오른쪽 트리거  순간 질주\n" +
            "왼쪽 그립 누르고 있기  달리기\n" +
            "A  점프     B  웅크리기\n" +
            "왼쪽 메뉴 길게  시점 다시 맞추기\n" +
            "(PC: WASD 이동, Shift 달리기, 클릭 질주, 마우스 오른쪽 드래그로 둘러보기)";

        public const string HintFootsteps = "발소리가 커지면\n의자 아래로 숨어요";
        public const string HintHide = "의자 아래는 안전해요";
        public const string HintBakery = "발 사이를 질주로 빠져나가요\n오른쪽 트리거";
        public const string HintBike = "자전거 소리를 듣고 건너요";
        public const string HintGoal = "센강 쪽 출구";
        public const string HintSeine = "센강이다!\n…어디선가 치즈 냄새가?";
        public const string HintKitchenDoor = "비스트로 뒷문\n안으로 들어가요";
        public const string HintKitchenClimb = "널빤지를 타고\n식탁 위 치즈까지!";
        public const string HintPallet = "팔레트 밑에 숨어요!\n웨이터가 지나갈 때까지";
        public const string HintCrumbs = "빵 부스러기를 모아요\n황금 크루아상은 3개!";
        public const string HintCrouchStealth = "B로 웅크리면\n사람 눈에 덜 띄어요";
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
        public const string MessageDied = "밟힐 뻔했어요! 체크포인트로 돌아갑니다";
        public const string MessageFell = "떨어졌어요! 체크포인트로 돌아갑니다";
        public const string MessageCrumbsKept = "모은 빵 부스러기를 지켰어요!";
        public const string MessageCrumbsLostFormat = "빵 부스러기 {0}개를 떨어뜨렸어요";
        public const string MessageGolden = "황금 크루아상!";
        public const string MessageSpotted = "사람에게 들켰어요! 숨어요!";
        public const string MessageEscaped = "휴, 따돌렸어요!";
        public const string MessageChase = "웨이터가 빗자루를 들고 쫓아와요! 숨을 곳으로!";
        public const string MessageBaguette = "위에서 바게트가 떨어져요! 그림자를 피해요!";
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
