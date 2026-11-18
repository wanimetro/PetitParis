using UnityEditor;
using UnityEngine;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// Assets/PetitParis/ThirdParty/ 아래 외부 에셋의 가져오기 규칙 (Quest 3 성능).
    /// - 질감: 최대 1024px, 노멀 맵은 NormalMap 타입
    /// - 모델: 카메라·조명 가져오지 않음. Quaternius 캐릭터·애니메이션은 Humanoid (애니메이션 리타깃)
    /// 처음 가져올 때만 적용된다. 이미 가져온 파일은 메뉴 PetitParis > 5. Reimport Third Party로 다시 적용한다.
    /// </summary>
    public class PetitParisThirdPartyImport : AssetPostprocessor
    {
        public const string Root = "Assets/PetitParis/ThirdParty/";
        public const int MaxTextureSize = 1024;

        static bool IsThirdParty(string path) => path.Replace('\\', '/').StartsWith(Root);

        public static bool IsNormalMapName(string path)
        {
            string n = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return n.Contains("_nor") || n.EndsWith("normal") || n.Contains("_normal");
        }

        public static bool IsHumanoidModel(string path)
        {
            path = path.Replace('\\', '/');
            return path.Contains("/Quaternius/UniversalBaseCharacters/Superhero_")
                || path.Contains("/Quaternius/UniversalAnimationLibrary/");
        }

        void OnPreprocessTexture()
        {
            if (!IsThirdParty(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = Mathf.Min(ti.maxTextureSize, MaxTextureSize);
            if (IsNormalMapName(assetPath)) ti.textureType = TextureImporterType.NormalMap;
            ti.mipmapEnabled = true;
        }

        void OnPreprocessModel()
        {
            if (!IsThirdParty(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            if (IsHumanoidModel(assetPath))
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
            else
            {
                mi.importAnimation = false;
            }
        }

        [MenuItem("PetitParis/5. Reimport Third Party (1K textures)", priority = 5)]
        public static void ReimportAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D t:Model", new[] { Root.TrimEnd('/') }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Debug.Log("[PetitParis] ThirdParty 에셋을 다시 가져왔습니다 (질감 최대 " + MaxTextureSize + "px).");
        }
    }
}
