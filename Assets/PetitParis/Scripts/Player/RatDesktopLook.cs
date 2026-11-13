using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace PetitParis
{
    /// <summary>
    /// PC 대체 모드(헤드셋 없음) 전용 마우스 시점 전환.
    /// - 마우스 오른쪽 버튼을 누른 채 움직이면: 좌우 = XR Origin Yaw 회전, 위아래 = 카메라 Pitch
    /// - 헤드셋 추적이 1초 넘게 없으면 PC 모드로 본다. 추적이 들어오면(VR 착용, XR Device Simulator) 바로 VR 모드로 돌아간다
    ///   (PC에 OpenXR·Meta Link가 깔려 있어 XR 자체는 켜져 있어도, 헤드셋 추적이 없으면 PC 모드)
    /// - Quest(Android) 빌드에서는 동작하지 않는다
    ///
    /// 왜 TrackedPoseDriver를 끄나:
    ///   Input System 1.18의 TrackedPoseDriver는 장치가 없으면 처음 카메라 자세를 매 프레임 다시 써서
    ///   마우스로 돌린 값을 덮어쓴다. 그래서 PC 모드에서만 카메라의 TrackedPoseDriver를 끄고,
    ///   장치가 연결되면 다시 켠다. (VR에서 "카메라는 TrackedPoseDriver만 쓴다" 규칙은 그대로 유지)
    /// </summary>
    [DisallowMultipleComponent]
    public class RatDesktopLook : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;

        [Tooltip("오른쪽 마우스 버튼을 누를 때만 시점 전환 (끄면 항상).")]
        [SerializeField] bool requireRightMouse = true;

        [Tooltip("마우스 1픽셀당 회전 각도(도).")]
        [SerializeField, Range(0.02f, 1f)] float sensitivity = 0.15f;

        [SerializeField, Range(10f, 89f)] float maxPitch = 75f;
        [SerializeField] bool invertY;

        TrackedPoseDriver headDriver;
        float pitch;
        float untrackedTime;
        bool loggedMode;

        public bool IsDesktopMode { get; private set; }

        void Awake()
        {
            if (rig == null) rig = GetComponent<RatScaleRig>();
            if (input == null) input = GetComponent<RatInputReader>();
            if (rig != null && rig.EyeCamera != null) headDriver = rig.EyeCamera.GetComponent<TrackedPoseDriver>();
        }

        void Update()
        {
            if (rig == null || !rig.IsValid) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            return; // Quest 빌드: 항상 머리 추적만 사용
#else
            bool headTracked = input != null && input.IsHeadTracked;
            untrackedTime = headTracked ? 0f : untrackedTime + Time.unscaledDeltaTime;
            bool desktop = untrackedTime > 1f;
            if (desktop != IsDesktopMode) SetDesktopMode(desktop);
#endif
            if (!IsDesktopMode) return;

            var mouse = Mouse.current;
            if (mouse == null) return;
            if (requireRightMouse && !mouse.rightButton.isPressed) return;

            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            Transform cam = rig.EyeCamera.transform;
            if (Mathf.Abs(delta.x) > 0f)
                rig.transform.RotateAround(cam.position, rig.transform.up, delta.x); // 이동 방향도 함께 바뀜
            pitch = Mathf.Clamp(pitch + (invertY ? delta.y : -delta.y), -maxPitch, maxPitch);
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void SetDesktopMode(bool desktop)
        {
            IsDesktopMode = desktop;
            if (desktop || loggedMode)
            {
                Debug.Log(desktop
                    ? "[PetitParis] PC 모드: 마우스 오른쪽 버튼을 누른 채 움직이면 시점이 돕니다 (WASD 이동). Game 창을 한 번 클릭해 두세요."
                    : "[PetitParis] 헤드셋 추적 감지 → VR 모드 (머리 회전으로 시점)", this);
                loggedMode = true;
            }
            if (headDriver != null) headDriver.enabled = !desktop;
            if (!desktop) pitch = 0f;
        }

        void OnDisable()
        {
            if (headDriver != null) headDriver.enabled = true;
            IsDesktopMode = false;
        }
    }
}
