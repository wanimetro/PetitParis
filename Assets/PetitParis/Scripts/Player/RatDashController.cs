using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 순간 질주 상태와 속도 곡선 계산. 스스로 움직이지 않는다 (이동은 RatLocomotionController가 적용).
    ///
    /// - 오른쪽 트리거를 누른 순간, 스틱 입력 방향으로 질주 시작 (입력 없으면 불가)
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
        [SerializeField, Range(0.1f, 1.5f)] float duration = 0.45f;

        [Tooltip("최고 속도까지 가속하는 시간(초).")]
        [SerializeField, Range(0f, 0.5f)] float accelerationTime = 0.08f;

        [Tooltip("끝날 때 걷기 속도로 감속하는 시간(초).")]
        [SerializeField, Range(0f, 0.5f)] float decelerationTime = 0.15f;

        [Tooltip("질주 후 다시 쓸 수 있을 때까지의 시간(초).")]
        [SerializeField, Range(0f, 5f)] float cooldown = 1.2f;

        [Tooltip("이 세기 이상 스틱을 밀어야 질주할 수 있다.")]
        [SerializeField, Range(0.05f, 1f)] float minStickInput = 0.3f;

        [Tooltip("공중에서도 질주 허용.")]
        [SerializeField] bool allowAirDash = false;

        [Tooltip("웅크린 상태에서 질주하면 공간이 있을 때 자동으로 일어선다.")]
        [SerializeField] bool standUpToDash = true;

        float timer;
        Vector3 direction;
        float entrySpeed;
        float exitSpeed;

        public DashState State { get; private set; } = DashState.Ready;
        public bool IsDashing => State == DashState.Dashing;
        public Vector3 CurrentVelocity { get; private set; }
        public float CooldownRemaining => State == DashState.Cooldown ? Mathf.Max(0f, timer) : 0f;
        public bool IsEnabled => comfort == null || comfort.enableDash;

        /// <summary>질주 시작.</summary>
        public event Action DashStarted;

        /// <summary>질주 종료 (true = 충돌로 정지).</summary>
        public event Action<bool> DashEnded;

        /// <summary>RatLocomotionController가 매 프레임 호출한다.</summary>
        public void Tick(Vector3 wishDirection, Vector3 currentPlanarVelocity, bool grounded, RatCrouchController crouch, float dt)
        {
            if (rig == null) rig = GetComponentInParent<RatScaleRig>();
            if (input == null) input = GetComponentInParent<RatInputReader>();
            if (rig == null || rig.Config == null) return;

            switch (State)
            {
                case DashState.Ready:
                    if (input != null && input.DashPressedThisFrame)
                        TryBegin(wishDirection, currentPlanarVelocity, grounded, crouch);
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

        void TryBegin(Vector3 wishDirection, Vector3 currentPlanarVelocity, bool grounded, RatCrouchController crouch)
        {
            if (!IsEnabled) return;
            float stick = wishDirection.magnitude;
            if (stick < minStickInput) return;          // 이동 입력이 없으면 질주 불가
            if (!grounded && !allowAirDash) return;
            if (crouch != null && crouch.IsCrouched)
            {
                if (!standUpToDash || !crouch.TryStand()) return;
            }

            direction = wishDirection / stick;
            entrySpeed = Vector3.Dot(currentPlanarVelocity, direction);
            entrySpeed = Mathf.Max(0f, entrySpeed);
            exitSpeed = rig.Config.walkSpeed * Mathf.Clamp01(stick);
            timer = 0f;
            State = DashState.Dashing;
            CurrentVelocity = direction * SpeedAt(0f);
            DashStarted?.Invoke();
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
