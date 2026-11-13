using Unity.XR.CoreUtils;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// RatScaleConfig 값을 XR 리그에 적용하는 유일한 지점.
    ///
    /// 계층 구조 (XR Origin과 Camera Offset은 반드시 스케일 1)
    ///   XR Origin (Rat)            ← XROrigin, CharacterController(월드 미터), 이 컴포넌트
    ///   └ Camera Offset            ← XROrigin이 높이를 관리 (Floor 모드에서는 0)
    ///      └ Rat Tracking Space    ← 스케일 = trackingScale, 위치/회전 = 캘리브레이션 결과
    ///         ├ Main Camera        ← TrackedPoseDriver가 실제 HMD 값을 그대로 씀 (우리는 건드리지 않음)
    ///         ├ Left Controller
    ///         └ Right Controller
    ///
    /// 스케일을 XR Origin 루트가 아니라 Tracking Space에만 주는 이유:
    /// CharacterController·이동 속도·XROrigin 계산이 모두 월드 미터 그대로 유지되어
    /// "월드 스케일 / 속도 / 충돌체"를 하나의 좌표계로 다룰 수 있다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public class RatScaleRig : MonoBehaviour
    {
        [SerializeField] RatScaleConfig config;
        [SerializeField] XROrigin xrOrigin;

        [Tooltip("스케일을 적용할 추적 공간. Main Camera와 컨트롤러의 부모.")]
        [SerializeField] Transform trackingSpace;

        [Tooltip("쥐 몸 충돌체. XR Origin 루트에 있어야 한다 (다음 단계의 이동이 이 컨트롤러로 움직인다).")]
        [SerializeField] CharacterController body;

        [SerializeField] Camera eyeCamera;

        public RatScaleConfig Config => config;
        public Transform TrackingSpace => trackingSpace;
        public Camera EyeCamera => eyeCamera;
        public CharacterController Body => body;
        public XROrigin Origin => xrOrigin;

        public bool IsValid => config != null && trackingSpace != null && eyeCamera != null;

        void Reset()
        {
            AutoAssign();
        }

        void Awake()
        {
            AutoAssign();
            if (!IsValid)
            {
                Debug.LogError("[PetitParis] RatScaleRig: Config / Tracking Space / Eye Camera 중 비어 있는 참조가 있습니다. " +
                               "메뉴 PetitParis > 1. Build Test Scene으로 씬을 다시 만들거나 Inspector에서 연결하세요.", this);
                return;
            }

            ValidateHierarchy();
            ApplyScaleSettings();
        }

        /// <summary>비어 있는 참조를 계층 구조에서 찾아 채운다.</summary>
        public void AutoAssign()
        {
            if (xrOrigin == null) xrOrigin = GetComponent<XROrigin>();
            if (body == null) body = GetComponent<CharacterController>();
            if (eyeCamera == null && xrOrigin != null) eyeCamera = xrOrigin.Camera;
            if (eyeCamera == null) eyeCamera = GetComponentInChildren<Camera>(true);
            if (trackingSpace == null && eyeCamera != null) trackingSpace = eyeCamera.transform.parent;
        }

        /// <summary>
        /// Config의 스케일·충돌체·클리핑 값을 적용한다. 시작할 때 한 번 호출된다 (매 프레임 아님).
        /// 에디터의 씬 빌더도 같은 함수를 호출해 Inspector 값이 Config와 일치하게 한다.
        /// </summary>
        public void ApplyScaleSettings()
        {
            if (!IsValid) return;

            trackingSpace.localScale = Vector3.one * config.trackingScale;

            eyeCamera.nearClipPlane = config.nearClip;
            eyeCamera.farClipPlane = config.farClip;

            if (body != null)
            {
                // 반지름 → 높이 → 스텝 순서 (CharacterController 제약 경고 방지)
                body.radius = config.bodyRadius;
                body.height = config.bodyHeight;
                body.skinWidth = config.skinWidth;
                body.stepOffset = config.stepOffset;
                body.slopeLimit = config.slopeLimit;
                body.minMoveDistance = 0f;
                body.center = new Vector3(0f, config.bodyHeight * 0.5f + config.skinWidth, 0f);
            }
        }

        void ValidateHierarchy()
        {
            if (!Approximately(transform.lossyScale, Vector3.one))
                Debug.LogWarning("[PetitParis] XR Origin의 스케일은 (1,1,1)이어야 합니다. 쥐 크기는 RatScaleConfig.trackingScale로만 조절하세요.", this);

            Transform parent = trackingSpace.parent;
            if (parent != null && !Approximately(parent.lossyScale, Vector3.one))
                Debug.LogWarning("[PetitParis] Camera Offset의 스케일은 (1,1,1)이어야 합니다.", parent);

            if (!eyeCamera.transform.IsChildOf(trackingSpace))
                Debug.LogWarning("[PetitParis] Main Camera가 Rat Tracking Space의 자식이 아닙니다. 쥐 스케일이 적용되지 않습니다.", eyeCamera);
        }

        static bool Approximately(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-6f;

        void OnDrawGizmosSelected()
        {
            if (config == null) return;
            // 목표 눈높이 (노란 점)와 몸 충돌체 범위 (초록 원) 표시
            Vector3 up = transform.up;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + up * config.targetEyeHeight, 0.008f);
            Gizmos.color = Color.green;
            Vector3 bottom = transform.position + up * (config.bodyRadius + config.skinWidth);
            Vector3 top = transform.position + up * (config.bodyHeight - config.bodyRadius + config.skinWidth);
            Gizmos.DrawWireSphere(bottom, config.bodyRadius);
            Gizmos.DrawWireSphere(top, config.bodyRadius);
            Gizmos.DrawLine(bottom + transform.right * config.bodyRadius, top + transform.right * config.bodyRadius);
            Gizmos.DrawLine(bottom - transform.right * config.bodyRadius, top - transform.right * config.bodyRadius);
        }
    }
}
