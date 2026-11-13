using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 점프 판정. 스스로 움직이지 않고, RatLocomotionController에 "도약 속도"만 돌려준다.
    /// - A 버튼, 지면에 있을 때만 (착지 직후 짧은 여유 시간 coyoteTime 허용)
    /// - 착지 전 짧게 미리 누른 입력도 인정 (inputBuffer)
    /// - 웅크린 상태에서는 점프하지 않음
    /// - jumpEnabled 또는 RatComfortSettings.enableJump로 끌 수 있음 (멀미 대비)
    /// 점프 높이는 월드 미터. 중력은 RatLocomotionController의 체감 중력을 사용.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatJumpController : MonoBehaviour
    {
        [SerializeField] RatInputReader input;
        [SerializeField] RatComfortSettings comfort;

        [Tooltip("점프 사용. 멀미가 심하면 끈다.")]
        [SerializeField] bool jumpEnabled = true;

        [Tooltip("점프 높이 (월드 m). 0.035 = 사람 기준 체감 약 35cm (배율 0.1).")]
        [SerializeField, Range(0.005f, 0.2f)] float jumpHeight = 0.035f;

        [Tooltip("발이 땅을 떠난 직후에도 점프를 허용하는 시간(초).")]
        [SerializeField, Range(0f, 0.3f)] float coyoteTime = 0.1f;

        [Tooltip("착지 직전에 누른 입력을 기억하는 시간(초).")]
        [SerializeField, Range(0f, 0.3f)] float inputBuffer = 0.12f;

        [SerializeField, Range(0f, 1f)] float cooldown = 0.25f;

        float bufferedUntil = -1f;
        float lastJumpTime = -10f;

        public bool IsEnabled => jumpEnabled && (comfort == null || comfort.enableJump);
        public bool IsAirborneFromJump { get; private set; }

        public event Action Jumped;
        public event Action Landed;

        public void SetJumpEnabled(bool value) => jumpEnabled = value;

        /// <summary>점프가 성립하면 true와 도약 속도(월드 m/s)를 돌려준다.</summary>
        public bool TryJump(bool grounded, float timeSinceGrounded, bool crouched, float gravity, out float launchSpeed)
        {
            launchSpeed = 0f;
            if (input == null) input = GetComponentInParent<RatInputReader>();
            if (input != null && input.JumpPressedThisFrame) bufferedUntil = Time.time + inputBuffer;

            if (!IsEnabled || crouched || IsAirborneFromJump) return false;
            if (Time.time > bufferedUntil) return false;
            if (Time.time - lastJumpTime < cooldown) return false;
            if (!grounded && timeSinceGrounded > coyoteTime) return false;

            launchSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
            bufferedUntil = -1f;
            lastJumpTime = Time.time;
            IsAirborneFromJump = true;
            Jumped?.Invoke();
            return true;
        }

        /// <summary>착지 판정. 상승이 끝난 뒤 지면에 닿으면 일반 이동 상태로 복귀.</summary>
        public void NotifyGroundState(bool grounded, float verticalVelocity)
        {
            if (IsAirborneFromJump && grounded && verticalVelocity <= 0f && Time.time - lastJumpTime > 0.05f)
            {
                IsAirborneFromJump = false;
                Landed?.Invoke();
            }
        }

        public void ResetState()
        {
            IsAirborneFromJump = false;
            bufferedUntil = -1f;
        }
    }
}
