using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 시야 아래쪽에 떠 있는 작은 안내판: 빵 점수 + 짧은 메시지("들켰어요!", "체크포인트 도착!" 등).
    /// - 머리에 고정하지 않고, 바라보는 방향을 천천히 따라온다 (lazy follow → 멀미 적음)
    /// - PC 모드에서도 보이도록 손목 패널과 같은 내용을 크게 보여준다
    /// - 완료 화면에서는 숨긴다
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

        [SerializeField, Range(0.05f, 1f)] float distance = 0.26f;
        [Tooltip("눈높이보다 얼마나 아래에 둘지 (월드 m).")]
        [SerializeField, Range(-0.2f, 0.2f)] float drop = 0.055f;
        [SerializeField, Range(0.5f, 20f)] float followSharpness = 4f;
        [Tooltip("시선이 이 각도 이상 벗어나야 따라오기 시작한다.")]
        [SerializeField, Range(0f, 60f)] float deadZoneDegrees = 18f;

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
            bool show = gameFlow == null || gameFlow.CurrentPhase != PetitParisGameFlow.Phase.Completed;
            if (scoreText != null) scoreText.gameObject.SetActive(show && crumbs != null);
            if (!show)
            {
                if (messageText != null) messageText.gameObject.SetActive(false);
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
