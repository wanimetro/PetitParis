using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// Waypoint를 따라 반복해서 걷는 보행자 NPC (기본 도형 버전).
    ///
    /// 걸음 표현 (애니메이션 에셋 없이 절차적으로)
    /// - 몸(body)은 경로를 따라 일정 속도로 이동
    /// - 한 발은 땅에 고정(디딘 발), 다른 발은 다음 착지점으로 호를 그리며 이동 → 착지 순간 발소리
    /// - 다리(원기둥)는 엉덩이와 발목을 매 프레임 이어 준다
    /// - 발 그림자: 발이 높을수록 크고 옅게, 내려올수록 작고 진하게 → 밟힐 위치를 미리 알 수 있다
    /// - 착지 예고 표시: 공중의 발이 떨어질 지점에 진해지는 원
    /// 발(신발)에는 FootstepHazard가 붙어 있어 쥐에게 닿으면 실패.
    /// 모든 속도·크기는 월드 미터 (실제 사람 크기).
    /// </summary>
    [DisallowMultipleComponent]
    public class PedestrianPath : MonoBehaviour
    {
        public enum LoopMode { Loop, PingPong }

        [Header("경로")]
        [SerializeField] Transform[] waypoints = new Transform[0];
        [SerializeField] LoopMode loopMode = LoopMode.Loop;

        [Tooltip("걷는 속도 (월드 m/s). 사람 보통 걸음 1.2~1.4.")]
        [SerializeField, Range(0.1f, 3f)] float walkSpeed = 1.0f;

        [Tooltip("방향 전환 속도 (도/초).")]
        [SerializeField, Range(30f, 720f)] float turnSpeed = 240f;

        [Tooltip("각 Waypoint에서 멈추는 시간(초).")]
        [SerializeField, Range(0f, 10f)] float pauseAtWaypoint = 0f;

        [Tooltip("시작 전 대기 시간(초). 두 NPC의 타이밍을 엇갈리게 할 때 사용.")]
        [SerializeField, Range(0f, 30f)] float startDelay = 0f;

        [SerializeField] int startWaypoint = 0;

        [Header("몸")]
        [SerializeField] Transform body;
        [SerializeField, Range(0.4f, 1.2f)] float hipHeight = 0.88f;
        [SerializeField, Range(0.03f, 0.25f)] float hipWidth = 0.1f;
        [SerializeField, Range(0f, 0.08f)] float bodyBob = 0.02f;

        [Header("발과 다리")]
        [SerializeField] Transform leftFoot;
        [SerializeField] Transform rightFoot;
        [SerializeField] Transform leftLeg;
        [SerializeField] Transform rightLeg;
        [SerializeField, Range(0.04f, 0.25f)] float legThickness = 0.12f;

        [Tooltip("한 걸음 길이 (월드 m).")]
        [SerializeField, Range(0.2f, 1.2f)] float strideLength = 0.6f;

        [Tooltip("발을 드는 최대 높이 (월드 m). 쥐 눈높이 12cm와 비교해 위협감을 조절.")]
        [SerializeField, Range(0.02f, 0.4f)] float liftHeight = 0.14f;

        [Tooltip("발(신발) 중심의 지면 위 높이 = 신발 높이의 절반.")]
        [SerializeField, Range(0.01f, 0.1f)] float footCenterHeight = 0.045f;

        [Header("그림자·예고 표시")]
        [SerializeField] RatBlobShadow leftShadow;
        [SerializeField] RatBlobShadow rightShadow;
        [SerializeField] RatBlobShadow bodyShadow;
        [SerializeField] RatBlobShadow landingMarker;
        [SerializeField, Range(0f, 1f)] float shadowStrength = 0.55f;

        [Header("발소리 (3D 공간 음향)")]
        [SerializeField] AudioSource leftAudio;
        [SerializeField] AudioSource rightAudio;
        [SerializeField] AudioClip footstepClip;
        [SerializeField, Range(0f, 1f)] float footstepVolume = 0.9f;
        [SerializeField] Vector2 pitchRange = new Vector2(0.85f, 1.05f);

        [Tooltip("3D 소리가 줄어들기 시작하는 거리 (월드 m).")]
        [SerializeField, Range(0.05f, 2f)] float audioMinDistance = 0.3f;

        [Tooltip("3D 소리가 들리는 최대 거리 (월드 m).")]
        [SerializeField, Range(1f, 30f)] float audioMaxDistance = 10f;

        [Header("추격 (PedestrianAwareness, ChaseEvent가 켠다)")]
        [Tooltip("추격 중 발을 쥐 쪽으로 최대 얼마나 내딛는지 (월드 m). 클수록 '밟으러 오는' 느낌.")]
        [SerializeField, Range(0f, 0.35f)] float stompReach = 0.2f;

        [Tooltip("추격 중 이 거리(월드 m)보다 가까우면 멈춰 서서 제자리에서 쿵쿵 밟는다.")]
        [SerializeField, Range(0.05f, 0.6f)] float stompStopDistance = 0.22f;

        struct Foot
        {
            public Transform transform;
            public Vector3 plantPosition;
            public Quaternion plantRotation;
            public Vector3 swingFrom;
            public Quaternion swingFromRotation;
            public Vector3 swingTo;
        }

        Foot left, right;
        Vector3 position;
        Quaternion heading = Quaternion.identity;
        float groundY;
        int targetIndex;
        int direction = 1;
        float pauseTimer;
        float delayTimer;
        float currentSpeed;
        bool stepping;
        bool swingLeft;
        float stepTimer;
        float stepDuration = 0.5f;
        bool valid;

        // 추격
        Transform chaseTarget;
        float chaseSpeed;
        float chaseTurnSpeed;
        Bounds chaseArea;
        bool hasChaseArea;
        Transform lookTarget;

        public bool IsMoving => currentSpeed > 0.05f;
        public float WalkSpeed { get => walkSpeed; set => walkSpeed = Mathf.Max(0.05f, value); }
        public bool IsChasing => chaseTarget != null;
        /// <summary>몸 중심의 바닥 위치 (월드).</summary>
        public Vector3 GroundPosition => new Vector3(position.x, groundY, position.z);
        public Vector3 Forward => heading * Vector3.forward;
        public Transform Body => body;
        /// <summary>경로 Waypoint 전체를 감싸는 영역 (추격 범위 계산용).</summary>
        public Bounds PathBounds
        {
            get
            {
                var b = new Bounds(transform.position, Vector3.zero);
                bool first = true;
                if (waypoints != null)
                    foreach (var w in waypoints)
                    {
                        if (w == null) continue;
                        if (first) { b = new Bounds(w.position, Vector3.zero); first = false; }
                        else b.Encapsulate(w.position);
                    }
                return b;
            }
        }

        /// <summary>
        /// 목표(쥐)를 향해 걸어가며 발을 그쪽으로 내딛는다. area가 있으면 목표 위치를 그 안으로 제한한다 (벽·가게 안으로 들어가지 않게).
        /// turnDegreesPerSecond가 작을수록 옆으로 피하기 쉽다.
        /// </summary>
        public void BeginChase(Transform target, float speed, float turnDegreesPerSecond, Bounds? area = null)
        {
            chaseTarget = target;
            chaseSpeed = Mathf.Max(0.05f, speed);
            chaseTurnSpeed = Mathf.Max(10f, turnDegreesPerSecond);
            hasChaseArea = area.HasValue;
            if (area.HasValue) chaseArea = area.Value;
            pauseTimer = 0f;
            lookTarget = null;
        }

        /// <summary>추격을 끝내고 원래 경로로 돌아간다 (가장 가까운 Waypoint부터).</summary>
        public void EndChase()
        {
            if (chaseTarget == null) return;
            chaseTarget = null;
            targetIndex = NearestWaypointIndex();
        }

        /// <summary>잠시 멈춰 서서 target 쪽을 바라본다 (쥐를 발견한 순간 등).</summary>
        public void PauseAndLook(float seconds, Transform target)
        {
            pauseTimer = Mathf.Max(pauseTimer, seconds);
            lookTarget = target;
        }

        int NearestWaypointIndex()
        {
            int best = targetIndex;
            float bestD = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = Flat(waypoints[i].position - position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        Vector3 ChaseGoal()
        {
            Vector3 g = chaseTarget.position;
            if (hasChaseArea)
            {
                g.x = Mathf.Clamp(g.x, chaseArea.min.x, chaseArea.max.x);
                g.z = Mathf.Clamp(g.z, chaseArea.min.z, chaseArea.max.z);
            }
            g.y = groundY;
            return g;
        }

        /// <summary>발이 땅에 닿을 때 (착지 위치).</summary>
        public event Action<Vector3> FootLanded;

        void Start()
        {
            valid = waypoints != null && waypoints.Length >= 2 && body != null && leftFoot != null && rightFoot != null;
            if (!valid)
            {
                Debug.LogError($"[PetitParis] {name}: Waypoint 2개 이상과 body/leftFoot/rightFoot 참조가 필요합니다.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null) continue;
                Debug.LogError($"[PetitParis] {name}: Waypoint {i}가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            if (footstepClip == null) footstepClip = PetitParisAudio.Footstep;
            PetitParisAudio.Configure3D(leftAudio, audioMinDistance, audioMaxDistance);
            PetitParisAudio.Configure3D(rightAudio, audioMinDistance, audioMaxDistance);

            startWaypoint = Mathf.Clamp(startWaypoint, 0, waypoints.Length - 1);
            position = waypoints[startWaypoint].position;
            groundY = position.y;
            targetIndex = startWaypoint;
            AdvanceWaypoint();
            Vector3 toNext = Flat(waypoints[targetIndex].position - position);
            if (toNext.sqrMagnitude > 1e-6f) heading = Quaternion.LookRotation(toNext.normalized, Vector3.up);

            left = new Foot { transform = leftFoot };
            right = new Foot { transform = rightFoot };
            PlantAtRest(ref left, true);
            PlantAtRest(ref right, false);
            delayTimer = startDelay;
            UpdateVisuals();
        }

        void Update()
        {
            if (!valid) return;
            float dt = Time.deltaTime;

            if (delayTimer > 0f)
            {
                delayTimer -= dt;
            }
            else if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, walkSpeed * 3f * dt);
                if (lookTarget != null)
                {
                    Vector3 look = Flat(lookTarget.position - position);
                    if (look.sqrMagnitude > 1e-6f)
                        heading = Quaternion.RotateTowards(heading, Quaternion.LookRotation(look.normalized, Vector3.up), turnSpeed * dt);
                }
                if (pauseTimer <= 0f) lookTarget = null;
            }
            else if (chaseTarget != null)
            {
                // 추격: 몸이 바라보는 방향으로만 걷고 천천히 돈다 → 옆으로 질주하면 따돌릴 수 있다
                Vector3 to = Flat(ChaseGoal() - position);
                float distance = to.magnitude;
                if (distance > 1e-4f)
                    heading = Quaternion.RotateTowards(heading, Quaternion.LookRotation(to / distance, Vector3.up), chaseTurnSpeed * dt);
                float facing = distance > 1e-4f ? Vector3.Dot(heading * Vector3.forward, to / distance) : 1f;
                float want = distance > stompStopDistance ? chaseSpeed * Mathf.Clamp01(facing * 1.2f) : 0f;
                currentSpeed = Mathf.MoveTowards(currentSpeed, want, chaseSpeed * 3f * dt);
                position += heading * Vector3.forward * (currentSpeed * dt);
                if (hasChaseArea)
                {
                    position.x = Mathf.Clamp(position.x, chaseArea.min.x, chaseArea.max.x);
                    position.z = Mathf.Clamp(position.z, chaseArea.min.z, chaseArea.max.z);
                }
            }
            else
            {
                Vector3 target = waypoints[targetIndex].position;
                target.y = groundY;
                Vector3 to = Flat(target - position);
                float distance = to.magnitude;
                if (distance < 0.03f)
                {
                    AdvanceWaypoint();
                    if (pauseAtWaypoint > 0f) pauseTimer = pauseAtWaypoint;
                }
                else
                {
                    Vector3 dir = to / distance;
                    heading = Quaternion.RotateTowards(heading, Quaternion.LookRotation(dir, Vector3.up), turnSpeed * dt);
                    currentSpeed = Mathf.MoveTowards(currentSpeed, walkSpeed, walkSpeed * 2f * dt);
                    position += dir * Mathf.Min(currentSpeed * dt, distance);
                }
            }

            UpdateGait(dt);
            UpdateVisuals();
        }

        void AdvanceWaypoint()
        {
            int n = waypoints.Length;
            if (loopMode == LoopMode.Loop)
            {
                targetIndex = (targetIndex + 1) % n;
            }
            else
            {
                if (targetIndex + direction >= n || targetIndex + direction < 0) direction = -direction;
                targetIndex += direction;
            }
        }

        void UpdateGait(float dt)
        {
            if (!stepping)
            {
                bool moving = currentSpeed > 0.05f;
                bool stomping = chaseTarget != null && Flat(chaseTarget.position - position).magnitude < stompStopDistance + stompReach + 0.15f;
                if (!moving && !stomping && !FeetNeedSettling()) return;
                BeginStep();
            }

            stepTimer += dt;
            float t = Mathf.Clamp01(stepTimer / stepDuration);
            float s = Mathf.SmoothStep(0f, 1f, t);
            ref Foot foot = ref swingLeft ? ref left : ref right;

            // 착지점을 계속 갱신 (회전하며 걸을 때도 자연스럽게)
            foot.swingTo = LandingPoint(swingLeft, Mathf.Max(0f, stepDuration - stepTimer));
            Vector3 p = Vector3.Lerp(foot.swingFrom, foot.swingTo, s);
            p.y = groundY + footCenterHeight + Mathf.Sin(t * Mathf.PI) * liftHeight;
            foot.transform.SetPositionAndRotation(p, Quaternion.Slerp(foot.swingFromRotation, heading, s));

            if (t >= 1f)
            {
                foot.plantPosition = foot.swingTo;
                foot.plantRotation = heading;
                stepping = false;
                swingLeft = !swingLeft;
                PlayFootstep(swingLeft ? rightAudio : leftAudio, foot.plantPosition); // 방금 디딘 발
                FootLanded?.Invoke(foot.plantPosition);
            }
        }

        void BeginStep()
        {
            stepping = true;
            stepTimer = 0f;
            stepDuration = chaseTarget != null
                ? Mathf.Clamp(strideLength / Mathf.Max(0.3f, Mathf.Max(currentSpeed, chaseSpeed)), 0.3f, 0.7f)
                : Mathf.Clamp(strideLength / Mathf.Max(0.3f, currentSpeed), 0.3f, 1f);
            ref Foot foot = ref swingLeft ? ref left : ref right;
            foot.swingFrom = foot.plantPosition;
            foot.swingFromRotation = foot.plantRotation;
            foot.swingTo = LandingPoint(swingLeft, stepDuration);
        }

        Vector3 LandingPoint(bool isLeft, float remainingTime)
        {
            Vector3 forward = heading * Vector3.forward;
            Vector3 side = heading * Vector3.right * (isLeft ? -hipWidth : hipWidth);
            Vector3 predicted = position + forward * currentSpeed * remainingTime;
            Vector3 lead = currentSpeed > 0.05f ? forward * strideLength * 0.3f : Vector3.zero;
            Vector3 p = predicted + side + lead;
            if (chaseTarget != null && stompReach > 0f)
            {
                // 추격 중: 발을 쥐가 있는 쪽으로 내딛는다 (최대 stompReach)
                Vector3 toRat = Flat(chaseTarget.position - p);
                p += Vector3.ClampMagnitude(toRat, stompReach);
            }
            p.y = groundY + footCenterHeight;
            return p;
        }

        bool FeetNeedSettling()
        {
            return (Flat(left.plantPosition - LandingPoint(true, 0f))).sqrMagnitude > 0.06f * 0.06f
                || (Flat(right.plantPosition - LandingPoint(false, 0f))).sqrMagnitude > 0.06f * 0.06f;
        }

        void PlantAtRest(ref Foot foot, bool isLeft)
        {
            foot.plantPosition = LandingPoint(isLeft, 0f);
            foot.plantRotation = heading;
            foot.transform.SetPositionAndRotation(foot.plantPosition, heading);
        }

        void PlayFootstep(AudioSource source, Vector3 at)
        {
            if (source == null || footstepClip == null) return;
            source.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(footstepClip, footstepVolume);
        }

        void UpdateVisuals()
        {
            float bob = stepping ? Mathf.Sin(Mathf.Clamp01(stepTimer / stepDuration) * Mathf.PI) * bodyBob : 0f;
            body.SetPositionAndRotation(new Vector3(position.x, groundY + bob, position.z), heading);

            // 디딘 발은 땅에 고정
            bool leftSwinging = stepping && swingLeft;
            bool rightSwinging = stepping && !swingLeft;
            if (!leftSwinging) left.transform.SetPositionAndRotation(left.plantPosition, left.plantRotation);
            if (!rightSwinging) right.transform.SetPositionAndRotation(right.plantPosition, right.plantRotation);

            UpdateLeg(leftLeg, left.transform, true);
            UpdateLeg(rightLeg, right.transform, false);

            UpdateFootShadow(leftShadow, left.transform);
            UpdateFootShadow(rightShadow, right.transform);

            Quaternion yaw = Quaternion.Euler(0f, heading.eulerAngles.y, 0f);
            if (bodyShadow != null)
                bodyShadow.Set(new Vector3(position.x, groundY + 0.006f, position.z), yaw, new Vector2(0.55f, 0.45f), shadowStrength * 0.45f);

            if (landingMarker != null)
            {
                if (stepping)
                {
                    ref Foot foot = ref swingLeft ? ref left : ref right;
                    float t = Mathf.Clamp01(stepTimer / stepDuration);
                    Vector3 g = foot.swingTo;
                    g.y = groundY + 0.007f;
                    landingMarker.Set(g, yaw, new Vector2(0.2f, 0.36f), Mathf.SmoothStep(0f, 0.55f, t) * shadowStrength / 0.55f);
                }
                else
                {
                    landingMarker.Set(new Vector3(position.x, groundY + 0.007f, position.z), yaw, Vector2.one * 0.01f, 0f);
                }
            }
        }

        void UpdateLeg(Transform leg, Transform foot, bool isLeft)
        {
            if (leg == null || foot == null) return;
            Vector3 hip = body.position + heading * new Vector3(isLeft ? -hipWidth : hipWidth, hipHeight, 0f);
            Vector3 ankle = foot.position + Vector3.up * footCenterHeight;
            Vector3 d = hip - ankle;
            float length = d.magnitude;
            if (length < 1e-4f) return;
            leg.SetPositionAndRotation((hip + ankle) * 0.5f, Quaternion.FromToRotation(Vector3.up, d / length));
            leg.localScale = new Vector3(legThickness, length * 0.5f, legThickness); // 기본 원기둥 높이 2
        }

        void UpdateFootShadow(RatBlobShadow shadow, Transform foot)
        {
            if (shadow == null || foot == null) return;
            float height = Mathf.Max(0f, foot.position.y - (groundY + footCenterHeight));
            float k = liftHeight > 0f ? Mathf.Clamp01(height / liftHeight) : 0f;
            Vector3 g = new Vector3(foot.position.x, groundY + 0.006f, foot.position.z);
            Vector2 size = Vector2.Lerp(new Vector2(0.16f, 0.32f), new Vector2(0.26f, 0.44f), k);
            shadow.Set(g, Quaternion.Euler(0f, foot.eulerAngles.y, 0f), size, Mathf.Lerp(shadowStrength, shadowStrength * 0.45f, k));
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                var a = waypoints[i];
                var b = waypoints[(i + 1) % waypoints.Length];
                if (a == null || b == null) continue;
                if (loopMode == LoopMode.PingPong && i == waypoints.Length - 1) break;
                Gizmos.DrawLine(a.position, b.position);
                Gizmos.DrawWireSphere(a.position, 0.05f);
            }
        }
    }
}
