using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace PetitParis
{
    /// <summary>
    /// RatComfortSettings를 실제 리그에 적용한다.
    /// 1) XRI SnapTurnProvider에 회전 각도(기본 30도)와 간격 적용
    /// 2) XRI TunnelingVignetteController용 "신호" Provider 역할:
    ///    질주·달리기·공중(옵션: 걷기, 편안함 모드) 중에만 Moving 상태가 되어 비네트를 켠다.
    ///    켜고 끌 때의 부드러운 전환은 TunnelingVignetteController의 ease in/out이 맡는다.
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

        [Tooltip("공중 비네트를 켜기 전 기다리는 시간(초). 작은 턱에서 깜빡이지 않게.")]
        [SerializeField, Range(0f, 0.5f)] float airGrace = 0.12f;

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

            bool airborne = locomotion != null && locomotion.Body != null && locomotion.Body.enabled && !locomotion.IsGrounded
                            && locomotion.TimeAirborne > airGrace; // 계단·턱의 아주 짧은 공중은 무시
            bool want =
                (comfort.vignetteOnDash && dash != null && dash.IsDashing) ||
                (comfort.vignetteOnRun && locomotion != null && locomotion.IsRunning && locomotion.PlanarSpeed > walkSpeedThreshold) ||
                (comfort.EffectiveVignetteOnWalk && locomotion != null && locomotion.PlanarSpeed > walkSpeedThreshold) ||
                (comfort.vignetteInAir && airborne);
            VignetteRequested = want;

            if (want && locomotionState != LocomotionState.Moving) TryStartLocomotionImmediately();
            else if (!want && locomotionState == LocomotionState.Moving) TryEndLocomotion();
        }
    }
}
