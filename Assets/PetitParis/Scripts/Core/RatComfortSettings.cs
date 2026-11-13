using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// VR 편안함 설정 (켜고 끌 수 있는 옵션 모음). 멀미 테스트 결과에 따라 이 에셋만 바꾸면 된다.
    /// - 스냅 회전 각도 (기본 30도)
    /// - 질주/점프 기능 사용 여부
    /// - Comfort Vignette (질주·걷기·공중) 사용 여부
    /// - 체감 속도 상한
    /// 카메라 Roll/Pitch/Head Bob은 어떤 옵션으로도 추가하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "RatComfortSettings", menuName = "PetitParis/Rat Comfort Settings")]
    public class RatComfortSettings : ScriptableObject
    {
        [Header("회전")]
        [Tooltip("오른쪽 스틱 스냅 회전 각도. 기본 30도.")]
        [Range(10f, 90f)] public float snapTurnDegrees = 30f;

        [Tooltip("스냅 회전 사이의 최소 간격(초).")]
        [Range(0.1f, 1.5f)] public float snapTurnDebounce = 0.5f;

        [Header("기능 사용")]
        public bool enableDash = true;

        [Tooltip("멀미가 심하면 끈다. 끄면 A 버튼이 무시된다.")]
        public bool enableJump = true;

        [Header("Comfort Vignette (시야 가장자리 어둡게)")]
        public bool vignetteOnDash = true;
        public bool vignetteOnWalk = false;
        public bool vignetteInAir = false;

        [Header("속도 제한")]
        [Tooltip("체감 속도(사람 기준 m/s) 상한을 적용한다.")]
        public bool limitSpeed = true;

        [Tooltip("체감 속도 상한 (사람 기준 m/s). 배율 0.1이면 월드 0.5 m/s.")]
        [Range(1f, 10f)] public float maxPerceivedSpeed = 5f;
    }
}
