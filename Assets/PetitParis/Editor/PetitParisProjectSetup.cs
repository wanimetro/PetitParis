using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// Quest 3(Android) 빌드에 필요한 Player Settings를 한 번에 적용한다.
    /// XR Plug-in Management / OpenXR 설정은 여기서 바꾸지 않는다 (Project Settings에서 직접 체크).
    /// </summary>
    public static class PetitParisProjectSetup
    {
        public const string ApplicationId = "com.DefaultCompany.PetitParis";
        public const string ProductName = "PETIT PARIS";
        public const int MinAndroidApi = 32;

        [MenuItem("PetitParis/0. Apply Quest Android Player Settings", priority = 0)]
        public static void Apply()
        {
            string message =
                "다음 Player Settings를 바꿉니다 (프로젝트 전체 설정):\n\n" +
                $"• Product Name: {PlayerSettings.productName} → {ProductName}\n" +
                $"• Android Package Name: {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)} → {ApplicationId}\n" +
                $"• Android Minimum API Level → {MinAndroidApi}\n" +
                "• Android Scripting Backend → IL2CPP, Target Architecture → ARM64\n" +
                "• Android Graphics API → Vulkan만 사용 (Auto Graphics API 끔)\n" +
                "• Color Space → Linear\n\n" +
                "Windows(Standalone) 빌드 설정은 Product Name 외에는 바뀌지 않습니다.";

            if (!EditorUtility.DisplayDialog("PETIT PARIS — Quest 설정", message, "적용", "취소")) return;

            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApi;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();

            Debug.Log("[PetitParis] Quest Android Player Settings를 적용했습니다. " +
                      "이어서 XR Plug-in Management > Project Validation을 확인하세요.");
        }

        public const string MobileUrpAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

        /// <summary>
        /// (선택) Android 품질 단계가 쓰는 Mobile_RPAsset을 Quest에 맞게 조정한다.
        /// 프로젝트 전체(Android) 설정이므로 확인 창에서 동의한 경우에만 바꾼다.
        /// 필드 이름은 레포의 Mobile_RPAsset.asset 직렬화 이름과 동일.
        /// </summary>
        [MenuItem("PetitParis/5. (선택) Quest URP 최적화 적용", priority = 20)]
        public static void ApplyQuestUrp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MobileUrpAssetPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("PETIT PARIS", "Mobile_RPAsset을 찾을 수 없습니다: " + MobileUrpAssetPath, "확인");
                return;
            }
            var so = new SerializedObject(asset);
            var hdr = so.FindProperty("m_SupportsHDR");
            var msaa = so.FindProperty("m_MSAA");
            var scale = so.FindProperty("m_RenderScale");
            var shadowDistance = so.FindProperty("m_ShadowDistance");
            string Current(SerializedProperty p) => p == null ? "?" :
                p.propertyType == SerializedPropertyType.Float ? p.floatValue.ToString("0.##") :
                p.propertyType == SerializedPropertyType.Boolean ? (p.boolValue ? "켬" : "끔") : p.intValue.ToString();

            string message =
                "Android(Quest)용 URP 설정을 바꿉니다. PC(에디터·Windows) 설정에는 영향이 없습니다.\n\n" +
                $"• HDR: {Current(hdr)} → 끔 (모바일 VR 대역폭 절약)\n" +
                $"• MSAA: {Current(msaa)}x → 4x (VR 계단 현상 감소)\n" +
                $"• Render Scale: {Current(scale)} → 1.0 (Quest 3 선명도)\n" +
                $"• Shadow Distance: {Current(shadowDistance)} m → {PetitParisParisKit.DuskShadowDistance} m (작은 쥐 시점에서 그림자 해상도 확보)\n\n" +
                "되돌리려면 Assets/Settings/Mobile_RPAsset의 Inspector에서 값을 다시 바꾸면 됩니다.";
            if (!EditorUtility.DisplayDialog("PETIT PARIS — Quest URP 최적화", message, "적용", "취소")) return;

            if (hdr != null) hdr.boolValue = false;
            if (msaa != null) msaa.intValue = 4;
            if (scale != null) scale.floatValue = 1f;
            if (shadowDistance != null) shadowDistance.floatValue = PetitParisParisKit.DuskShadowDistance;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[PetitParis] Mobile_RPAsset에 Quest 최적화를 적용했습니다.");
        }
    }
}
