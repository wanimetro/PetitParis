using UnityEditor;

namespace PetitParis.EditorTools
{
    /// <summary>PETIT PARIS 에셋 경로 모음.</summary>
    public static class PetitParisPaths
    {
        public const string Root = "Assets/PetitParis";

        /// <summary>실제 게임 씬 (빌드 목록 맨 앞).</summary>
        public const string Scene = Root + "/Scenes/PetitParisVR.unity";

        /// <summary>조작·크기 테스트용 샌드박스 씬.</summary>
        public const string SandboxScene = Root + "/Scenes/PetitParisSandbox.unity";

        public const string Config = Root + "/Settings/RatScaleConfig.asset";
        public const string Comfort = Root + "/Settings/RatComfortSettings.asset";
        public const string InputActions = Root + "/Settings/PetitParisInput.inputactions";
        public const string RigPrefab = Root + "/Prefabs/XR Origin (Rat).prefab";
        public const string Materials = Root + "/Materials";
        public const string KoreanFontFile = Root + "/UI/Fonts/NanumGothicBold.ttf";
        public const string KoreanFontAsset = Root + "/UI/Fonts/NanumGothicBold SDF.asset";
        public const string FadeShader = "PetitParis/ScreenFade";
        public const string BlobShader = "PetitParis/BlobShadow";

        /// <summary>XRI Starter Assets 샘플의 Comfort Vignette 프리팹 (이미 프로젝트에 임포트되어 있음).</summary>
        public const string VignettePrefab =
            "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";

        static readonly string[] SubFolders =
            { "Scenes", "Scripts", "Prefabs", "Materials", "Models", "Audio", "Settings", "UI" };

        public static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "PetitParis");
            foreach (var folder in SubFolders)
                if (!AssetDatabase.IsValidFolder($"{Root}/{folder}")) AssetDatabase.CreateFolder(Root, folder);
        }
    }
}
