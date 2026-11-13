using System.Text;
using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 왼손 손목 위의 디버그 패널. Quest Link 없이 실기기에서 바로 확인하기 위한 용도.
    /// 추적 상태, 실제 머리 높이, 쥐 눈높이, 배율, 컨트롤러 입력값, FPS를 표시한다.
    /// (한글 TMP 폰트 에셋이 아직 없어 영문/숫자로 표시한다)
    /// </summary>
    [DisallowMultipleComponent]
    public class RatDebugPanel : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] RatInputReader input;
        [SerializeField] RatEyeCalibrator calibrator;
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatLocomotionController locomotion;
        [SerializeField] RatDashController dash;
        [SerializeField] RatCrouchController crouch;

        [SerializeField] bool showPanel = true;
        [SerializeField, Range(0.05f, 1f)] float refreshInterval = 0.2f;

        float nextRefresh;
        float smoothedDelta = 1f / 72f;
        readonly StringBuilder sb = new StringBuilder(512);

        void Awake()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            if (input == null) input = GetComponentInParent<RatInputReader>();
            if (calibrator == null) calibrator = GetComponentInParent<RatEyeCalibrator>();
            if (rig == null) rig = GetComponentInParent<RatScaleRig>();
            if (rig != null)
            {
                if (locomotion == null) locomotion = rig.GetComponentInChildren<RatLocomotionController>(true);
                if (dash == null) dash = rig.GetComponentInChildren<RatDashController>(true);
                if (crouch == null) crouch = rig.GetComponentInChildren<RatCrouchController>(true);
            }
            if (text == null) Debug.LogWarning("[PetitParis] RatDebugPanel: TMP_Text가 없습니다.", this);
        }

        void OnValidate()
        {
            if (text != null) text.enabled = showPanel;
        }

        void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.1f);
            if (text == null) return;
            text.enabled = showPanel;
            if (!showPanel || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshInterval;
            text.SetText(Build());
        }

        string Build()
        {
            sb.Clear();
            sb.Append("<b>PETIT PARIS  debug</b>\n");

            if (rig != null && rig.IsValid)
            {
                var cfg = rig.Config;
                Vector3 eyeLocal = rig.transform.InverseTransformPoint(rig.EyeCamera.transform.position);
                sb.Append("Eye height: ").Append((eyeLocal.y * 100f).ToString("F1")).Append(" cm (target ")
                  .Append((cfg.targetEyeHeight * 100f).ToString("F0")).Append(")\n");
                sb.Append("World scale: x").Append(cfg.Magnification.ToString("F1"))
                  .Append("  (space ").Append(rig.TrackingSpace.localScale.x.ToString("F3")).Append(")\n");
            }
            else
            {
                sb.Append("<color=#ff6060>RatScaleRig invalid</color>\n");
            }

            if (input != null)
            {
                Vector3 raw = input.RawHeadPosition;
                sb.Append("HMD ").Append(State(input.HeadTrackingState))
                  .Append("  raw y ").Append(raw.y.ToString("F2")).Append(" m\n");
                sb.Append("L ctrl ").Append(State(input.LeftTrackingState))
                  .Append("   R ctrl ").Append(State(input.RightTrackingState)).Append('\n');
                Vector2 m = input.Move, t = input.Turn;
                sb.Append("Move ").Append(m.x.ToString("F2")).Append(", ").Append(m.y.ToString("F2"))
                  .Append("  Turn ").Append(t.x.ToString("F2")).Append('\n');
                sb.Append("Dash ").Append(Flag(input.DashHeld))
                  .Append(" Jump ").Append(Flag(input.JumpHeld))
                  .Append(" Crouch ").Append(Flag(input.CrouchHeld))
                  .Append(" Use ").Append(Flag(input.InteractHeld)).Append('\n');
                sb.Append("Calib (hold Menu/Y 1s) ").Append(Flag(input.CalibrateHeld)).Append('\n');
            }
            else
            {
                sb.Append("<color=#ff6060>RatInputReader missing</color>\n");
            }

            if (locomotion != null)
            {
                sb.Append("Speed ").Append((locomotion.PlanarSpeed * 100f).ToString("F1")).Append(" cm/s  ")
                  .Append(locomotion.IsGrounded ? "ground" : "<color=#ffd060>air</color>").Append('\n');
            }
            if (dash != null || crouch != null)
            {
                if (dash != null) sb.Append("Dash ").Append(dash.State).Append(' ').Append(dash.CooldownRemaining.ToString("0.0")).Append("s  ");
                if (crouch != null) sb.Append(crouch.IsCrouched ? "<color=#7CFC7C>CROUCH</color>" : "stand");
                sb.Append('\n');
            }

            if (calibrator != null)
            {
                sb.Append("Calibrated: ").Append(calibrator.CalibrationCount)
                  .Append("x  head ").Append(calibrator.LastRawHeadHeight.ToString("F2")).Append(" m\n");
            }

            float fps = 1f / Mathf.Max(0.0001f, smoothedDelta);
            sb.Append("FPS ").Append(fps.ToString("F0"));
            return sb.ToString();
        }

        static string State(int flags)
        {
            bool p = (flags & 1) != 0, r = (flags & 2) != 0;
            if (p && r) return "<color=#7CFC7C>P+R</color>";
            if (r) return "<color=#ffd060>R only</color>";
            if (p) return "<color=#ffd060>P only</color>";
            return "<color=#ff6060>--</color>";
        }

        static string Flag(bool on) => on ? "<color=#7CFC7C>1</color>" : "0";
    }
}
