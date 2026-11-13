using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace PetitParis
{
    /// <summary>
    /// 앉든 서든, 키가 크든 작든 같은 쥐 눈높이가 되도록 맞추는 캘리브레이션.
    ///
    /// 원리
    /// - 카메라(HMD) Transform은 TrackedPoseDriver만 쓴다. 이 스크립트는 카메라를 건드리지 않는다.
    /// - 대신 카메라의 부모인 Rat Tracking Space의 위치·회전을 "캘리브레이션할 때 한 번만" 바꾼다.
    ///   머리 실제 위치(raw) × trackingScale 만큼을 빼서, 지금 이 순간의 눈이
    ///   XR Origin 바로 위 targetEyeHeight에 오게 한다.
    /// - 이후의 머리 움직임은 그대로 반영된다 (회전 1:1, 위치는 trackingScale 배).
    ///
    /// 실행 시점
    /// - 시작: 검은 화면 → HMD 추적이 안정되면 캘리브레이션 → 페이드 인
    /// - 수동: Calibrate 입력 (왼손 Menu 버튼 1초 / Y 버튼 1초 / 키보드 F5)
    /// - Quest 재중심(Meta 버튼 길게) 이벤트가 오면 자동으로 다시 맞춘다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-800)]
    public class RatEyeCalibrator : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;
        [SerializeField] ScreenFader fader;

        [Header("자동 캘리브레이션")]
        [SerializeField] bool autoCalibrateOnStart = true;

        [Tooltip("HMD 추적을 기다리는 최대 시간(초). 넘으면 현재 자세로 맞춘다.")]
        [SerializeField, Range(0.5f, 10f)] float startupTimeout = 4f;

        [Tooltip("추적이 시작된 뒤 값이 안정되기를 기다리는 시간(초).")]
        [SerializeField, Range(0f, 2f)] float trackingSettleTime = 0.3f;

        [Tooltip("XR 장치가 없으면(데스크톱 Play) 이 시간 뒤 바로 맞춘다.")]
        [SerializeField, Range(0f, 2f)] float desktopDetectTime = 0.5f;

        [Tooltip("Quest 재중심(tracking origin 변경) 이벤트가 오면 다시 캘리브레이션한다.")]
        [SerializeField] bool recalibrateOnRecenter = true;

        [Header("페이드 (시점이 순간 이동할 때 멀미 방지)")]
        [SerializeField, Range(0f, 1f)] float fadeOutTime = 0.2f;
        [SerializeField, Range(0f, 1f)] float fadeInTime = 0.35f;

        /// <summary>캘리브레이션이 끝날 때마다 호출된다.</summary>
        public event Action Calibrated;

        /// <summary>마지막 캘리브레이션 때의 실제 머리 높이 (추적 공간 단위 = 실제 미터).</summary>
        public float LastRawHeadHeight { get; private set; }
        public float LastCalibrationTime { get; private set; } = -1f;
        public int CalibrationCount { get; private set; }
        public bool IsCalibrating => busy;

        bool busy;
        readonly List<UnityEngine.XR.XRInputSubsystem> subsystems = new List<UnityEngine.XR.XRInputSubsystem>();

        void Awake()
        {
            if (rig == null) rig = GetComponent<RatScaleRig>();
            if (input == null) input = GetComponent<RatInputReader>();
            if (fader == null) fader = GetComponentInChildren<ScreenFader>(true);
            if (input == null)
                Debug.LogWarning("[PetitParis] RatEyeCalibrator: RatInputReader가 없어 수동 캘리브레이션 입력을 받을 수 없습니다.", this);
        }

        void OnEnable()
        {
            if (input != null) input.CalibrateRequested += Calibrate;
        }

        void OnDisable()
        {
            if (input != null) input.CalibrateRequested -= Calibrate;
            UnsubscribeRecenter();
            busy = false;
        }

        IEnumerator Start()
        {
            if (rig == null || !rig.IsValid)
            {
                Debug.LogError("[PetitParis] RatEyeCalibrator: RatScaleRig가 유효하지 않아 캘리브레이션을 건너뜁니다.", this);
                if (fader != null) fader.SetAlpha(0f);
                yield break;
            }

            if (autoCalibrateOnStart)
            {
                busy = true;
                if (fader != null) fader.SetAlpha(1f);
                yield return WaitForHeadTracking();
                CalibrateImmediate();
                busy = false;
            }

            if (fader != null) yield return fader.Fade(0f, fadeInTime);

            if (recalibrateOnRecenter) SubscribeRecenter();
        }

        IEnumerator WaitForHeadTracking()
        {
            float elapsed = 0f;
            float trackedFor = 0f;
            while (elapsed < startupTimeout)
            {
                bool xrPresent = UnityEngine.XR.XRSettings.isDeviceActive || InputSystem.GetDevice<XRHMD>() != null;
                if (!xrPresent && elapsed >= desktopDetectTime)
                    yield break; // 헤드셋 없음: 데스크톱 모드로 바로 진행

                if (input != null && input.IsHeadTracked)
                {
                    trackedFor += Time.unscaledDeltaTime;
                    if (trackedFor >= trackingSettleTime) yield break;
                }
                else
                {
                    trackedFor = 0f;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning("[PetitParis] HMD 추적을 기다리다 시간이 초과되어 현재 자세로 캘리브레이션합니다.", this);
        }

        /// <summary>페이드 아웃 → 캘리브레이션 → 페이드 인. 진행 중이면 무시한다.</summary>
        public void Calibrate()
        {
            if (!isActiveAndEnabled || busy) return;
            StartCoroutine(CalibrateRoutine());
        }

        IEnumerator CalibrateRoutine()
        {
            busy = true;
            if (fader != null) yield return fader.Fade(1f, fadeOutTime);
            CalibrateImmediate();
            yield return null; // 새 위치로 한 프레임 렌더링된 뒤 밝힌다
            if (fader != null) yield return fader.Fade(0f, fadeInTime);
            busy = false;
        }

        /// <summary>
        /// 페이드 없이 즉시 캘리브레이션한다. Rat Tracking Space의 로컬 Transform만 바꾼다.
        /// </summary>
        public void CalibrateImmediate()
        {
            if (rig == null || !rig.IsValid) return;

            RatScaleConfig cfg = rig.Config;
            Transform space = rig.TrackingSpace;
            Transform parent = space.parent;
            Transform cam = rig.EyeCamera.transform;
            Transform root = rig.transform;
            float s = cfg.trackingScale;

            // 1) 추적 공간 기준 머리의 실제 위치·회전 (= TrackedPoseDriver가 쓴 원본 값, 실제 미터)
            Vector3 rawPos = space.InverseTransformPoint(cam.position);
            Quaternion rawRot = Quaternion.Inverse(space.rotation) * cam.rotation;
            LastRawHeadHeight = rawPos.y;

            // 2) 바라보는 방향을 XR Origin 정면에 맞출 Yaw 회전 (Pitch·Roll은 절대 건드리지 않음)
            Quaternion yaw = space.localRotation;
            if (cfg.alignYawOnCalibrate)
            {
                Vector3 forward = rawRot * Vector3.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 1e-4f)
                {
                    // 바로 아래/위를 보고 있으면 머리 윗방향으로 수평 방향을 추정
                    forward = rawRot * Vector3.up;
                    forward.y = 0f;
                }
                if (forward.sqrMagnitude > 1e-6f)
                {
                    float yawDeg = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                    yaw = Quaternion.Euler(0f, -yawDeg, 0f);
                }
            }

            // 3) 목표 눈 위치: XR Origin 바로 위 targetEyeHeight (월드 미터)
            Vector3 targetWorld = root.position + root.up * cfg.targetEyeHeight;
            Vector3 targetInParent = parent != null ? parent.InverseTransformPoint(targetWorld) : targetWorld;

            // 4) 눈이 목표에 오도록 추적 공간을 배치 (camInParent = position + yaw * (raw * s))
            space.localScale = Vector3.one * s;
            space.localRotation = yaw;
            space.localPosition = targetInParent - yaw * (rawPos * s);

            LastCalibrationTime = Time.unscaledTime;
            CalibrationCount++;
            Debug.Log($"[PetitParis] 캘리브레이션 #{CalibrationCount}: 실제 머리 높이 {rawPos.y:F2} m → 쥐 눈높이 " +
                      $"{cfg.targetEyeHeight * 100f:F1} cm (세상 x{cfg.Magnification:F1})", this);
            Calibrated?.Invoke();
        }

        void SubscribeRecenter()
        {
            UnsubscribeRecenter();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
                subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
        }

        void UnsubscribeRecenter()
        {
            foreach (var subsystem in subsystems)
            {
                if (subsystem != null) subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            }
            subsystems.Clear();
        }

        void OnTrackingOriginUpdated(UnityEngine.XR.XRInputSubsystem subsystem)
        {
            // 직전 캘리브레이션 직후 연달아 오는 이벤트는 무시
            if (Time.unscaledTime - LastCalibrationTime < 0.5f) return;
            Calibrate();
        }
    }
}
