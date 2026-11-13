using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace PetitParis
{
    /// <summary>
    /// RatComfortSettings를 실제 리그에 적용한다.
    /// 1) XRI SnapTurnProvider에 회전 각도(기본 30도)와 간격 적용
    /// 2) XRI TunnelingVignetteController용 "신호" Provider 역할:
    ///    질주(또는 옵션: 걷기/공중) 중에만 Moving 상태가 되어 비네트를 켠다.
    ///    이 Provider는 Transformation을 큐에 넣지 않는다 → 이동이 중복되지 않는다.
    /// LocomotionProvider의 Awake/OnEnable을 가리지 않도록 Update만 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatComfortApplier : LocomotionProvider
    {
        [SerializeField] RatComfortSettings comfort;
        [SerializeField] SnapTurnProvider snapTurn;
        [SerializeField] RatLocomotionController locomotion;
        [SerializeField] RatDashController dash;

        [Tooltip("걷기 비네트를 켤 최소 속도 (월드 m/s).")]
        [SerializeField, Range(0f, 0.2f)] float walkSpeedThreshold = 0.03f;

        bool applied;
        float appliedDegrees = -1f;

        public bool VignetteRequested { get; private set; }

        void Update()
        {
            if (comfort == null) return;

            // 설정값이 바뀌면(Inspector에서 Play 중 조정) 다시 적용
            if (!applied || !Mathf.Approximately(appliedDegrees, comfort.snapTurnDegrees))
            {
                if (snapTurn != null)
                {
                    snapTurn.turnAmount = comfort.snapTurnDegrees;
                    snapTurn.debounceTime = comfort.snapTurnDebounce;
                }
                appliedDegrees = comfort.snapTurnDegrees;
                applied = true;
            }

            bool want =
                (comfort.vignetteOnDash && dash != null && dash.IsDashing) ||
                (comfort.vignetteOnWalk && locomotion != null && locomotion.PlanarSpeed > walkSpeedThreshold) ||
                (comfort.vignetteInAir && locomotion != null && !locomotion.IsGrounded);
            VignetteRequested = want;

            if (want && locomotionState != LocomotionState.Moving) TryStartLocomotionImmediately();
            else if (!want && locomotionState == LocomotionState.Moving) TryEndLocomotion();
        }
    }
}
