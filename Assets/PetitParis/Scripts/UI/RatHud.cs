using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 시야 위쪽에 떠 있는 작은 안내판: 짧은 메시지("들켰어요!", "체크포인트 도착!" 등) + 빵 점수.
    /// - 머리에 고정하지 않고, 바라보는 방향을 천천히 따라온다 (lazy follow → 멀미 적음)
    /// - 눈높이보다 조금 위(약 8°)에 둔다: 아래쪽의 안내 화살표와 겹치지 않고, 바닥·널빤지에 가려질 일이 없다 (ISSUE-04)
    ///   글자와 배경은 빌더에서 깊이 검사 없는 Overlay 재질로 그린다 → 벽·물체에 가려지지 않음
    /// - 어두운 배경은 메시지가 있을 때만 보인다. 점수 줄은 작게
    /// - 완료 화면과 메뉴에서는 숨긴다
    /// 거리 단위는 월드 미터 (0.25 = 체감 2.5m).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)] // 카메라·시점 회전(RatDesktopLook 200) 뒤
    public class RatHud : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] PetitParisGameFlow gameFlow;
        [SerializeField] CrumbCollection crumbs;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text messageText;
        [Tooltip("메시지 뒤 어두운 배경 (메시지가 있을 때만 켠다).")]
        [SerializeField] GameObject backdrop;

        [SerializeField, Range(0.05f, 1f)] float distance = 0.25f;
        [Tooltip("눈높이보다 얼마나 아래에 둘지 (월드 m). 음수 = 눈보다 위. -0.035 = 시선 위 약 8°: 화살표(눈 아래 약 12°)와 겹치지 않는다.")]
        [SerializeField, Range(-0.2f, 0.2f)] float drop = -0.035f;
        [SerializeField, Range(0.5f, 20f)] float followSharpness = 4f;
        [Tooltip("시선이 이 각도 이상 벗어나야 따라오기 시작한다.")]
        [SerializeField, Range(0f, 60f)] float deadZoneDegrees = 10f;

        Vector3 anchorForward;
        bool placed;
        bool following;
        int lastScore = -1, lastGolden = -1;
        string lastMessage;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (crumbs == null) crumbs = FindAnyObjectByType<CrumbCollection>();
        }

        void LateUpdate()
        {
            if (rig == null || rig.EyeCamera == null) return;
            bool show = gameFlow == null || (gameFlow.CurrentPhase != PetitParisGameFlow.Phase.Completed && !gameFlow.IsMenuOpen);
            if (scoreText != null) scoreText.gameObject.SetActive(show && crumbs != null);
            if (!show)
            {
                if (messageText != null) messageText.gameObject.SetActive(false);
                if (backdrop != null) backdrop.SetActive(false);
                return;
            }

            Transform cam = rig.EyeCamera.transform;
            Vector3 look = cam.forward;
            look.y = 0f;
            if (look.sqrMagnitude < 1e-4f) look = rig.transform.forward;
            look.Normalize();
            if (!placed) { anchorForward = look; placed = true; }
            float off = Vector3.Angle(anchorForward, look);
            if (off > deadZoneDegrees) following = true;
            else if (off < 2f) following = false;
            if (following)
                anchorForward = Vector3.Slerp(anchorForward, look, 1f - Mathf.Exp(-followSharpness * Time.deltaTime)).normalized;

            Vector3 target = cam.position + anchorForward * distance + Vector3.down * drop;
            transform.SetPositionAndRotation(target, Quaternion.LookRotation(anchorForward, Vector3.up));

            if (scoreText != null && crumbs != null && (crumbs.Score != lastScore || crumbs.GoldenCount != lastGolden))
            {
                lastScore = crumbs.Score;
                lastGolden = crumbs.GoldenCount;
                scoreText.text = $"{PetitParisText.StatusCrumbs} {crumbs.Score}   {PetitParisText.StatusGolden} {crumbs.GoldenCount}/{Mathf.Max(crumbs.GoldenTotal, 0)}";
            }

            if (messageText != null && gameFlow != null)
            {
                bool hasMessage = !string.IsNullOrEmpty(gameFlow.CurrentMessage) && Time.time < gameFlow.MessageUntil;
                messageText.gameObject.SetActive(hasMessage);
                if (backdrop != null && backdrop.activeSelf != hasMessage) backdrop.SetActive(hasMessage);
                if (hasMessage && gameFlow.CurrentMessage != lastMessage)
                {
                    lastMessage = gameFlow.CurrentMessage;
                    messageText.text = lastMessage;
                }
                if (!hasMessage) lastMessage = null;
            }
        }
    }
}
