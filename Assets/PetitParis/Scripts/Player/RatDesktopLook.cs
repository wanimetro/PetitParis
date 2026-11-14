using UnityEngine;
using UnityEngine.InputSystem;

namespace PetitParis
{
    /// <summary>
    /// PC 대체 모드(헤드셋 없음) 전용 마우스 시점 전환.
    /// - 마우스 오른쪽 버튼을 누른 채 움직이면: 좌우 = XR Origin Yaw 회전, 위아래 = Rat Tracking Space Pitch
    /// - 헤드셋 추적이 1초 넘게 없으면 PC 모드로 본다. 추적이 들어오면(VR 착용, XR Device Simulator) 바로 VR 모드로 돌아간다
    ///   (PC에 OpenXR·Meta Link가 깔려 있어 XR 자체는 켜져 있어도, 헤드셋 추적이 없으면 PC 모드)
    /// - Quest(Android) 빌드에서는 동작하지 않는다
    ///
    /// 카메라는 건드리지 않는다:
    ///   Input System 1.18의 TrackedPoseDriver는 장치가 없으면 처음 카메라 자세를 매 프레임(렌더링 직전 포함) 다시 쓴다.
    ///   그래서 카메라 대신 카메라의 부모(Rat Tracking Space)를 눈 위치 기준으로 기울여 위아래 시점을 만든다.
    ///   PC 모드에서 카메라 로컬 위치는 원점이라, 부모를 회전해도 눈 위치는 그대로다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public class RatDesktopLook : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;
        [SerializeField] RatEyeCalibrator calibrator;

        [Tooltip("오른쪽 마우스 버튼을 누를 때만 시점 전환 (끄면 항상).")]
        [SerializeField] bool requireRightMouse = true;

        [Tooltip("마우스 1픽셀당 회전 각도(도).")]
        [SerializeField, Range(0.02f, 1f)] float sensitivity = 0.2f;

        [SerializeField, Range(10f, 89f)] float maxPitch = 75f;
        [SerializeField] bool invertY;

        float pitch;
        float untrackedTime;
        bool loggedMode;

        public bool IsDesktopMode { get; private set; }

        void Awake()
        {
            if (rig == null) rig = GetComponent<RatScaleRig>();
            if (input == null) input = GetComponent<RatInputReader>();
            if (calibrator == null) calibrator = GetComponent<RatEyeCalibrator>();
        }

        void OnEnable()
        {
            if (calibrator != null) calibrator.Calibrated += OnCalibrated;
        }

        void OnDisable()
        {
            if (calibrator != null) calibrator.Calibrated -= OnCalibrated;
            if (IsDesktopMode) SetPitch(0f);
            IsDesktopMode = false;
        }

        // 캘리브레이션(시작·리스폰)은 Tracking Space를 수평으로 되돌리므로 기울기 값도 0으로 맞춘다
        void OnCalibrated() => pitch = 0f;

        void LateUpdate()
        {
            if (rig == null || !rig.IsValid) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            return; // Quest 빌드: 항상 머리 추적만 사용
#else
            bool headTracked = input != null && input.IsHeadTracked;
            untrackedTime = headTracked ? 0f : untrackedTime + Time.unscaledDeltaTime;
            bool desktop = untrackedTime > 1f;
            if (desktop != IsDesktopMode) SetDesktopMode(desktop);
            if (!IsDesktopMode) return;

            var mouse = Mouse.current;
            if (mouse == null) return;
            if (requireRightMouse && !mouse.rightButton.isPressed) return;

            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            if (Mathf.Abs(delta.x) > 0f)
                rig.transform.RotateAround(rig.EyeCamera.transform.position, rig.transform.up, delta.x); // 이동 방향도 함께 바뀜
            if (Mathf.Abs(delta.y) > 0f)
                SetPitch(Mathf.Clamp(pitch + (invertY ? delta.y : -delta.y), -maxPitch, maxPitch));
#endif
        }

        void SetPitch(float value)
        {
            pitch = value;
            Transform space = rig.TrackingSpace;
            Vector3 eye = rig.EyeCamera.transform.position;
            float yaw = space.localEulerAngles.y;
            space.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            // 눈 위치가 바뀌지 않도록 회전 후 다시 맞춘다
            space.position += eye - rig.EyeCamera.transform.position;
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
            if (!desktop) SetPitch(0f);
        }
    }
}
