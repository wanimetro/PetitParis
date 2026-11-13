using System.Text;
using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 왼손 손목의 간단한 상태 표시 (한국어, 3~4줄).
    /// 체크포인트 수·시간 / 안전·위험 상태 / 질주·웅크림 / 짧은 메시지.
    /// 복잡한 HUD 대신 손목을 볼 때만 보이는 방식.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatStatusPanel : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] PetitParisGameFlow gameFlow;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] RatDashController dash;
        [SerializeField] RatCrouchController crouch;
        [SerializeField] RatRespawn respawn;
        [SerializeField, Range(0.05f, 1f)] float refreshInterval = 0.15f;

        readonly StringBuilder sb = new StringBuilder(256);
        float nextRefresh;

        void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (checkpoints == null) checkpoints = FindAnyObjectByType<CheckpointManager>();
            if (danger == null) danger = GetComponentInParent<RatDangerDetector>();
            if (dash == null) dash = GetComponentInParent<RatScaleRig>()?.GetComponentInChildren<RatDashController>(true);
            if (crouch == null) crouch = GetComponentInParent<RatScaleRig>()?.GetComponentInChildren<RatCrouchController>(true);
            if (respawn == null) respawn = GetComponentInParent<RatRespawn>();
        }

        void Update()
        {
            if (text == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshInterval;
            sb.Clear();

            // 1줄: 체크포인트 / 시간
            if (checkpoints != null)
                sb.Append(PetitParisText.StatusCheckpoint).Append(' ').Append(checkpoints.ReachedCount).Append('/').Append(checkpoints.Count);
            if (gameFlow != null)
            {
                int t = Mathf.FloorToInt(gameFlow.ElapsedTime);
                sb.Append("   ").Append(t / 60).Append(':').Append((t % 60).ToString("00"));
            }
            sb.Append('\n');

            // 2줄: 안전 / 위험
            if (respawn != null && respawn.IsRespawning)
                sb.Append("<color=#ffb060>").Append(PetitParisText.StatusRespawning).Append("</color>");
            else if (danger != null && danger.IsSafe)
                sb.Append("<color=#7CFC7C>").Append(PetitParisText.StatusSafe).Append(" · ").Append(danger.CurrentSafeZone.ZoneName).Append("</color>");
            else if (danger != null && danger.DangerLevel > 0.35f)
                sb.Append("<color=#ff6060>").Append(PetitParisText.StatusDanger).Append("</color>");
            else
                sb.Append(PetitParisText.StatusExplore);
            sb.Append('\n');

            // 3줄: 질주 / 웅크림
            if (dash != null)
            {
                if (dash.IsDashing) sb.Append(PetitParisText.StatusDashing);
                else if (dash.CooldownRemaining > 0f) sb.Append(PetitParisText.StatusDashCooldown).Append(' ').Append(dash.CooldownRemaining.ToString("0.0")).Append(PetitParisText.Seconds);
                else sb.Append(PetitParisText.StatusDashReady);
            }
            if (crouch != null && crouch.IsCrouched)
                sb.Append("   ").Append(crouch.IsWaitingToStand ? PetitParisText.StatusCannotStand : PetitParisText.StatusCrouch);

            // 4줄: 메시지
            if (gameFlow != null && !string.IsNullOrEmpty(gameFlow.CurrentMessage) && Time.time < gameFlow.MessageUntil)
                sb.Append("\n<color=#ffe080>").Append(gameFlow.CurrentMessage).Append("</color>");

            text.SetText(sb.ToString());
        }
    }
}
