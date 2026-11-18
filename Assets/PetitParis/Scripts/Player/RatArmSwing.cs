using UnityEngine;
using UnityEngine.XR;

namespace PetitParis
{
    /// <summary>
    /// 양손 흔들기 달리기 (Quest Touch 컨트롤러).
    /// - 두 컨트롤러가 모두 머리 기준으로 빠르게 움직이면(팔을 앞뒤로 흔들면) SwingFactor가 0→1로 오른다.
    /// - 한 손만 흔들거나, 고개·몸만 움직이면 0 (머리 움직임을 빼고 계산).
    /// - 실제 이동은 하지 않는다. RatLocomotionController가 SwingFactor를 읽어 속도 배율로 쓴다 (이동 경로는 하나).
    /// - 손 위치는 추적 공간의 실제 미터 (쥐 스케일과 무관). 장치가 없으면(PC 모드) 항상 0.
    /// UnityEngine.XR.InputDevices를 쓴다 (RatHaptics와 같은 방식, 패키지 추가 없음). Quest 실기기에서 기준값 확인 필요.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatArmSwing : MonoBehaviour
    {
        [SerializeField] bool enableArmSwing = true;
        [Tooltip("이 속도(m/s, 머리 기준 손 속도) 이상으로 두 손을 흔들면 빨라지기 시작한다.")]
        [SerializeField, Range(0.1f, 2f)] float minHandSpeed = 0.3f; // Quest 2 실측: 가만히 0.0~0.1, 흔들 때 0.7~1.4 m/s (2026-11-18)
        [Tooltip("이 속도에서 최고 배율.")]
        [SerializeField, Range(0.3f, 4f)] float fullHandSpeed = 0.9f;
        [Tooltip("손 속도 평활 시간(초). 흔들기 한 번의 정지 순간에 끊기지 않게.")]
        [SerializeField, Range(0.05f, 1f)] float smoothTime = 0.25f;
        [Tooltip("흔들기를 멈춘 뒤 원래 속도로 돌아오는 시간(초).")]
        [SerializeField, Range(0.05f, 2f)] float releaseTime = 0.4f;

        Vector3 lastLeft, lastRight, lastHead;
        bool hasLast;
        float leftSpeed, rightSpeed;

        /// <summary>0 = 흔들지 않음, 1 = 힘껏 흔드는 중.</summary>
        public float SwingFactor { get; private set; }
        public bool IsSwinging => SwingFactor > 0.05f;
        // 실기기 진단용 (RatQuestDiagnostics)
        public float LeftHandSpeed => leftSpeed;
        public float RightHandSpeed => rightSpeed;
        public bool DevicesFound { get; private set; }
        public string MissingNode { get; private set; } = "";
        public string Source { get; private set; } = "";

        RatInputReader input;

        void OnDisable()
        {
            hasLast = false;
            SwingFactor = leftSpeed = rightSpeed = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            // 1순위: 손 모델을 움직이는 Input System 액션과 같은 값 (실기기에서 손이 따라오면 여기서도 읽힌다)
            // 2순위: UnityEngine.XR.InputDevices (예전 방식)
            if (input == null) input = GetComponentInParent<RatInputReader>();
            bool hasL, hasR, hasH;
            Vector3 l, r, h;
            if (input != null && input.IsLeftPositionTracked && input.IsRightPositionTracked && input.IsHeadTracked)
            {
                l = input.RawLeftPosition; r = input.RawRightPosition; h = input.RawHeadPosition;
                hasL = hasR = hasH = true;
                Source = "InputSystem";
            }
            else
            {
                hasL = TryGet(XRNode.LeftHand, out l); hasR = TryGet(XRNode.RightHand, out r); hasH = TryGet(XRNode.Head, out h);
                Source = "XRInputDevices";
            }
            DevicesFound = hasL && hasR && hasH;
            MissingNode = (hasL ? "" : "L") + (hasR ? "" : "R") + (hasH ? "" : "H");
            if (!enableArmSwing || dt <= 0f || !DevicesFound)
            {
                hasLast = false;
                SwingFactor = Mathf.MoveTowards(SwingFactor, 0f, dt / Mathf.Max(0.01f, releaseTime));
                return;
            }

            if (hasLast)
            {
                Vector3 headDelta = h - lastHead;
                float k = 1f - Mathf.Exp(-dt / smoothTime);
                leftSpeed = Mathf.Lerp(leftSpeed, ((l - lastLeft) - headDelta).magnitude / dt, k);
                rightSpeed = Mathf.Lerp(rightSpeed, ((r - lastRight) - headDelta).magnitude / dt, k);
            }
            lastLeft = l; lastRight = r; lastHead = h; hasLast = true;

            // 두 손이 모두 움직여야 한다 → 느린 쪽 기준
            float target = Mathf.InverseLerp(minHandSpeed, fullHandSpeed, Mathf.Min(leftSpeed, rightSpeed));
            SwingFactor = target > SwingFactor
                ? Mathf.MoveTowards(SwingFactor, target, dt / 0.15f)
                : Mathf.MoveTowards(SwingFactor, target, dt / Mathf.Max(0.01f, releaseTime));
        }

        static bool TryGet(XRNode node, out Vector3 position)
        {
            position = default;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return false;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position)) return true;
            // OpenXR 머리 장치는 centerEyePosition만 줄 수 있다
            return node == XRNode.Head && device.TryGetFeatureValue(CommonUsages.centerEyePosition, out position);
        }
    }
}
