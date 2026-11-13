using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 내비게이션처럼 "지금 가야 할 방향"을 쥐 발 앞 바닥의 화살표로 보여준다.
    ///
    /// - 레벨에 미리 정해 둔 안내 경로(route, 순서 있는 점들) 위에서 쥐와 가장 가까운 지점을 찾고,
    ///   그보다 lookAhead만큼 앞의 경로 지점을 가리킨다 → 벽을 뚫고 직선으로 가리키지 않는다.
    /// - 쥐가 경로를 벗어나도 다시 경로 쪽으로 안내한다. 지나간 구간으로 되돌아가 가리키지 않도록 진행도를 기억한다.
    /// - 리스폰하면 진행도를 체크포인트 위치에 맞춰 다시 계산한다.
    /// - 화살표는 월드 공간 물체라 카메라를 움직이지 않는다 (멀미 없음). 목적지 근처와 완료 화면에서는 숨긴다.
    /// 거리 단위는 월드 미터.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatGuideArrow : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatRespawn respawn;
        [SerializeField] PetitParisGameFlow gameFlow;

        [Tooltip("안내 경로 점들 (시작 → 목적지 순서).")]
        [SerializeField] Transform[] route = new Transform[0];

        [Tooltip("화살표 모양 오브젝트 (바닥에 눕힌 화살표, +Z가 앞).")]
        [SerializeField] Transform arrow;

        [SerializeField] bool showArrow = true;

        [Tooltip("쥐 앞 몇 m 지점에 화살표를 둘지 (0.26 = 체감 2.6m).")]
        [SerializeField, Range(0.05f, 0.5f)] float distanceAhead = 0.26f;

        [Tooltip("경로를 얼마나 앞서 가리킬지 (월드 m). 길수록 모퉁이를 미리 꺾는다.")]
        [SerializeField, Range(0.1f, 2f)] float lookAhead = 0.6f;

        [Tooltip("바닥에서 떠 있는 높이 (월드 m). 눈높이 12cm보다 조금 낮게 → PC 화면과 VR 모두 시야 안에 들어온다.")]
        [SerializeField, Range(0f, 0.1f)] float hoverHeight = 0.065f;

        [Tooltip("화살표 앞쪽을 들어 올리는 각도. 0이면 바닥에 납작, 클수록 표지판처럼 서서 잘 보인다.")]
        [SerializeField, Range(0f, 80f)] float tiltDegrees = 50f;
        [SerializeField, Range(0f, 0.02f)] float bobAmplitude = 0.004f;
        [SerializeField, Range(30f, 720f)] float turnSpeed = 240f;

        [Tooltip("목적지까지 이 거리(월드 m) 안이면 숨긴다.")]
        [SerializeField, Range(0f, 2f)] float hideNearEnd = 0.4f;

        float[] cumulative = new float[0];
        float totalLength;
        float maxReached;
        Quaternion currentYaw = Quaternion.identity;
        bool hasYaw;
        bool needsResync = true;

        public bool ShowArrow { get => showArrow; set => showArrow = value; }
        public float Progress01 => totalLength > 0f ? Mathf.Clamp01(maxReached / totalLength) : 0f;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (respawn == null && rig != null) respawn = rig.GetComponent<RatRespawn>();
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            BuildCumulative();
            if (arrow == null) Debug.LogWarning("[PetitParis] RatGuideArrow: 화살표(arrow) 오브젝트가 연결되지 않았습니다.", this);
        }

        void OnEnable()
        {
            if (respawn != null) respawn.Respawned += OnRespawned;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.Respawned -= OnRespawned;
        }

        void OnRespawned() => needsResync = true;

        void BuildCumulative()
        {
            int n = route != null ? route.Length : 0;
            cumulative = new float[n];
            totalLength = 0f;
            for (int i = 1; i < n; i++)
            {
                if (route[i] == null || route[i - 1] == null) continue;
                totalLength += Flat(route[i].position - route[i - 1].position).magnitude;
                cumulative[i] = totalLength;
            }
        }

        void LateUpdate()
        {
            if (arrow == null) return;
            bool show = showArrow && rig != null && totalLength > 0f
                        && (gameFlow == null || gameFlow.CurrentPhase != PetitParisGameFlow.Phase.Completed)
                        && (respawn == null || !respawn.IsRespawning);
            if (!show) { SetVisible(false); return; }

            Vector3 p = rig.transform.position;
            if (needsResync)
            {
                maxReached = ClosestParam(p, float.NegativeInfinity);
                needsResync = false;
                hasYaw = false;
            }

            float param = ClosestParam(p, maxReached - 1.5f);
            maxReached = Mathf.Max(maxReached, param);
            if (totalLength - param < hideNearEnd) { SetVisible(false); return; }

            Vector3 target = PointAt(Mathf.Min(totalLength, param + lookAhead));
            Vector3 dir = Flat(target - p);
            if (dir.sqrMagnitude < 1e-6f) dir = Flat(PointAt(totalLength) - p);
            if (dir.sqrMagnitude < 1e-6f) { SetVisible(false); return; }

            Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
            currentYaw = hasYaw ? Quaternion.RotateTowards(currentYaw, want, turnSpeed * Time.deltaTime) : want;
            hasYaw = true;

            float bob = Mathf.Sin(Time.time * 3f) * bobAmplitude;
            Vector3 forward = currentYaw * Vector3.forward;
            arrow.SetPositionAndRotation(p + forward * distanceAhead + Vector3.up * (hoverHeight + bob),
                currentYaw * Quaternion.Euler(-tiltDegrees, 0f, 0f));
            SetVisible(true);
        }

        void SetVisible(bool visible)
        {
            if (arrow != null && arrow.gameObject.activeSelf != visible) arrow.gameObject.SetActive(visible);
        }

        /// <summary>경로 위에서 p와 가장 가까운 지점의 누적 거리. minParam보다 앞쪽만 찾는다.</summary>
        float ClosestParam(Vector3 p, float minParam)
        {
            float bestParam = Mathf.Max(0f, minParam);
            float bestDistance = float.MaxValue;
            for (int i = 0; i + 1 < route.Length; i++)
            {
                if (route[i] == null || route[i + 1] == null) continue;
                Vector3 a = Flat(route[i].position), b = Flat(route[i + 1].position);
                Vector3 ab = b - a;
                float len = ab.magnitude;
                if (len < 1e-4f) continue;
                float segStart = cumulative[i];
                if (segStart + len < minParam) continue;

                float t = Mathf.Clamp01(Vector3.Dot(Flat(p) - a, ab) / (len * len));
                if (segStart + t * len < minParam) t = Mathf.Clamp01((minParam - segStart) / len);
                float d = (Flat(p) - (a + ab * t)).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    bestParam = segStart + t * len;
                }
            }
            return bestParam;
        }

        Vector3 PointAt(float param)
        {
            for (int i = 0; i + 1 < route.Length; i++)
            {
                if (route[i] == null || route[i + 1] == null) continue;
                float len = cumulative[i + 1] - cumulative[i];
                if (param <= cumulative[i + 1] || i + 2 == route.Length)
                    return Vector3.Lerp(route[i].position, route[i + 1].position, len > 1e-4f ? Mathf.Clamp01((param - cumulative[i]) / len) : 1f);
            }
            return route.Length > 0 && route[route.Length - 1] != null ? route[route.Length - 1].position : transform.position;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void OnDrawGizmos()
        {
            if (route == null || route.Length < 2) return;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            for (int i = 0; i + 1 < route.Length; i++)
                if (route[i] != null && route[i + 1] != null)
                    Gizmos.DrawLine(route[i].position + Vector3.up * 0.02f, route[i + 1].position + Vector3.up * 0.02f);
        }
    }
}
