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
            "작은 쥐가 되어 센강까지 가 보세요\n\n" +
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

        public const string MessageCheckpoint = "체크포인트 도착!";
        public const string MessageHidden = "잘 숨었어요! 발걸음이 지나가길 기다려요";
        public const string MessageDied = "밟힐 뻔했어요! 체크포인트로 돌아갑니다";
        public const string MessageFell = "떨어졌어요! 체크포인트로 돌아갑니다";

        public const string CompleteTitle = "도착!";
        public const string CompleteBody = "센강이 보여요. 작은 모험 성공!";
        public const string CompleteTime = "걸린 시간";
        public const string CompleteDeaths = "다시 시작한 횟수";
        public const string CompleteRestart = "A 버튼  처음부터 다시";

        public const string Seconds = "초";
        public const string Extra = "0123456789:./!?·,()+-%  분초회";

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
