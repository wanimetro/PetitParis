using UnityEngine;
using UnityEngine.XR;

namespace PetitParis
{
    /// <summary>
    /// 컨트롤러 진동 (AI 리뷰 ISSUE-15). Quest 2/3 Touch 컨트롤러.
    /// - 발걸음·자전거가 가까우면(위험도) 두 손에 짧게 톡톡 (가까울수록 세고 잦게)
    /// - 실패하면 두 손에 한 번 길게, 사람에게 들키면 두 손에 짧게
    /// - 부스러기를 주우면 오른손에 아주 약하게
    /// UnityEngine.XR.InputDevices의 SendHapticImpulse를 쓴다 (OpenXR 플러그인이 지원, 패키지 추가 없음).
    /// 장치가 없으면(PC 모드) 아무 일도 하지 않는다. Quest 실기기에서 세기 확인 필요.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatHaptics : MonoBehaviour
    {
        [SerializeField] bool enableHaptics = true;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] RatRespawn respawn;
        [SerializeField] RatFallRecovery fallRecovery;
        [SerializeField] CrumbCollection crumbs;

        [Tooltip("위험도 이상에서 톡톡 진동을 시작한다.")]
        [SerializeField, Range(0f, 1f)] float dangerThreshold = 0.45f;
        [SerializeField, Range(0f, 1f)] float maxDangerAmplitude = 0.35f;

        float nextDangerPulse;

        void Awake()
        {
            if (danger == null) danger = GetComponent<RatDangerDetector>();
            if (respawn == null) respawn = GetComponent<RatRespawn>();
            if (fallRecovery == null) fallRecovery = GetComponent<RatFallRecovery>();
        }

        void Start()
        {
            if (crumbs == null) crumbs = FindAnyObjectByType<CrumbCollection>();
            if (respawn != null) respawn.Died += OnDied;
            if (fallRecovery != null) fallRecovery.Recovered += OnRecovered;
            if (crumbs != null) crumbs.Picked += OnPicked;
            PedestrianAwareness.AnyNoticed += OnNoticed;
        }

        void OnDestroy()
        {
            if (respawn != null) respawn.Died -= OnDied;
            if (fallRecovery != null) fallRecovery.Recovered -= OnRecovered;
            if (crumbs != null) crumbs.Picked -= OnPicked;
            PedestrianAwareness.AnyNoticed -= OnNoticed;
        }

        void Update()
        {
            if (!enableHaptics || danger == null || (respawn != null && respawn.IsRespawning)) return;
            float d = danger.DangerLevel;
            if (d < dangerThreshold || danger.IsSafe || Time.unscaledTime < nextDangerPulse) return;
            float k = Mathf.InverseLerp(dangerThreshold, 1f, d);
            Both(Mathf.Lerp(0.1f, maxDangerAmplitude, k), 0.04f);
            nextDangerPulse = Time.unscaledTime + Mathf.Lerp(0.6f, 0.2f, k);
        }

        void OnDied(string reason) => Both(0.8f, 0.3f);
        void OnRecovered() => Both(0.3f, 0.1f);
        void OnNoticed(PedestrianAwareness who) => Both(0.5f, 0.12f);
        void OnPicked(CrumbPickup c) => Pulse(XRNode.RightHand, c.IsGolden ? 0.5f : 0.15f, c.IsGolden ? 0.15f : 0.03f);

        void Both(float amplitude, float duration)
        {
            Pulse(XRNode.LeftHand, amplitude, duration);
            Pulse(XRNode.RightHand, amplitude, duration);
        }

        void Pulse(XRNode node, float amplitude, float duration)
        {
            if (!enableHaptics) return;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;
            if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }
    }
}
