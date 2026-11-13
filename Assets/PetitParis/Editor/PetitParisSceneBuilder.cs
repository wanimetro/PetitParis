using UnityEditor;
using UnityEngine;
using static PetitParis.EditorTools.PetitParisBuildKit;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 조작·크기 테스트용 샌드박스 씬 (PetitParisSandbox.unity).
    /// 1단계의 크기 비교 오브젝트 + 이동 테스트 장치(벽, 좁은 틈, 점프 턱, 의자 아래 안전 구역) + 보행자 1명.
    /// 게임 흐름(안내·완료)은 없다. 실제 게임은 메뉴 "3. Build Game Scene".
    /// </summary>
    public static class PetitParisSceneBuilder
    {
        static readonly Vector3 Spawn = Vector3.zero;

        [MenuItem("PetitParis/1. Build Sandbox Scene (조작 테스트)", priority = 1)]
        public static void BuildSandbox()
        {
            if (!PrepareNewScene(PetitParisPaths.SandboxScene, out var scene)) return;
            var m = new MaterialLibrary();
            var config = GetOrCreateAsset<RatScaleConfig>(PetitParisPaths.Config);
            var font = KoreanFont();
            var dark = new Color(0.12f, 0.12f, 0.14f);

            SetupLighting(true);
            var env = new GameObject("Environment_Sandbox").transform;

            // 바닥·보도(12cm 턱 = 질주 충돌 테스트 벽)·건물 벽
            var street = Group("Street", env);
            var wall = m.Lit("PP_Wall", new Color(0.86f, 0.8f, 0.68f));
            Box("Ground", street, new Vector3(0f, -0.05f, 2f), new Vector3(12f, 0.1f, 12f), m.Lit("PP_Ground", new Color(0.45f, 0.43f, 0.4f)));
            Box("Sidewalk_12cm", street, new Vector3(0f, 0.06f, 5.5f), new Vector3(12f, 0.12f, 3f), m.Lit("PP_Sidewalk", new Color(0.64f, 0.62f, 0.58f)));
            Box("BuildingWall", street, new Vector3(0f, 1.5f, 7.15f), new Vector3(12f, 3f, 0.3f), wall);
            Box("SideWall_Left", street, new Vector3(-3.15f, 1.5f, 1.5f), new Vector3(0.3f, 3f, 7f), wall);
            Label("Label_DashWall", street, new Vector3(0f, 0.2f, 3.95f), "Dash test: 12 cm curb", 0.12f, 0.8f, dark, null, Spawn);

            // 카페 (의자 아래 안전 구역)
            var cafe = Group("Cafe", env);
            CafeChair("CafeChair_A", cafe, new Vector3(0.6f, 0f, 1.2f), 0f, m, true);
            CafeTable("CafeTable", cafe, new Vector3(1.5f, 0f, 1.75f), m);
            CafeChair("CafeChair_B", cafe, new Vector3(1.5f, 0f, 2.4f), 180f, m, true);
            Label("Label_Chair", cafe, new Vector3(0.6f, 0.98f, 1.2f), PetitParisText.HintHide, 0.22f, 0.9f, dark, font, Spawn);

            // 크기 기준
            var refs = Group("ScaleReferences", env);
            var orange = m.Lit("PP_SizeCube", new Color(0.92f, 0.52f, 0.2f));
            Box("Cube_10cm", refs, new Vector3(0.35f, 0.05f, 0.45f), Vector3.one * 0.1f, orange);
            Label("Label_10cm", refs, new Vector3(0.35f, 0.14f, 0.45f), "10 cm", 0.25f, 0.4f, dark, null, Spawn);
            Box("Cube_1m", refs, new Vector3(2.7f, 0.5f, 2.8f), Vector3.one, orange);
            Label("Label_1m", refs, new Vector3(2.7f, 1.1f, 2.8f), "1 m", 0.5f, 0.8f, dark, null, Spawn);
            HeightRuler(refs, new Vector3(-0.35f, 0f, 0.6f), config.targetEyeHeight, m, dark);

            // 이동 테스트 장치
            var tests = Group("LocomotionTests", env);
            var testMat = m.Lit("PP_TestBlock", new Color(0.4f, 0.55f, 0.75f));
            // 좁은 틈 6cm: 서 있으면(지름 약 7.7cm) 못 지나가고, 웅크리면(약 5.6cm) 지나간다
            Box("Gap_Left", tests, new Vector3(-1.38f, 0.15f, 1.0f), new Vector3(0.7f, 0.3f, 0.3f), testMat);   // x -1.73 ~ -1.03
            Box("Gap_Right", tests, new Vector3(-0.68f, 0.15f, 1.0f), new Vector3(0.58f, 0.3f, 0.3f), testMat); // x -0.97 ~ -0.39
            Label("Label_Gap", tests, new Vector3(-1.0f, 0.36f, 0.84f), "6 cm gap: B (crouch) to pass", 0.1f, 0.8f, dark, null, Spawn);
            // 점프 턱 3cm (stepOffset 2cm보다 높음 → 점프해야 올라감)
            Box("JumpStep_3cm", tests, new Vector3(0.9f, 0.015f, 0.35f), new Vector3(0.4f, 0.03f, 0.4f), testMat);
            Label("Label_Jump", tests, new Vector3(0.9f, 0.12f, 0.35f), "3 cm: A (jump)", 0.1f, 0.5f, dark, null, Spawn);
            // 낮은 턱 1.5cm (걸어서 넘음)
            Box("Step_1_5cm", tests, new Vector3(-0.2f, 0.0075f, -0.6f), new Vector3(0.6f, 0.015f, 0.3f), testMat);

            MarkStatic(env);

            // 보행자 1명 (발걸음 테스트) — 왼쪽 벽과 의자 사이를 왕복
            var npcs = Group("Pedestrians", null);
            Pedestrian("Pedestrian_Test", npcs, new[] { new Vector3(-2.4f, 0f, 3.3f), new Vector3(2.0f, 0f, 3.3f) },
                PedestrianPath.LoopMode.PingPong, 0.9f, 2f, new Color(0.35f, 0.45f, 0.3f), m, PedestrianStyle.Chef);

            // 체크포인트 (시작 지점 하나)
            var gameplay = Group("Gameplay", null);
            var cps = Group("Checkpoints", gameplay);
            var flag = Checkpoint("CP0_Start", cps, Spawn, 0f, m);
            var manager = gameplay.gameObject.AddComponent<CheckpointManager>();
            var chime = gameplay.gameObject.AddComponent<AudioSource>();
            var mso = new SerializedObject(manager);
            SetObjectArray(mso, "checkpoints", new Object[] { cps.GetChild(0) });
            SetObjectArray(mso, "markers", new Object[] { flag });
            SetObject(mso, "chimeSource", chime);
            mso.ApplyModifiedPropertiesWithoutUndo();

            var rig = BuildRig(Spawn, Quaternion.identity, true, m);
            if (rig == null) return;
            Wire(rig.respawn, ("checkpoints", manager));
            Wire(manager, ("rig", rig.scaleRig));
            Wire(rig.statusPanel, ("checkpoints", manager));

            if (!SaveScene(scene, PetitParisPaths.SandboxScene, false)) return;
            Selection.activeGameObject = rig.root;
            Debug.Log($"[PetitParis] 샌드박스 씬을 만들었습니다: {PetitParisPaths.SandboxScene}. 다음: PetitParis > 4. Validate Scene");
        }

        static void HeightRuler(Transform parent, Vector3 position, float eyeHeight, MaterialLibrary m, Color textColor)
        {
            var ruler = Group("HeightRuler_2m", parent);
            ruler.localPosition = position;
            var dark = m.Unlit("PP_RulerTick", new Color(0.1f, 0.1f, 0.1f));
            Box("Pole", ruler, new Vector3(0f, 1f, 0f), new Vector3(0.012f, 2f, 0.012f), m.Lit("PP_RulerWhite", new Color(0.95f, 0.95f, 0.95f)));
            for (int cm = 10; cm <= 200; cm += 10)
            {
                bool major = cm % 50 == 0 || cm <= 20;
                float length = major ? 0.08f : 0.04f;
                RemoveCollider(Box($"Tick_{cm}cm", ruler, new Vector3(0.006f + length * 0.5f, cm / 100f, 0f), new Vector3(length, 0.003f, 0.003f), dark));
                if (major) Label($"Label_{cm}cm", ruler, new Vector3(0.16f, cm / 100f, 0f), $"{cm} cm", 0.12f, 0.2f, textColor, null, Spawn);
            }
            RemoveCollider(Box("EyeLevelMark", ruler, new Vector3(0.056f, eyeHeight, 0f), new Vector3(0.1f, 0.005f, 0.005f), m.Unlit("PP_RulerEye", new Color(0.9f, 0.1f, 0.1f))));
            Label("Label_Eye", ruler, new Vector3(0.18f, eyeHeight + 0.015f, 0f), $"eye {eyeHeight * 100f:F0} cm", 0.12f, 0.25f, new Color(0.85f, 0.1f, 0.1f), null, Spawn);
        }
    }
}
