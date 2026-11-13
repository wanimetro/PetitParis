using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 웅크리기 (B 버튼). 사용자의 실제 머리나 카메라를 낮추지 않는다. 앉은 자세에서도 똑같이 동작한다.
    ///
    /// 웅크리면 바뀌는 것
    /// - 몸 충돌체 반지름 축소 (radiusScale) → 몸통보다 좁은 틈을 통과할 수 있다
    ///   (카메라 높이는 그대로라, 눈높이보다 낮은 틈은 설계상 만들지 않는다)
    /// - 이동 속도 감소 (speedMultiplier)
    /// - "웅크려야 숨을 수 있는" 안전 구역(RatSafeZone.requireCrouch) 조건 충족
    /// 일어설 공간이 없으면(틈 안) 웅크린 상태를 유지하고, 공간이 생기면 자동으로 일어선다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatCrouchController : MonoBehaviour
    {
        public enum CrouchMode { Toggle, Hold }

        const int IgnoreRaycastLayer = 2;

        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;

        [Tooltip("Toggle: B를 누를 때마다 전환 / Hold: 누르고 있는 동안만.")]
        [SerializeField] CrouchMode mode = CrouchMode.Toggle;

        [SerializeField, Range(0.1f, 1f)] float speedMultiplier = 0.5f;

        [Tooltip("웅크렸을 때 몸 반지름 비율. 0.7 → 지름 7cm가 약 5cm로.")]
        [SerializeField, Range(0.4f, 1f)] float radiusScale = 0.7f;

        [Tooltip("일어설 공간 검사 대상 레이어. 플레이어(Ignore Raycast)는 제외.")]
        [SerializeField] LayerMask obstacleMask = ~(1 << IgnoreRaycastLayer);

        bool wantsToStand;

        public bool IsCrouched { get; private set; }
        public bool IsWaitingToStand => IsCrouched && wantsToStand;
        public float SpeedMultiplier => IsCrouched ? speedMultiplier : 1f;

        public event Action<bool> CrouchChanged;

        void Awake()
        {
            if (rig == null) rig = GetComponentInParent<RatScaleRig>();
            if (input == null) input = GetComponentInParent<RatInputReader>();
        }

        void Update()
        {
            if (input != null)
            {
                if (mode == CrouchMode.Toggle)
                {
                    if (input.CrouchPressedThisFrame)
                    {
                        if (IsCrouched) TryStand();
                        else SetCrouched(true);
                    }
                }
                else
                {
                    if (input.CrouchHeld && !IsCrouched) SetCrouched(true);
                    else if (!input.CrouchHeld && IsCrouched && !wantsToStand) TryStand();
                }
            }

            if (wantsToStand) TryStand();
        }

        /// <summary>일어서기 시도. 공간이 없으면 false, 공간이 생기면 자동으로 일어선다.</summary>
        public bool TryStand()
        {
            if (!IsCrouched) return true;
            if (HasRoomToStand())
            {
                SetCrouched(false);
                return true;
            }
            wantsToStand = true;
            return false;
        }

        /// <summary>리스폰 때 공간 검사 없이 일어선다.</summary>
        public void ForceStand()
        {
            if (IsCrouched) SetCrouched(false);
            wantsToStand = false;
        }

        void SetCrouched(bool crouched)
        {
            IsCrouched = crouched;
            wantsToStand = false;
            ApplyRadius();
            CrouchChanged?.Invoke(crouched);
        }

        void ApplyRadius()
        {
            if (rig == null || rig.Body == null || rig.Config == null) return;
            rig.Body.radius = rig.Config.bodyRadius * (IsCrouched ? radiusScale : 1f);
        }

        bool HasRoomToStand()
        {
            if (rig == null || rig.Body == null || rig.Config == null) return true;
            var body = rig.Body;
            float standRadius = rig.Config.bodyRadius;
            Vector3 up = body.transform.up;
            Vector3 center = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0f, body.height * 0.5f - standRadius);
            return !Physics.CheckCapsule(center + up * half, center - up * half, standRadius - 0.001f,
                obstacleMask, QueryTriggerInteraction.Ignore);
        }
    }
}
