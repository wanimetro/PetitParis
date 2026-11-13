using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PetitParis
{
    /// <summary>
    /// PetitParisInput.inputactions 하나만 읽는 입력 창구.
    /// - 에셋 전체(Tracking + Rat 맵)를 켜고 끈다. TrackedPoseDriver도 이 에셋의 액션을 참조한다.
    /// - 액션 이름이 빠져 있으면 시작할 때 한 번에 알려준다 (참조 누락 방지).
    /// - 프로젝트 전역 액션(InputSystem_Actions)은 읽지 않는다 → 다른 입력과 중복되지 않음.
    ///
    /// 이동(Move)은 RatLocomotionController, 회전(Turn)은 XRI SnapTurnProvider가 같은 에셋의 액션을 읽는다.
    /// 버튼 입력은 "이번 프레임에 눌림"(WasPressedThisFrame)으로 읽어 한 번 누를 때 한 번만 동작하게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public class RatInputReader : MonoBehaviour
    {
        // 액션 경로 ("맵/액션")
        public const string MoveAction = "Rat/Move";
        public const string TurnAction = "Rat/Turn";
        public const string DashAction = "Rat/Dash";
        public const string JumpAction = "Rat/Jump";
        public const string CrouchAction = "Rat/Crouch";
        public const string InteractAction = "Rat/Interact";
        public const string CalibrateAction = "Rat/Calibrate";
        public const string HeadPositionAction = "Tracking/HeadPosition";
        public const string HeadRotationAction = "Tracking/HeadRotation";
        public const string HeadTrackingStateAction = "Tracking/HeadTrackingState";
        public const string LeftPositionAction = "Tracking/LeftPosition";
        public const string LeftRotationAction = "Tracking/LeftRotation";
        public const string LeftTrackingStateAction = "Tracking/LeftTrackingState";
        public const string RightPositionAction = "Tracking/RightPosition";
        public const string RightRotationAction = "Tracking/RightRotation";
        public const string RightTrackingStateAction = "Tracking/RightTrackingState";

        public static readonly string[] RequiredActionNames =
        {
            MoveAction, TurnAction, DashAction, JumpAction, CrouchAction, InteractAction, CalibrateAction,
            HeadPositionAction, HeadRotationAction, HeadTrackingStateAction,
            LeftPositionAction, LeftRotationAction, LeftTrackingStateAction,
            RightPositionAction, RightRotationAction, RightTrackingStateAction,
        };

        [Tooltip("Assets/PetitParis/Settings/PetitParisInput.inputactions")]
        [SerializeField] InputActionAsset actions;

        InputAction move, turn, dash, jump, crouch, interact, calibrate;
        InputAction headPosition, headState, leftState, rightState;
        bool resolved;

        /// <summary>Calibrate 입력이 완료(길게 누르기 성공)되면 호출된다.</summary>
        public event Action CalibrateRequested;

        public InputActionAsset Actions => actions;

        // ── 게임플레이 입력 (다음 단계에서 사용) ──
        public Vector2 Move => ReadVector2(move);
        public Vector2 Turn => ReadVector2(turn);
        public bool DashHeld => IsPressed(dash);
        public bool JumpHeld => IsPressed(jump);
        public bool CrouchHeld => IsPressed(crouch);
        public bool InteractHeld => IsPressed(interact);
        public bool CalibrateHeld => IsPressed(calibrate);

        // ── 이번 프레임에 눌렸는지 (한 번 누르면 한 번만 동작) ──
        public bool DashPressedThisFrame => WasPressed(dash);
        public bool JumpPressedThisFrame => WasPressed(jump);
        public bool CrouchPressedThisFrame => WasPressed(crouch);
        public bool InteractPressedThisFrame => WasPressed(interact);

        // ── 추적 상태 (InputTrackingState 플래그: Position=1, Rotation=2) ──
        public int HeadTrackingState => ReadInt(headState);
        public int LeftTrackingState => ReadInt(leftState);
        public int RightTrackingState => ReadInt(rightState);
        public bool IsHeadTracked => (HeadTrackingState & 3) != 0;
        public Vector3 RawHeadPosition => headPosition != null && headPosition.enabled ? headPosition.ReadValue<Vector3>() : Vector3.zero;

        void Awake()
        {
            Resolve();
        }

        void Resolve()
        {
            resolved = true;
            if (actions == null)
            {
                Debug.LogError("[PetitParis] RatInputReader: Input Action Asset이 비어 있습니다. " +
                               "Assets/PetitParis/Settings/PetitParisInput.inputactions를 연결하세요.", this);
                return;
            }

            move = Find(MoveAction);
            turn = Find(TurnAction);
            dash = Find(DashAction);
            jump = Find(JumpAction);
            crouch = Find(CrouchAction);
            interact = Find(InteractAction);
            calibrate = Find(CalibrateAction);
            headPosition = Find(HeadPositionAction);
            headState = Find(HeadTrackingStateAction);
            leftState = Find(LeftTrackingStateAction);
            rightState = Find(RightTrackingStateAction);

            var missing = new List<string>();
            foreach (var name in RequiredActionNames)
                if (actions.FindAction(name, false) == null) missing.Add(name);
            if (missing.Count > 0)
                Debug.LogError($"[PetitParis] 입력 액션이 없습니다: {string.Join(", ", missing)}", this);
        }

        InputAction Find(string path) => actions.FindAction(path, false);

        void OnEnable()
        {
            if (!resolved) Resolve();
            if (actions == null) return;
            actions.Enable();
            if (calibrate != null) calibrate.performed += OnCalibratePerformed;
        }

        void OnDisable()
        {
            if (calibrate != null) calibrate.performed -= OnCalibratePerformed;
            if (actions != null) actions.Disable();
        }

        void OnCalibratePerformed(InputAction.CallbackContext context)
        {
            CalibrateRequested?.Invoke();
        }

        static Vector2 ReadVector2(InputAction action) =>
            action != null && action.enabled ? action.ReadValue<Vector2>() : Vector2.zero;

        static int ReadInt(InputAction action) =>
            action != null && action.enabled ? action.ReadValue<int>() : 0;

        static bool IsPressed(InputAction action) =>
            action != null && action.enabled && action.IsPressed();

        static bool WasPressed(InputAction action) =>
            action != null && action.enabled && action.WasPressedThisFrame();
    }
}
