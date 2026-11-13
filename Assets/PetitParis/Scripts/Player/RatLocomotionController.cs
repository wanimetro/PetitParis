using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace PetitParis
{
    /// <summary>
    /// 쥐 이동의 유일한 "실제 이동" 담당 (XRI LocomotionProvider).
    ///
    /// 중복 방지 설계
    /// - 걷기 + 질주 + 점프 + 중력을 여기서 한 번에 합산해, 프레임마다 XROriginMovement 하나만 큐에 넣는다.
    /// - RatDashController / RatJumpController / RatCrouchController는 상태와 속도만 계산하고 스스로 움직이지 않는다.
    /// - 실제 적용은 XRI의 LocomotionMediator → XRBodyTransformer → CharacterController.Move가 한다.
    ///   (벽 충돌·계단은 CharacterController가 처리 → 충돌체를 뚫지 않는다)
    /// - XRI ContinuousMoveProvider / GravityProvider / JumpProvider는 리그에 넣지 않는다 (같은 일을 두 번 하게 되므로).
    /// - 회전은 XRI SnapTurnProvider가 따로 담당한다 (Yaw 회전만, 이동과 겹치지 않음).
    ///
    /// 편안함
    /// - 이동 방향은 카메라의 Yaw(수평 방향)만 사용한다. Pitch/Roll은 무시한다.
    /// - 카메라 Transform은 절대 건드리지 않는다. Head Bob 없음.
    ///
    /// 주의: LocomotionProvider의 Awake/OnEnable을 가리지 않도록 이 클래스는 Update만 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatLocomotionController : LocomotionProvider
    {
        const int IgnoreRaycastLayer = 2;

        [Header("참조")]
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;
        [SerializeField] RatComfortSettings comfort;
        [SerializeField] RatDashController dash;
        [SerializeField] RatJumpController jump;
        [SerializeField] RatCrouchController crouch;

        [Header("기본 이동 — 최고 속도는 RatScaleConfig.walkSpeed (월드 m/s)")]
        [Tooltip("스틱 데드존 (원형).")]
        [SerializeField, Range(0f, 0.5f)] float stickDeadzone = 0.15f;

        [Tooltip("가속 (월드 m/s²). 1.2면 약 0.13초 만에 걷기 속도 도달.")]
        [SerializeField, Range(0.05f, 5f)] float acceleration = 1.2f;

        [Tooltip("감속 (월드 m/s²). 스틱을 놓았을 때 부드럽게 멈춘다.")]
        [SerializeField, Range(0.05f, 5f)] float deceleration = 1.8f;

        [Tooltip("공중에서의 조작 비율.")]
        [SerializeField, Range(0f, 1f)] float airControl = 0.4f;

        [Header("중력")]
        [Tooltip("켜면 중력도 쥐 배율만큼 줄여 '사람 기준'으로 자연스럽게 느끼게 한다 (9.81 × trackingScale).")]
        [SerializeField] bool perceivedGravity = true;

        [SerializeField, Range(0.1f, 3f)] float gravityMultiplier = 1f;

        [Tooltip("최대 낙하 속도 (월드 m/s).")]
        [SerializeField, Range(0.05f, 5f)] float maxFallSpeed = 1f;

        [Tooltip("지면에 붙여두는 작은 하강 속도 (월드 m/s). isGrounded를 안정적으로 유지한다.")]
        [SerializeField, Range(0f, 0.5f)] float groundStickSpeed = 0.05f;

        [Header("질주 충돌 검사")]
        [Tooltip("질주 전방 검사 대상 레이어. 플레이어(Ignore Raycast)는 제외.")]
        [SerializeField] LayerMask obstacleMask = ~(1 << IgnoreRaycastLayer);

        [Tooltip("정면으로 이 각도 이내의 벽에 부딪히면 질주를 멈춘다 (스치는 벽은 계속).")]
        [SerializeField, Range(10f, 89f)] float dashStopAngle = 60f;

        [SerializeField, Range(0f, 0.05f)] float dashProbeMargin = 0.01f;

        // XRI 3.3.2의 이동 변환 클래스 (XRI ContinuousMoveProvider와 같은 방식)
        readonly XROriginMovement translation = new XROriginMovement();

        Vector3 planarVelocity;
        float verticalVelocity;
        float lastGroundedTime = -10f;
        bool inputLocked;
        bool suspended;
        bool initialized;
        bool missingRefsLogged;

        public RatScaleRig Rig => rig;
        public CharacterController Body => rig != null ? rig.Body : null;
        public Vector3 PlanarVelocity => planarVelocity;
        public float PlanarSpeed => planarVelocity.magnitude;
        public float VerticalVelocity => verticalVelocity;
        public bool IsGrounded => Body != null && Body.enabled && Body.isGrounded;
        public bool IsInputLocked => inputLocked;
        public bool IsSuspended => suspended;
        public Vector2 LastStick { get; private set; }

        /// <summary>플레이어 체감 기준 중력 (월드 m/s²).</summary>
        public float GravityMagnitude
        {
            get
            {
                float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
                if (perceivedGravity && rig != null && rig.Config != null) g *= rig.Config.trackingScale;
                return Mathf.Max(0.01f, g);
            }
        }

        /// <summary>스틱·질주·점프 입력을 막는다 (중력은 계속). 완료 화면, 리스폰 중 사용.</summary>
        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (locked) planarVelocity = Vector3.zero;
        }

        /// <summary>이동 큐 자체를 멈춘다. 순간이동(리스폰) 직전·직후에 사용.</summary>
        public void SetSuspended(bool value)
        {
            suspended = value;
            if (value) ResetMotion();
        }

        public void ResetMotion()
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            if (rig == null) rig = GetComponentInParent<RatScaleRig>();
            if (input == null) input = GetComponentInParent<RatInputReader>();
            if (dash == null) dash = GetComponent<RatDashController>();
            if (jump == null) jump = GetComponent<RatJumpController>();
            if (crouch == null) crouch = GetComponent<RatCrouchController>();
        }

        void Update()
        {
            EnsureInitialized();
            var body = Body;
            if (body == null || rig == null || !rig.IsValid || input == null)
            {
                if (!missingRefsLogged)
                {
                    missingRefsLogged = true;
                    Debug.LogError("[PetitParis] RatLocomotionController: Rig / CharacterController / RatInputReader 참조가 없습니다.", this);
                }
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f || suspended || !body.enabled) return;

            Transform origin = rig.transform;
            Vector3 up = origin.up;
            bool grounded = body.isGrounded;
            if (grounded) lastGroundedTime = Time.time;

            // ── 1. 스틱 → 목표 수평 속도 (카메라 Yaw 기준) ──
            Vector2 stick = inputLocked ? Vector2.zero : ApplyDeadzone(input.Move);
            LastStick = stick;
            GetYawBasis(up, out Vector3 forward, out Vector3 right);
            Vector3 wishDirection = forward * stick.y + right * stick.x;   // 크기 0~1 = 스틱 세기
            float walkSpeed = rig.Config.walkSpeed * (crouch != null ? crouch.SpeedMultiplier : 1f);
            Vector3 targetPlanar = wishDirection * walkSpeed;

            // ── 2. 질주 (방향은 시작할 때 고정, 속도 곡선은 RatDashController가 계산) ──
            bool dashing = false;
            if (dash != null)
            {
                dash.Tick(inputLocked ? Vector3.zero : wishDirection, planarVelocity, grounded, crouch, dt);
                if (dash.IsDashing)
                {
                    Vector3 dashVelocity = dash.CurrentVelocity;
                    if (IsDashBlocked(body, up, dashVelocity, dt))
                    {
                        dash.Stop(true);              // 벽·물체 정면 충돌 → 즉시 정지
                        planarVelocity = Vector3.zero;
                        targetPlanar = Vector3.zero;
                    }
                    else
                    {
                        dashing = true;
                        planarVelocity = dashVelocity;
                    }
                }
            }

            if (!dashing)
            {
                bool speedingUp = targetPlanar.sqrMagnitude > planarVelocity.sqrMagnitude;
                float rate = speedingUp ? acceleration : deceleration;
                if (!grounded) rate *= airControl;
                planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanar, rate * dt);
            }

            // 체감 속도 상한
            if (comfort != null && comfort.limitSpeed)
            {
                float maxWorld = comfort.maxPerceivedSpeed * rig.Config.trackingScale;
                if (planarVelocity.magnitude > maxWorld) planarVelocity = planarVelocity.normalized * maxWorld;
            }

            // ── 3. 수직: 점프와 중력 ──
            float gravity = GravityMagnitude;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -groundStickSpeed;

            if (jump != null)
            {
                bool crouched = crouch != null && crouch.IsCrouched;
                if (!inputLocked && jump.TryJump(grounded, Time.time - lastGroundedTime, crouched, gravity, out float launch))
                    verticalVelocity = launch;
                jump.NotifyGroundState(grounded, verticalVelocity);
            }

            if ((body.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f; // 머리 위에 부딪히면 상승 중단

            if (!grounded || verticalVelocity > 0f)
                verticalVelocity -= gravity * dt;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);

            // ── 4. 이번 프레임의 이동을 한 번만 큐에 넣는다 ──
            Vector3 displacement = (planarVelocity + up * verticalVelocity) * dt;
            if (locomotionState != LocomotionState.Moving)
                TryStartLocomotionImmediately();
            if (locomotionState == LocomotionState.Moving)
            {
                translation.motion = displacement;
                TryQueueTransformation(translation);
            }
        }

        Vector2 ApplyDeadzone(Vector2 raw)
        {
            float m = raw.magnitude;
            if (m <= stickDeadzone) return Vector2.zero;
            float scaled = Mathf.Clamp01((m - stickDeadzone) / (1f - stickDeadzone));
            return raw / m * scaled;
        }

        void GetYawBasis(Vector3 up, out Vector3 forward, out Vector3 right)
        {
            Transform cam = rig.EyeCamera.transform;
            forward = Vector3.ProjectOnPlane(cam.forward, up);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(cam.up, up); // 바로 아래를 볼 때
            forward = forward.sqrMagnitude > 1e-8f ? forward.normalized : rig.transform.forward;
            right = Vector3.Cross(up, forward);
        }

        /// <summary>
        /// 질주 방향으로 CharacterController 캡슐을 미리 쓸어 본다 (Physics.CapsuleCast).
        /// 정면에 가까운 벽·물체가 있으면 true → 질주 정지. 트리거(안전 구역 등)와 플레이어 레이어는 무시.
        /// 보조로 실제 이동 결과(CharacterController.velocity)가 크게 줄었을 때도 막힌 것으로 본다.
        /// </summary>
        bool IsDashBlocked(CharacterController body, Vector3 up, Vector3 dashVelocity, float dt)
        {
            float speed = dashVelocity.magnitude;
            if (speed < 1e-4f) return false;
            Vector3 dir = dashVelocity / speed;

            Vector3 center = body.transform.TransformPoint(body.center);
            float radius = body.radius * 0.95f;
            float half = Mathf.Max(0f, body.height * 0.5f - body.radius);
            Vector3 top = center + up * half;
            Vector3 bottom = center - up * half + up * body.stepOffset; // 넘을 수 있는 턱은 무시
            if (Vector3.Dot(top - bottom, up) < 0f) bottom = top;

            float distance = speed * dt + body.skinWidth + dashProbeMargin;
            if (Physics.CapsuleCast(top, bottom, radius, dir, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                float facing = Vector3.Dot(-hit.normal, dir);
                if (facing >= Mathf.Cos(dashStopAngle * Mathf.Deg2Rad)) return true;
            }

            // 보조 판정: 옆이 막혀 실제 속도가 명령 속도의 25% 미만이면 정지
            if ((body.collisionFlags & CollisionFlags.Sides) != 0)
            {
                Vector3 actual = Vector3.ProjectOnPlane(body.velocity, up);
                if (Vector3.Dot(actual, dir) < speed * 0.25f) return true;
            }
            return false;
        }
    }
}
