using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 쥐의 위험·안전 상태를 매 프레임 판정한다 (XR Origin에 붙음).
    /// - 안전 구역(RatSafeZone) 안인지
    /// - 가장 가까운 위험 물체(FootstepHazard)와의 거리 → 위험도 0~1
    /// - 위험 물체에 닿았고 안전 구역 밖이면 RatRespawn.Kill → 페이드 후 체크포인트
    /// 물리 밀어내기 없이 거리 계산만 하므로 카메라가 튕기지 않는다.
    /// LateUpdate에서 실행해 NPC 발 위치가 갱신된 뒤 판정한다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class RatDangerDetector : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatCrouchController crouch;
        [SerializeField] RatRespawn respawn;

        [Tooltip("위험도 변화의 부드러움 (클수록 빠르게 반응).")]
        [SerializeField, Range(1f, 30f)] float dangerSmoothing = 10f;

        [Tooltip("이 위험도 이상일 때 안전 구역에 들어가면 '잘 숨었다' 이벤트.")]
        [SerializeField, Range(0f, 1f)] float hideEventDanger = 0.25f;

        public float DangerLevel { get; private set; }
        public float RawDanger { get; private set; }
        public bool IsSafe => CurrentSafeZone != null;
        public RatSafeZone CurrentSafeZone { get; private set; }
        public FootstepHazard NearestHazard { get; private set; }
        public float NearestHazardDistance { get; private set; } = float.MaxValue;
        public Vector3 BodyCenter { get; private set; }

        public event Action<RatSafeZone> EnteredSafeZone;
        public event Action<RatSafeZone> ExitedSafeZone;
        /// <summary>위험한 순간에 안전 구역으로 숨었을 때.</summary>
        public event Action<RatSafeZone> HidFromDanger;
        public event Action<FootstepHazard> HitByHazard;

        float recentDanger;

        void Awake()
        {
            if (rig == null) rig = GetComponent<RatScaleRig>();
            if (crouch == null) crouch = GetComponentInChildren<RatCrouchController>(true);
            if (respawn == null) respawn = GetComponent<RatRespawn>();
        }

        void LateUpdate()
        {
            if (rig == null || rig.Body == null) return;
            var body = rig.Body;
            Vector3 up = body.transform.up;
            Vector3 center = body.transform.TransformPoint(body.center);
            float radius = body.radius;
            float half = Mathf.Max(0f, body.height * 0.5f - radius);
            Vector3 a = center + up * half;
            Vector3 b = center - up * half;
            BodyCenter = center;

            // ── 안전 구역 ──
            bool crouched = crouch != null && crouch.IsCrouched;
            RatSafeZone zone = null;
            foreach (var z in RatSafeZone.Active)
            {
                if (z != null && z.Contains(center) && z.Accepts(crouched))
                {
                    zone = z;
                    break;
                }
            }
            if (zone != CurrentSafeZone)
            {
                var previous = CurrentSafeZone;
                CurrentSafeZone = zone;
                if (previous != null) { previous.SetOccupied(false); ExitedSafeZone?.Invoke(previous); }
                if (zone != null)
                {
                    zone.SetOccupied(true);
                    EnteredSafeZone?.Invoke(zone);
                    if (recentDanger >= hideEventDanger) HidFromDanger?.Invoke(zone);
                }
            }

            // ── 위험 물체 ──
            float raw = 0f;
            float nearest = float.MaxValue;
            FootstepHazard nearestHazard = null;
            FootstepHazard hit = null;
            foreach (var h in FootstepHazard.Active)
            {
                if (h == null) continue;
                float d = h.DistanceToCapsule(a, b, radius, out _);
                if (d < nearest) { nearest = d; nearestHazard = h; }
                raw = Mathf.Max(raw, 1f - Mathf.Clamp01(d / h.WarningRadius));
                if (d <= 0f && h.IsLethal && hit == null) hit = h;
            }
            NearestHazard = nearestHazard;
            NearestHazardDistance = nearest;
            if (IsSafe) raw *= 0.3f;   // 숨어 있으면 위험 표시를 약하게
            RawDanger = raw;
            DangerLevel = Mathf.Lerp(DangerLevel, raw, 1f - Mathf.Exp(-dangerSmoothing * Time.deltaTime));
            recentDanger = Mathf.Max(raw, recentDanger - Time.deltaTime); // 1초 정도 기억

            // ── 밟힘 → 리스폰 ──
            if (hit != null && !IsSafe && respawn != null && !respawn.IsRespawning && !respawn.IsInvulnerable)
            {
                HitByHazard?.Invoke(hit);
                respawn.Kill(hit.HazardName);
            }
        }

        public void ResetState()
        {
            DangerLevel = 0f;
            RawDanger = 0f;
            recentDanger = 0f;
        }
    }
}
