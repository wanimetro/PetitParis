using UnityEditor;
using UnityEngine;

namespace PetitParis.EditorTools
{
    /// <summary>RatScaleConfig Inspector 아래에 "플레이어 체감 값"을 함께 보여준다.</summary>
    [CustomEditor(typeof(RatScaleConfig))]
    public class RatScaleConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var c = (RatScaleConfig)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"체감 환산 (사람 기준, 배율 x{c.Magnification:F1})\n" +
                $"• 눈높이 {c.targetEyeHeight * 100f:F1} cm → 사람 눈 {c.ToPerceived(c.targetEyeHeight):F2} m 높이처럼 보이는 크기감\n" +
                $"• 의자 좌판 45 cm → {c.ToPerceived(0.45f):F1} m,  사람 키 1.75 m → {c.ToPerceived(1.75f):F1} m\n" +
                $"• 몸 반지름 {c.bodyRadius * 100f:F1} cm / 높이 {c.bodyHeight * 100f:F1} cm\n" +
                $"• 걷기 {c.walkSpeed:F2} m/s → 체감 {c.ToPerceived(c.walkSpeed):F1} m/s,  질주 {c.dashSpeed:F2} m/s → 체감 {c.ToPerceived(c.dashSpeed):F1} m/s\n" +
                "값을 바꾸면 Play 시작 시 RatScaleRig가 자동 적용합니다.",
                MessageType.Info);

            if (c.ToPerceived(c.dashSpeed) > 6f)
                EditorGUILayout.HelpBox("질주 체감 속도가 6 m/s를 넘습니다. VR 멀미 위험이 큽니다.", MessageType.Warning);
        }
    }
}
