using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 순간 질주 상태와 속도 곡선 계산. 스스로 움직이지 않는다 (이동은 RatLocomotionController가 적용).
    ///
    /// - 오른쪽 트리거(또는 오른쪽 그립)를 누르면 스틱 방향으로 질주. 스틱을 안 밀었으면 바라보는 방향으로 (Quest 2 실기기 테스트 Q2-01)
    /// - 누른 순간 조건이 안 맞아도(공중·쿨다운 끝나기 직전) inputBuffer초 동안 기억했다가 가능해지면 바로 발동
    /// - 땅에서 막 떨어진 직후(groundGrace초)도 땅으로 본다 (CharacterController.isGrounded가 경사·턱에서 깜빡이기 때문)
    /// - 방향은 시작할 때 고정 → 카메라·진행 방향을 강제로 돌리지 않는다
    /// - 가속(accelerationTime) → 유지 → 감속(decelerationTime) 곡선으로 부드럽게
    /// - 정면 충돌 시 RatLocomotionController가 Stop(true) 호출 → 즉시 정지
    /// - 끝나면 쿨다운
    /// 속도 값 자체는 RatScaleConfig.dashSpeed (월드 m/s).
    /// </summary>
    [DisallowMultipleComponent]
    public class RatDashController : MonoBehaviour
    {
        public enum DashState { Ready, Dashing, Cooldown }

        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;
        [SerializeField] RatComfortSettings comfort;

        [Header("질주 (속도는 RatScaleConfig.dashSpeed)")]
        [Tooltip("질주 지속 시간(초).")]
        [SerializeField, Range(0.1f, 1.5f)] float duration = 0.6f;

        [Tooltip("최고 속도까지 가속하는 시간(초).")]
        [SerializeField, Range(0f, 0.5f)] float accelerationTime = 0.08f;

        [Tooltip("끝날 때 걷기 속도로 감속하는 시간(초).")]
        [SerializeField, Range(0f, 0.5f)] float decelerationTime = 0.15f;

        [Tooltip("질주 후 다시 쓸 수 있을 때까지의 시간(초).")]
        [SerializeField, Range(0f, 5f)] float cooldown = 0.8f;

        [Tooltip("이 세기보다 스틱을 약하게 밀었으면 스틱 대신 바라보는 방향으로 질주한다.")]
        [SerializeField, Range(0.05f, 1f)] float minStickInput = 0.15f;

        [Tooltip("스틱을 안 밀어도 바라보는 방향으로 질주한다.")]
        [SerializeField] bool dashForwardWithoutStick = true;

        [Tooltip("버튼을 누른 뒤 이 시간(초) 안에 질주할 수 있게 되면 발동한다.")]
        [SerializeField, Range(0f, 0.5f)] float inputBuffer = 0.25f;

        [Tooltip("땅에서 떨어진 뒤 이 시간(초)까지는 땅으로 본다.")]
        [SerializeField, Range(0f, 0.3f)] float groundGrace = 0.15f;

        [Tooltip("공중에서도 질주 허용.")]
        [SerializeField] bool allowAirDash = false;

        [Tooltip("웅크린 상태에서 질주하면 공간이 있을 때 자동으로 일어선다.")]
        [SerializeField] bool standUpToDash = true;

        float timer;
        Vector3 direction;
        float entrySpeed;
        float exitSpeed;
        float pressedAt = -10f;
        float lastGroundedAt = -10f;

        public DashState State { get; private set; } = DashState.Ready;
        public bool IsDashing => State == DashState.Dashing;
        public Vector3 CurrentVelocity { get; private set; }
        public float CooldownRemaining => State == DashState.Cooldown ? Mathf.Max(0f, timer) : 0f;
        public bool IsEnabled => comfort == null || comfort.enableDash;

        /// <summary>질주 시작.</summary>
        public event Action DashStarted;

        /// <summary>질주 종료 (true = 충돌로 정지).</summary>
        public event Action<bool> DashEnded;

        /// <summary>RatLocomotionController가 매 프레임 호출한다. viewForward = 카메라 수평 정면 (스틱 없이 질주할 때).</summary>
        public void Tick(Vector3 wishDirection, Vector3 viewForward, Vector3 currentPlanarVelocity, bool grounded, RatCrouchController crouch, float dt)
        {
            if (rig == null) rig = GetComponentInParent<RatScaleRig>();
            if (input == null) input = GetComponentInParent<RatInputReader>();
            if (rig == null || rig.Config == null) return;

            if (grounded) lastGroundedAt = Time.time;
            if (input != null && input.DashPressedThisFrame) pressedAt = Time.time; // 쿨다운·공중이어도 잠깐 기억
            bool buffered = Time.time - pressedAt <= inputBuffer;
            bool groundedish = grounded || Time.time - lastGroundedAt <= groundGrace;

            switch (State)
            {
                case DashState.Ready:
                    if (buffered && TryBegin(wishDirection, viewForward, currentPlanarVelocity, groundedish, crouch))
                        pressedAt = -10f;
                    break;

                case DashState.Dashing:
                    timer += dt;
                    CurrentVelocity = direction * SpeedAt(timer);
                    if (timer >= duration) End(false);
                    break;

                case DashState.Cooldown:
                    timer -= dt;
                    if (timer <= 0f) State = DashState.Ready;
                    break;
            }
        }

        bool TryBegin(Vector3 wishDirection, Vector3 viewForward, Vector3 currentPlanarVelocity, bool grounded, RatCrouchController crouch)
        {
            if (!IsEnabled) return false;
            float stick = wishDirection.magnitude;
            Vector3 dir;
            if (stick >= minStickInput) dir = wishDirection / stick;
            else if (dashForwardWithoutStick && viewForward.sqrMagnitude > 1e-6f) { dir = viewForward.normalized; stick = 1f; }
            else return false;
            if (!grounded && !allowAirDash) return false;
            if (crouch != null && crouch.IsCrouched)
            {
                if (!standUpToDash || !crouch.TryStand()) return false;
            }

            direction = dir;
            entrySpeed = Vector3.Dot(currentPlanarVelocity, direction);
            entrySpeed = Mathf.Max(0f, entrySpeed);
            exitSpeed = rig.Config.walkSpeed * Mathf.Clamp01(stick);
            timer = 0f;
            State = DashState.Dashing;
            CurrentVelocity = direction * SpeedAt(0f);
            DashStarted?.Invoke();
            return true;
        }

        float SpeedAt(float t)
        {
            float dashSpeed = rig.Config.dashSpeed;
            float up = accelerationTime > 0f ? Mathf.SmoothStep(0f, 1f, t / accelerationTime) : 1f;
            float down = decelerationTime > 0f ? Mathf.SmoothStep(0f, 1f, (duration - t) / decelerationTime) : 1f;
            float rising = Mathf.Lerp(entrySpeed, dashSpeed, up);
            float falling = Mathf.Lerp(exitSpeed, dashSpeed, down);
            return Mathf.Max(0f, Mathf.Min(rising, falling));
        }

        /// <summary>질주를 즉시 멈춘다 (충돌 등).</summary>
        public void Stop(bool blocked)
        {
            if (!IsDashing) return;
            End(blocked);
        }

        void End(bool blocked)
        {
            CurrentVelocity = Vector3.zero;
            State = cooldown > 0f ? DashState.Cooldown : DashState.Ready;
            timer = cooldown;
            DashEnded?.Invoke(blocked);
        }

        /// <summary>리스폰 등에서 상태 초기화.</summary>
        public void ResetState()
        {
            State = DashState.Ready;
            CurrentVelocity = Vector3.zero;
            timer = 0f;
        }
    }
}
