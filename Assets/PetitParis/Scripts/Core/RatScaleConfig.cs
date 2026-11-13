using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 쥐 시점 스케일 체계의 단일 기준값.
    ///
    /// 좌표계 규칙
    /// - 씬의 모든 오브젝트는 "월드 미터" = 실제 파리 크기로 만든다 (의자 좌판 0.45m, 사람 1.75m).
    /// - 플레이어는 XR Origin 아래 "Rat Tracking Space"를 trackingScale 배로 줄여서 작게 만든다.
    ///   → 머리 추적(TrackedPoseDriver)은 그대로 두고, 실제 머리 10cm 이동이 월드 1cm 이동이 된다.
    /// - 이 에셋의 길이·속도 값은 전부 월드 미터 단위다.
    ///   플레이어가 체감하는 값 = 월드 값 / trackingScale (ToPerceived).
    /// </summary>
    [CreateAssetMenu(fileName = "RatScaleConfig", menuName = "PetitParis/Rat Scale Config")]
    public class RatScaleConfig : ScriptableObject
    {
        [Header("시점 스케일")]
        [Tooltip("추적 공간 배율. 0.1이면 실제 머리 10cm 이동이 월드 1cm 이동이 되고, 세상이 약 10배 크게 느껴진다.")]
        [Range(0.02f, 1f)] public float trackingScale = 0.1f;

        [Tooltip("캘리브레이션 후 쥐 눈높이 (월드 미터, XR Origin 바닥 기준). 기획 기준 0.10~0.15.")]
        [Range(0.03f, 0.5f)] public float targetEyeHeight = 0.12f;

        [Tooltip("캘리브레이션할 때 사용자가 보고 있는 방향을 XR Origin의 정면(시작 방향)에 맞춘다.")]
        public bool alignYawOnCalibrate = true;

        [Header("쥐 몸 충돌체 — CharacterController (월드 미터)")]
        [Tooltip("몸 반지름. 쥐 몸폭 약 7cm 기준 0.035.")]
        [Range(0.01f, 0.2f)] public float bodyRadius = 0.035f;

        [Tooltip("몸 높이. 눈높이보다 약간 높게 두어 머리까지 감싼다.")]
        [Range(0.03f, 0.5f)] public float bodyHeight = 0.13f;

        [Tooltip("넘을 수 있는 턱 높이. 보도블록 틈·자갈 정도.")]
        [Range(0f, 0.1f)] public float stepOffset = 0.02f;

        [Range(0f, 80f)] public float slopeLimit = 50f;

        [Tooltip("CharacterController Skin Width. Unity 권장값은 반지름의 약 10%.")]
        [Range(0.0005f, 0.02f)] public float skinWidth = 0.0035f;

        [Header("카메라 클리핑 (월드 미터)")]
        [Tooltip("가까운 클리핑 거리. 쥐 크기에 맞춰 아주 작게 둔다.")]
        [Range(0.001f, 0.05f)] public float nearClip = 0.005f;

        [Range(5f, 500f)] public float farClip = 80f;

        [Header("이동 속도 (월드 m/s) — 다음 단계(이동)에서 사용")]
        [Tooltip("기본 걷기 속도. 0.15 = 사람 기준 체감 1.5 m/s (배율 0.1일 때).")]
        [Range(0.01f, 1f)] public float walkSpeed = 0.15f;

        [Tooltip("순간 질주 속도. 짧은 구간에서만 사용.")]
        [Range(0.01f, 2f)] public float dashSpeed = 0.45f;

        /// <summary>세상이 몇 배 크게 느껴지는지 (1 / trackingScale).</summary>
        public float Magnification => 1f / Mathf.Max(0.0001f, trackingScale);

        /// <summary>월드 값 → 플레이어 체감 값 (사람 기준 미터, m/s).</summary>
        public float ToPerceived(float worldValue) => worldValue / Mathf.Max(0.0001f, trackingScale);

        /// <summary>플레이어 체감 값 → 월드 값.</summary>
        public float ToWorld(float perceivedValue) => perceivedValue * trackingScale;

        void OnValidate()
        {
            // CharacterController 제약: height >= 2 * radius, stepOffset <= height
            bodyHeight = Mathf.Max(bodyHeight, bodyRadius * 2f + 0.001f);
            stepOffset = Mathf.Min(stepOffset, bodyHeight * 0.5f);
            skinWidth = Mathf.Clamp(skinWidth, 0.0005f, bodyRadius * 0.5f);
            farClip = Mathf.Max(farClip, nearClip * 100f);
            dashSpeed = Mathf.Max(dashSpeed, walkSpeed);
        }
    }
}
