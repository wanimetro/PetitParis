using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// VR 편안함 설정 (켜고 끌 수 있는 옵션 모음). 멀미 테스트 결과에 따라 이 에셋만 바꾸면 된다.
    /// - 스냅 회전 각도 (기본 30도)
    /// - 질주/점프 기능 사용 여부
    /// - Comfort Vignette (질주·달리기·걷기·공중) 사용 여부
    /// - 체감 속도 상한
    /// - 편안함 모드: 게임 안 X 메뉴에서 켜고 끈다 (RatComfortRuntime, 기기에 저장).
    ///   켜면 이동 속도를 줄이고, 걷기에도 비네트를 켜고, 점프를 끄고, 체감 속도 상한을 낮춘다.
    ///   코드는 enableJump 등을 직접 읽지 말고 Effective… 값을 읽는다.
    /// 카메라 Roll/Pitch/Head Bob은 어떤 옵션으로도 추가하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "RatComfortSettings", menuName = "PetitParis/Rat Comfort Settings")]
    public class RatComfortSettings : ScriptableObject
    {
        /// <summary>기본값을 바꿀 때 올린다. 빌더가 이전 버전 에셋의 값을 새 기본값으로 옮긴다.</summary>
        public const int CurrentVersion = 2;
        [HideInInspector] public int settingsVersion; // 이전 에셋(필드 없음)은 0으로 읽힌다

        [Header("회전")]
        [Tooltip("오른쪽 스틱 스냅 회전 각도. 기본 30도.")]
        [Range(10f, 90f)] public float snapTurnDegrees = 30f;

        [Tooltip("스냅 회전 사이의 최소 간격(초).")]
        [Range(0.1f, 1.5f)] public float snapTurnDebounce = 0.5f;

        [Header("기능 사용")]
        public bool enableDash = true;

        [Tooltip("누르고 있는 동안 빨라지는 달리기 (VR 왼손 그립 / PC 왼쪽 Shift).")]
        public bool enableRun = true;

        [Tooltip("멀미가 심하면 끈다. 끄면 A 버튼이 무시된다.")]
        public bool enableJump = true;

        [Header("Comfort Vignette (시야 가장자리 어둡게)")]
        public bool vignetteOnDash = true;
        [Tooltip("달리기(그립) 중 비네트.")]
        public bool vignetteOnRun = true;
        public bool vignetteOnWalk = false;
        [Tooltip("공중(점프·낙하) 비네트. 주방 널빤지에서 떨어질 때 멀미를 줄인다.")]
        public bool vignetteInAir = true;

        [Header("속도 제한")]
        [Tooltip("체감 속도(사람 기준 m/s) 상한을 적용한다.")]
        public bool limitSpeed = true;

        [Tooltip("체감 속도 상한 (사람 기준 m/s). 배율 0.1이면 6.5 → 월드 0.65 m/s. 질주(0.6)가 달리기(0.36)보다 확실히 빠르도록 질주보다 높게 둔다.")]
        [Range(1f, 10f)] public float maxPerceivedSpeed = 6.5f;

        [Header("편안함 모드 (게임 안 X 메뉴에서 전환)")]
        [Tooltip("편안함 모드에서 걷기·달리기 속도 배율.")]
        [Range(0.3f, 1f)] public float comfortSpeedMultiplier = 0.7f;
        [Tooltip("편안함 모드에서 체감 속도 상한 (사람 기준 m/s).")]
        [Range(1f, 10f)] public float comfortMaxPerceivedSpeed = 3.5f;
        [Tooltip("편안함 모드에서 점프를 끈다.")]
        public bool comfortDisableJump = true;
        [Tooltip("편안함 모드에서 걷기에도 비네트를 켠다.")]
        public bool comfortVignetteOnWalk = true;

        // ───── 실제로 쓰는 값 (편안함 모드 반영) ─────
        public bool ComfortMode => RatComfortRuntime.ComfortMode;
        public bool EffectiveJumpEnabled => enableJump && !(ComfortMode && comfortDisableJump);
        public bool EffectiveVignetteOnWalk => vignetteOnWalk || (ComfortMode && comfortVignetteOnWalk);
        public float EffectiveSpeedMultiplier => ComfortMode ? comfortSpeedMultiplier : 1f;
        public float EffectiveMaxPerceivedSpeed => ComfortMode ? Mathf.Min(maxPerceivedSpeed, comfortMaxPerceivedSpeed) : maxPerceivedSpeed;

        /// <summary>이전 버전 에셋을 새 기본값으로 옮긴다 (빌더가 호출). 바꿨으면 true.</summary>
        public bool MigrateToCurrent()
        {
            if (settingsVersion >= CurrentVersion) return false;
            // v2 (2026-11 AI 리뷰 ISSUE-02/03/11): 공중·달리기 비네트 기본 켬, 체감 속도 상한 5 → 6.5
            vignetteInAir = true;
            vignetteOnRun = true;
            if (maxPerceivedSpeed < 6.5f) maxPerceivedSpeed = 6.5f;
            settingsVersion = CurrentVersion;
            return true;
        }
    }

    /// <summary>
    /// 게임 중에 바꾸는 편안함 설정 (기기에 PlayerPrefs로 저장). 에셋(RatComfortSettings)은 건드리지 않는다.
    /// </summary>
    public static class RatComfortRuntime
    {
        const string Key = "PetitParis.ComfortMode";
        static bool loaded;
        static bool comfortMode;

        public static event System.Action Changed;

        public static bool ComfortMode
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    try { comfortMode = PlayerPrefs.GetInt(Key, 0) == 1; } catch { comfortMode = false; }
                }
                return comfortMode;
            }
            set
            {
                loaded = true;
                if (comfortMode == value) return;
                comfortMode = value;
                try { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); } catch { }
                Changed?.Invoke();
            }
        }
    }
}
