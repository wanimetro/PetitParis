using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 샌드박스 씬과 게임 씬 빌더가 함께 쓰는 생성 도구.
    /// - XR 리그 (쥐 시점 + 이동 + 회전 + 비네트 + 위험 판정 + 리스폰)
    /// - 기본 도형, 머티리얼, 라벨, 한글 폰트
    /// - 의자·테이블·벤치(안전 구역 포함), 보행자 NPC, 자전거
    /// 모든 크기는 월드 미터(실제 크기).
    /// </summary>
    public static class PetitParisBuildKit
    {
        public const int PlayerLayer = 2; // Ignore Raycast: 플레이어 자신을 물리 검사에서 제외
        public static readonly Color SkyColor = new Color(0.66f, 0.76f, 0.86f);

        // ───────────────────────── 씬 ─────────────────────────

        /// <summary>
        /// 자동화(Unity MCP 등)용. true면 덮어쓰기 확인 대화상자를 띄우지 않고 "예"로 처리하며, 오류는 Console에만 남긴다.
        /// 모달 대화상자가 뜨면 에디터가 멈춰 MCP 호출이 시간 초과되기 때문이다. 메뉴에서 직접 실행할 때는 false 그대로 둔다.
        /// 사용: PetitParisBuildKit.AssumeYes = true; try { PetitParisLevelBuilder.BuildGame(); } finally { AssumeYes = false; }
        /// </summary>
        public static bool AssumeYes;

        /// <summary>새 씬을 만들 준비. 취소하면 false. 기존 씬은 건드리지 않는다.</summary>
        public static bool PrepareNewScene(string scenePath, out Scene scene)
        {
            scene = default;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Fail("Play 모드를 끈 뒤 다시 실행하세요.");
                return false;
            }
            if (AssumeYes)
            {
                if (SceneManager.GetActiveScene().isDirty)
                {
                    Fail("저장되지 않은 씬 변경이 있어 자동 생성을 중단했습니다. 저장하거나 버린 뒤 다시 실행하세요.");
                    return false;
                }
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (File.Exists(scenePath) && !AssumeYes &&
                !EditorUtility.DisplayDialog("PETIT PARIS",
                    $"{scenePath} 이(가) 이미 있습니다.\n새로 만들어 덮어쓸까요? (다른 씬은 건드리지 않습니다)", "덮어쓰기", "취소"))
                return false;

            PetitParisPaths.EnsureFolders();
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return true;
        }

        public static bool SaveScene(Scene scene, string path, bool firstInBuild)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                Fail("씬 저장에 실패했습니다: " + path);
                return false;
            }
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            if (firstInBuild) scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            else scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            return true;
        }

        public static void SetupLighting(bool shadows)
        {
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.15f;
            light.shadows = shadows ? LightShadows.Hard : LightShadows.None;
            lightGO.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.sun = light;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.71f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.5f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.28f);
        }

        // ───────────────────────── 리그 ─────────────────────────

        public class RigParts
        {
            public GameObject root;
            public Camera camera;
            public RatScaleRig scaleRig;
            public RatInputReader reader;
            public RatEyeCalibrator calibrator;
            public ScreenFader fader;
            public RatLocomotionController locomotion;
            public RatDashController dash;
            public RatJumpController jump;
            public RatCrouchController crouch;
            public RatComfortApplier comfort;
            public SnapTurnProvider snapTurn;
            public RatDangerDetector danger;
            public RatRespawn respawn;
            public RatStatusPanel statusPanel;
            public RatDebugPanel debugPanel;
        }

        /// <summary>
        /// 쥐 XR 리그를 만들고 프리팹으로 저장한다.
        /// 이동 처리 경로는 하나: RatLocomotionController → LocomotionMediator → XRBodyTransformer → CharacterController.
        /// </summary>
        public static RigParts BuildRig(Vector3 position, Quaternion rotation, bool showDebugPanel, MaterialLibrary mats)
        {
            var config = GetOrCreateAsset<RatScaleConfig>(PetitParisPaths.Config);
            var comfortSettings = GetOrCreateAsset<RatComfortSettings>(PetitParisPaths.Comfort);
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(PetitParisPaths.InputActions);
            if (inputAsset == null) { Fail("입력 에셋이 없습니다: " + PetitParisPaths.InputActions); return null; }
            var refs = LoadActionReferences(PetitParisPaths.InputActions);
            var missing = RatInputReader.RequiredActionNames.Where(n => !refs.ContainsKey(n)).ToList();
            if (missing.Count > 0) { Fail("입력 에셋에 없는 액션:\n" + string.Join("\n", missing)); return null; }
            var fadeShader = Shader.Find(PetitParisPaths.FadeShader);
            if (fadeShader == null) { Fail($"셰이더 '{PetitParisPaths.FadeShader}'를 찾을 수 없습니다."); return null; }

            var parts = new RigParts();
            var root = new GameObject("XR Origin (Rat)") { layer = PlayerLayer };
            parts.root = root;
            root.transform.SetPositionAndRotation(position, rotation);

            var origin = root.AddComponent<XROrigin>();
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject); // 자동 생성 자식 제거

            var offset = new GameObject("Camera Offset").transform;
            offset.SetParent(root.transform, false);
            var space = new GameObject("Rat Tracking Space").transform;
            space.SetParent(offset, false);
            space.localPosition = new Vector3(0f, config.targetEyeHeight, 0f);
            space.localScale = Vector3.one * config.trackingScale;

            // 카메라
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            camGO.transform.SetParent(space, false);
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.nearClipPlane = config.nearClip;
            cam.farClipPlane = config.farClip;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            camGO.AddComponent<AudioListener>();
            ConfigurePose(camGO.AddComponent<TrackedPoseDriver>(), refs, "Head");
            parts.camera = cam;

            var fadeGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fadeGO.name = "Screen Fader";
            RemoveCollider(fadeGO);
            fadeGO.transform.SetParent(camGO.transform, false);
            var fadeRenderer = fadeGO.GetComponent<MeshRenderer>();
            fadeRenderer.sharedMaterial = mats.Custom("PP_ScreenFade", fadeShader, Color.black);
            fadeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            fadeRenderer.receiveShadows = false;
            fadeRenderer.enabled = false;
            parts.fader = fadeGO.AddComponent<ScreenFader>();
            parts.fader.PlaceInFrontOfCamera();

            var vignette = AddComfortVignette(camGO.transform);

            // 컨트롤러
            var pawMat = mats.Lit("PP_Paw", new Color(0.95f, 0.72f, 0.76f));
            var left = BuildController("Left Controller", space, refs, "Left", pawMat, new Vector3(-0.18f, -0.35f, 0.3f));
            var right = BuildController("Right Controller", space, refs, "Right", pawMat, new Vector3(0.18f, -0.35f, 0.3f));

            var font = KoreanFont();
            var (statusPanel, statusText) = BuildWristPanel<RatStatusPanel>("Wrist Status Panel", left.transform, mats, font, 0.11f, new Vector2(0.24f, 0.11f));
            parts.statusPanel = statusPanel;
            var (debugPanel, debugText) = BuildWristPanel<RatDebugPanel>("Wrist Debug Panel", right.transform, mats, null, 0.08f, new Vector2(0.24f, 0.17f));
            parts.debugPanel = debugPanel;

            // 루트 컴포넌트
            var body = root.AddComponent<CharacterController>();
            parts.scaleRig = root.AddComponent<RatScaleRig>();
            parts.reader = root.AddComponent<RatInputReader>();
            parts.calibrator = root.AddComponent<RatEyeCalibrator>();
            root.AddComponent<XROriginGuard>();
            var desktopLook = root.AddComponent<RatDesktopLook>(); // 헤드셋 추적이 없을 때만 마우스 시점 전환
            parts.danger = root.AddComponent<RatDangerDetector>();
            parts.respawn = root.AddComponent<RatRespawn>();
            var sfx = root.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            var so = new SerializedObject(origin);
            SetObject(so, "m_Camera", cam);
            SetObject(so, "m_OriginBaseGameObject", root);
            SetObject(so, "m_CameraFloorOffsetObject", offset.gameObject);
            SetInt(so, "m_RequestedTrackingOriginMode", (int)XROrigin.TrackingOriginMode.Floor);
            SetFloat(so, "m_CameraYOffset", 0f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // XRI 이동 구조: Mediator + BodyTransformer (CharacterController 사용)
            var locoGO = new GameObject("Locomotion");
            locoGO.transform.SetParent(root.transform, false);
            var transformer = locoGO.AddComponent<XRBodyTransformer>();
            var mediator = locoGO.GetComponent<LocomotionMediator>();
            if (mediator == null) mediator = locoGO.AddComponent<LocomotionMediator>();
            Wire(transformer, ("m_XROrigin", origin));
            SetBool(transformer, "m_UseCharacterControllerIfExists", true);

            // 회전: XRI SnapTurnProvider (오른쪽 스틱만)
            var turnGO = new GameObject("Turn");
            turnGO.transform.SetParent(locoGO.transform, false);
            var snap = turnGO.AddComponent<SnapTurnProvider>();
            ConfigureSnapTurn(snap, mediator, refs[RatInputReader.TurnAction], comfortSettings);
            parts.snapTurn = snap;

            // 이동: 쥐 전용 Provider 하나 + 상태 컨트롤러들
            var moveGO = new GameObject("Rat Movement");
            moveGO.transform.SetParent(locoGO.transform, false);
            parts.locomotion = moveGO.AddComponent<RatLocomotionController>();
            parts.dash = moveGO.AddComponent<RatDashController>();
            parts.jump = moveGO.AddComponent<RatJumpController>();
            parts.crouch = moveGO.AddComponent<RatCrouchController>();

            var comfortGO = new GameObject("Comfort");
            comfortGO.transform.SetParent(locoGO.transform, false);
            parts.comfort = comfortGO.AddComponent<RatComfortApplier>();

            Wire(parts.scaleRig, ("config", config), ("xrOrigin", origin), ("trackingSpace", space), ("body", body), ("eyeCamera", cam));
            parts.scaleRig.ApplyScaleSettings();
            Wire(parts.reader, ("actions", inputAsset));
            Wire(desktopLook, ("rig", parts.scaleRig), ("input", parts.reader), ("calibrator", parts.calibrator));
            Wire(parts.calibrator, ("rig", parts.scaleRig), ("input", parts.reader), ("fader", parts.fader));
            Wire(parts.locomotion, ("m_Mediator", mediator), ("rig", parts.scaleRig), ("input", parts.reader), ("comfort", comfortSettings),
                ("dash", parts.dash), ("jump", parts.jump), ("crouch", parts.crouch));
            Wire(parts.dash, ("rig", parts.scaleRig), ("input", parts.reader), ("comfort", comfortSettings));
            Wire(parts.jump, ("input", parts.reader), ("comfort", comfortSettings));
            Wire(parts.crouch, ("rig", parts.scaleRig), ("input", parts.reader));
            Wire(parts.comfort, ("m_Mediator", mediator), ("comfort", comfortSettings), ("snapTurn", snap),
                ("locomotion", parts.locomotion), ("dash", parts.dash));
            Wire(parts.danger, ("rig", parts.scaleRig), ("crouch", parts.crouch), ("respawn", parts.respawn));
            Wire(parts.respawn, ("rig", parts.scaleRig), ("locomotion", parts.locomotion), ("dash", parts.dash), ("jump", parts.jump),
                ("crouch", parts.crouch), ("calibrator", parts.calibrator), ("fader", parts.fader), ("danger", parts.danger), ("sfx", sfx));
            Wire(statusPanel, ("text", statusText), ("danger", parts.danger), ("dash", parts.dash), ("crouch", parts.crouch), ("respawn", parts.respawn));
            Wire(debugPanel, ("text", debugText), ("input", parts.reader), ("calibrator", parts.calibrator), ("rig", parts.scaleRig),
                ("locomotion", parts.locomotion), ("dash", parts.dash), ("crouch", parts.crouch));
            SetBool(debugPanel, "showPanel", showDebugPanel);
            debugText.enabled = showDebugPanel;
            debugPanel.transform.parent.Find("Background").gameObject.SetActive(showDebugPanel);

            if (vignette != null) ConnectVignette(vignette, parts.comfort);

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PetitParisPaths.RigPrefab, InteractionMode.AutomatedAction);
            return parts;
        }

        static GameObject BuildController(string name, Transform space, Dictionary<string, InputActionReference> refs,
            string prefix, Material pawMaterial, Vector3 restPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(space, false);
            go.transform.localPosition = restPosition;
            ConfigurePose(go.AddComponent<TrackedPoseDriver>(), refs, prefix);

            BuildRatPaw(go.transform, pawMaterial, prefix == "Left");
            return go;
        }

        /// <summary>
        /// 쥐 앞발 (자체 제작, 기본 도형). 추적 공간 단위 = 실제 손 크기라 사람 손처럼 보인다.
        /// 분홍 손바닥 + 손가락 4개 + 작은 발톱 + 회색 털 손목. 충돌체 없음.
        /// </summary>
        static void BuildRatPaw(Transform controller, Material skin, bool isLeft)
        {
            var paw = new GameObject("Paw Visual").transform;
            paw.SetParent(controller, false);
            paw.localPosition = new Vector3(0f, -0.01f, 0.02f);
            var lib = new MaterialLibrary();
            var fur = lib.Lit("PP_RatFur", new Color(0.46f, 0.43f, 0.47f));
            var claw = lib.Lit("PP_RatClaw", new Color(0.95f, 0.9f, 0.82f));

            var wrist = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            wrist.name = "FurWrist";
            wrist.transform.SetParent(paw, false);
            wrist.transform.localPosition = new Vector3(0f, 0f, -0.07f);
            wrist.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wrist.transform.localScale = new Vector3(0.05f, 0.06f, 0.045f);
            wrist.GetComponent<MeshRenderer>().sharedMaterial = fur;

            var palm = Sphere("Palm", paw, new Vector3(0f, 0f, 0f), 1f, skin);
            RemoveCollider(palm);
            palm.transform.localScale = new Vector3(0.055f, 0.028f, 0.06f);

            float side = isLeft ? 1f : -1f;
            for (int i = 0; i < 4; i++)
            {
                float x = (-0.018f + i * 0.012f);
                float splay = (i - 1.5f) * 9f;
                var finger = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                finger.name = $"Finger_{i}";
                finger.transform.SetParent(paw, false);
                finger.transform.localPosition = new Vector3(x, -0.004f, 0.038f);
                finger.transform.localRotation = Quaternion.Euler(100f, splay, 0f);
                finger.transform.localScale = new Vector3(0.011f, 0.017f, 0.011f);
                finger.GetComponent<MeshRenderer>().sharedMaterial = skin;

                var tip = Sphere($"Claw_{i}", paw, Vector3.zero, 1f, claw);
                RemoveCollider(tip);
                tip.transform.localPosition = finger.transform.localPosition + finger.transform.localRotation * new Vector3(0f, 0.018f, 0f);
                tip.transform.localScale = new Vector3(0.006f, 0.004f, 0.008f);
            }
            // 엄지 (안쪽)
            var thumb = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            thumb.name = "Thumb";
            thumb.transform.SetParent(paw, false);
            thumb.transform.localPosition = new Vector3(0.03f * side, 0f, 0.012f);
            thumb.transform.localRotation = Quaternion.Euler(90f, 40f * side, 0f);
            thumb.transform.localScale = new Vector3(0.011f, 0.012f, 0.011f);
            thumb.GetComponent<MeshRenderer>().sharedMaterial = skin;
        }

        /// <summary>손목 패널 (컨트롤러 위, 사용자 쪽으로 45도). 추적 공간 단위 = 실제 손 크기.</summary>
        static (T, TextMeshPro) BuildWristPanel<T>(string name, Transform controller, MaterialLibrary mats, TMP_FontAsset font,
            float fontSize, Vector2 size) where T : Component
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(controller, false);
            holder.localPosition = new Vector3(0f, 0.09f, -0.03f);
            holder.localRotation = Quaternion.Euler(45f, 0f, 0f);

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            RemoveCollider(bg);
            bg.transform.SetParent(holder, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            bg.transform.localScale = new Vector3(size.x + 0.01f, size.y + 0.01f, 1f);
            var r = bg.GetComponent<MeshRenderer>();
            r.sharedMaterial = mats.Unlit("PP_PanelBackground", new Color(0.08f, 0.08f, 0.1f));
            r.shadowCastingMode = ShadowCastingMode.Off;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(holder, false);
            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = "PETIT PARIS";
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.rectTransform.sizeDelta = size;
            var comp = textGO.AddComponent<T>();
            return (comp, tmp);
        }

        static void ConfigurePose(TrackedPoseDriver driver, Dictionary<string, InputActionReference> refs, string prefix)
        {
            var so = new SerializedObject(driver);
            SetInt(so, "m_TrackingType", 0);       // Rotation And Position
            SetInt(so, "m_UpdateType", 0);         // Update And Before Render
            SetBool(so, "m_IgnoreTrackingState", false);
            SetActionProperty(so, "m_PositionInput", refs[$"Tracking/{prefix}Position"]);
            SetActionProperty(so, "m_RotationInput", refs[$"Tracking/{prefix}Rotation"]);
            SetActionProperty(so, "m_TrackingStateInput", refs[$"Tracking/{prefix}TrackingState"]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>SnapTurnProvider: 오른쪽 스틱만, 30도, 뒤로 돌기 끔. 필드 이름은 XRI 3.3.2 샘플 프리팹과 동일.</summary>
        static void ConfigureSnapTurn(SnapTurnProvider snap, LocomotionMediator mediator, InputActionReference turn, RatComfortSettings comfort)
        {
            var so = new SerializedObject(snap);
            SetObject(so, "m_Mediator", mediator);
            SetFloat(so, "m_TurnAmount", comfort.snapTurnDegrees);
            SetFloat(so, "m_DebounceTime", comfort.snapTurnDebounce);
            SetBool(so, "m_EnableTurnLeftRight", true);
            SetBool(so, "m_EnableTurnAround", false);
            SetFloat(so, "m_DelayTime", 0f);
            var leftMode = so.FindProperty("m_LeftHandTurnInput.m_InputSourceMode");
            if (leftMode != null) leftMode.intValue = 0; // Unused: 왼쪽 스틱은 이동 전용
            else Debug.LogError("[PetitParis] SnapTurnProvider.m_LeftHandTurnInput 구조를 읽지 못했습니다.");
            var rightMode = so.FindProperty("m_RightHandTurnInput.m_InputSourceMode");
            var rightRef = so.FindProperty("m_RightHandTurnInput.m_InputActionReference");
            if (rightMode != null && rightRef != null)
            {
                rightMode.intValue = 2; // InputActionReference
                rightRef.objectReferenceValue = turn;
            }
            else Debug.LogError("[PetitParis] SnapTurnProvider.m_RightHandTurnInput 구조를 읽지 못했습니다.");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>XRI 샘플의 TunnelingVignette 프리팹을 카메라 아래에 둔다. 없으면 비네트 없이 진행.</summary>
        static MonoBehaviour AddComfortVignette(Transform camera)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PetitParisPaths.VignettePrefab);
            if (prefab == null)
            {
                Debug.LogWarning("[PetitParis] TunnelingVignette 프리팹이 없어 Comfort Vignette 없이 진행합니다: " + PetitParisPaths.VignettePrefab);
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, camera);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            foreach (var mb in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Name == "TunnelingVignetteController") return mb;
            Debug.LogWarning("[PetitParis] TunnelingVignetteController를 찾지 못했습니다.");
            return null;
        }

        static void ConnectVignette(MonoBehaviour controller, LocomotionProvider provider)
        {
            var so = new SerializedObject(controller);
            var list = so.FindProperty("m_LocomotionVignetteProviders");
            if (list == null || !list.isArray)
            {
                Debug.LogWarning("[PetitParis] 비네트 Provider 목록을 찾지 못했습니다. Inspector에서 Comfort(RatComfortApplier)를 직접 추가하세요.");
                return;
            }
            list.arraySize = 1;
            var element = list.GetArrayElementAtIndex(0);
            var loco = element.FindPropertyRelative("m_LocomotionProvider");
            var enabled = element.FindPropertyRelative("m_Enabled");
            if (loco == null)
            {
                Debug.LogWarning("[PetitParis] 비네트 Provider 항목 구조가 예상과 다릅니다. Inspector에서 Comfort를 직접 연결하세요.");
                return;
            }
            loco.objectReferenceValue = provider;
            if (enabled != null) enabled.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────────────────────── 카페·거리 소품 ─────────────────────────

        public static Transform CafeChair(string name, Transform parent, Vector3 position, float yaw, MaterialLibrary m, bool safeZone)
        {
            var chair = Group(name, parent);
            chair.localPosition = position;
            chair.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var frame = m.LitTex("PP_ChairFrame", PetitParisTextures.Wood(), new Color(0.75f, 0.6f, 0.55f));
            var seat = m.LitTex("PP_ChairSeat", PetitParisTextures.Rattan(), Color.white);

            Box("Seat", chair, new Vector3(0f, 0.435f, 0f), new Vector3(0.42f, 0.03f, 0.42f), seat);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.18f : 0.18f;
                float z = (i < 2) ? -0.18f : 0.18f;
                Box($"Leg_{i}", chair, new Vector3(x, 0.21f, z), new Vector3(0.03f, 0.42f, 0.03f), frame);
            }
            Box("BackPost_L", chair, new Vector3(-0.18f, 0.655f, 0.195f), new Vector3(0.03f, 0.41f, 0.03f), frame);
            Box("BackPost_R", chair, new Vector3(0.18f, 0.655f, 0.195f), new Vector3(0.03f, 0.41f, 0.03f), frame);
            Box("Backrest", chair, new Vector3(0f, 0.79f, 0.195f), new Vector3(0.42f, 0.13f, 0.025f), seat);

            if (safeZone) SafeZone("SafeZone_UnderChair", chair, new Vector3(0f, 0.2f, 0f), new Vector3(0.34f, 0.4f, 0.34f), "의자 아래", false, m);
            return chair;
        }

        public static Transform CafeTable(string name, Transform parent, Vector3 position, MaterialLibrary m)
        {
            var table = Group(name, parent);
            table.localPosition = position;
            var metal = m.Lit("PP_TableMetal", new Color(0.18f, 0.18f, 0.2f));
            Cylinder("Top", table, new Vector3(0f, 0.735f, 0f), 0.6f, 0.03f, m.LitTex("PP_TableMarble", PetitParisTextures.Marble(), Color.white));
            Cylinder("Pedestal", table, new Vector3(0f, 0.37f, 0f), 0.06f, 0.7f, metal);
            Cylinder("Base", table, new Vector3(0f, 0.01f, 0f), 0.42f, 0.02f, metal);
            return table;
        }

        /// <summary>테이블 1개 + 마주 보는 의자 2개 (의자 아래 안전 구역).</summary>
        public static void CafeSet(string name, Transform parent, Vector3 center, MaterialLibrary m)
        {
            var set = Group(name, parent);
            set.localPosition = center;
            CafeTable("Table", set, Vector3.zero, m);
            CafeChair("Chair_S", set, new Vector3(0f, 0f, -0.48f), 180f, m, true);
            CafeChair("Chair_N", set, new Vector3(0f, 0f, 0.48f), 0f, m, true);
        }

        public static Transform Bench(string name, Transform parent, Vector3 position, float yaw, float length, MaterialLibrary m, bool safeZone)
        {
            var bench = Group(name, parent);
            bench.localPosition = position;
            bench.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = m.LitTex("PP_BenchWood", PetitParisTextures.Wood(), Color.white);
            var iron = m.Lit("PP_BenchIron", new Color(0.12f, 0.16f, 0.14f));
            Box("Seat", bench, new Vector3(0f, 0.44f, 0f), new Vector3(length, 0.03f, 0.38f), wood);
            Box("Back", bench, new Vector3(0f, 0.7f, 0.18f), new Vector3(length, 0.22f, 0.03f), wood);
            Box("Side_L", bench, new Vector3(-length * 0.5f + 0.03f, 0.215f, 0f), new Vector3(0.04f, 0.43f, 0.34f), iron);
            Box("Side_R", bench, new Vector3(length * 0.5f - 0.03f, 0.215f, 0f), new Vector3(0.04f, 0.43f, 0.34f), iron);
            if (safeZone) SafeZone("SafeZone_UnderBench", bench, new Vector3(0f, 0.2f, 0f), new Vector3(length - 0.12f, 0.4f, 0.32f), "벤치 아래", false, m);
            return bench;
        }

        public static RatSafeZone SafeZone(string name, Transform parent, Vector3 center, Vector3 size, string zoneName, bool requireCrouch, MaterialLibrary m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            // 바닥 표시 (은은한 초록 원)
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = "Indicator";
            RemoveCollider(indicator);
            indicator.transform.SetParent(go.transform, false);
            indicator.transform.localPosition = new Vector3(0f, -center.y + 0.006f, 0f);
            indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = new Vector3(size.x, size.z, 1f);
            var r = indicator.GetComponent<MeshRenderer>();
            r.sharedMaterial = m.Blob("PP_SafeZoneIndicator", new Color(0.55f, 0.95f, 0.6f, 0.2f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            var zone = go.AddComponent<RatSafeZone>();
            var so = new SerializedObject(zone);
            SetString(so, "zoneName", zoneName);
            SetBool(so, "requireCrouch", requireCrouch);
            SetObject(so, "indicator", r);
            so.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }

        public static void Planter(string name, Transform parent, Vector3 position, MaterialLibrary m)
        {
            var p = Group(name, parent);
            p.localPosition = position;
            Cylinder("Pot", p, new Vector3(0f, 0.17f, 0f), 0.36f, 0.34f, m.Lit("PP_Terracotta", new Color(0.72f, 0.38f, 0.24f)));
            RemoveCollider(Sphere("Leaves", p, new Vector3(0f, 0.48f, 0f), 0.44f, m.Lit("PP_Leaves", new Color(0.28f, 0.5f, 0.24f))));
        }

        public static void PaperCup(string name, Transform parent, Vector3 position, MaterialLibrary m)
        {
            var c = Group(name, parent);
            c.localPosition = position;
            Cylinder("Cup", c, new Vector3(0f, 0.05f, 0f), 0.08f, 0.1f, m.Lit("PP_PaperCup", new Color(0.95f, 0.94f, 0.9f)));
            Cylinder("Sleeve", c, new Vector3(0f, 0.05f, 0f), 0.085f, 0.035f, m.Lit("PP_CupSleeve", new Color(0.55f, 0.36f, 0.2f)));
        }

        public static void Crate(string name, Transform parent, Vector3 position, Vector3 size, float yaw, MaterialLibrary m)
        {
            var go = Box(name, parent, position + Vector3.up * size.y * 0.5f, size, m.LitTex("PP_CrateWood", PetitParisTextures.Wood(), new Color(1f, 0.9f, 0.75f)));
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // ───────────────────────── 보행자·자전거 ─────────────────────────

        public enum PedestrianStyle { Casual, Chef, Waiter }

        /// <summary>기본 도형 보행자. 신발 2개에 FootstepHazard + 3D 발소리, 바닥 그림자.</summary>
        public static PedestrianPath Pedestrian(string name, Transform parent, Vector3[] waypoints, PedestrianPath.LoopMode mode,
            float speed, float startDelay, Color coatColor, MaterialLibrary m, PedestrianStyle style = PedestrianStyle.Casual)
        {
            var root = Group(name, parent);
            var wpRoot = Group("Waypoints", root);
            var wps = new Transform[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                wps[i] = Group($"W{i}", wpRoot);
                wps[i].position = waypoints[i];
            }

            var coat = m.Lit("PP_Coat_" + ColorUtility.ToHtmlStringRGB(coatColor), coatColor);
            var pants = m.Lit("PP_HumanPants", new Color(0.2f, 0.26f, 0.42f));
            var skin = m.Lit("PP_HumanSkin", new Color(0.86f, 0.68f, 0.56f));
            var shoeMat = m.Lit("PP_HumanShoe", new Color(0.12f, 0.09f, 0.07f));
            var blob = m.Blob("PP_BlobShadow", new Color(0f, 0f, 0f, 0.5f));
            var white = m.Lit("PP_ChefWhite", new Color(0.95f, 0.94f, 0.9f));
            var black = m.Lit("PP_WaiterBlack", new Color(0.08f, 0.08f, 0.09f));
            if (style == PedestrianStyle.Chef)
            {
                coat = white;
                pants = m.LitTex("PP_ChefCheck", PetitParisTextures.Checker("PP_ChefCheckTex", new Color(0.1f, 0.1f, 0.12f), new Color(0.92f, 0.92f, 0.9f), 12), Color.white);
            }
            else if (style == PedestrianStyle.Waiter)
            {
                coat = white;
                pants = black;
                shoeMat = black;
            }

            var body = Group("Body", root);
            RemoveCollider(Box("Hips", body, new Vector3(0f, 0.9f, 0f), new Vector3(0.36f, 0.14f, 0.22f), pants));
            RemoveCollider(Box("Torso", body, new Vector3(0f, 1.2f, 0f), new Vector3(0.42f, 0.56f, 0.24f), coat));
            RemoveCollider(Cylinder("Arm_L", body, new Vector3(-0.26f, 1.15f, 0f), 0.09f, 0.58f, coat));
            RemoveCollider(Cylinder("Arm_R", body, new Vector3(0.26f, 1.15f, 0f), 0.09f, 0.58f, coat));
            RemoveCollider(Sphere("Head", body, new Vector3(0f, 1.62f, 0f), 0.22f, skin));
            RemoveCollider(Sphere("Nose", body, new Vector3(0f, 1.61f, 0.11f), 0.05f, skin));
            if (style == PedestrianStyle.Chef)
            {
                // 셰프: 높은 모자, 빨간 스카프, 두 줄 단추, 앞치마, 콧수염
                RemoveCollider(Cylinder("ToqueBand", body, new Vector3(0f, 1.79f, 0f), 0.22f, 0.12f, white));
                var puff = RemoveCollider(Sphere("ToquePuff", body, new Vector3(0f, 1.93f, 0f), 0.3f, white));
                puff.transform.localScale = new Vector3(0.3f, 0.22f, 0.3f);
                RemoveCollider(Box("Neckerchief", body, new Vector3(0f, 1.47f, 0.1f), new Vector3(0.16f, 0.07f, 0.06f), m.Lit("PP_ChefRed", new Color(0.75f, 0.12f, 0.12f))));
                for (int i = 0; i < 3; i++)
                    foreach (float x in new[] { -0.07f, 0.07f })
                        RemoveCollider(Sphere("Button", body, new Vector3(x, 1.36f - i * 0.12f, 0.122f), 0.03f, black));
                RemoveCollider(Box("Apron", body, new Vector3(0f, 0.78f, 0.13f), new Vector3(0.38f, 0.55f, 0.02f), white));
                RemoveCollider(Box("Mustache", body, new Vector3(0f, 1.575f, 0.112f), new Vector3(0.1f, 0.02f, 0.02f), m.Lit("PP_Mustache", new Color(0.2f, 0.12f, 0.08f))));
            }
            else if (style == PedestrianStyle.Waiter)
            {
                // 파리 카페 웨이터: 흰 셔츠 + 검은 조끼 + 나비넥타이 + 발목까지 오는 긴 흰 앞치마
                RemoveCollider(Box("Vest", body, new Vector3(0f, 1.2f, 0.005f), new Vector3(0.44f, 0.5f, 0.24f), black));
                RemoveCollider(Box("BowTie", body, new Vector3(0f, 1.46f, 0.125f), new Vector3(0.1f, 0.04f, 0.02f), black));
                RemoveCollider(Box("LongApron", body, new Vector3(0f, 0.55f, 0.13f), new Vector3(0.4f, 0.75f, 0.02f), white));
                RemoveCollider(Sphere("Hair", body, new Vector3(0f, 1.68f, -0.01f), 0.21f, black)).transform.localScale = new Vector3(0.23f, 0.15f, 0.23f);
            }

            var footL = Shoe("Foot_L", root, shoeMat);
            var footR = Shoe("Foot_R", root, shoeMat);
            var legL = RemoveCollider(Cylinder("Leg_L", root, Vector3.up * 0.5f, 0.12f, 0.8f, pants));
            var legR = RemoveCollider(Cylinder("Leg_R", root, Vector3.up * 0.5f, 0.12f, 0.8f, pants));

            var shadowL = BlobQuad("Shadow_Foot_L", root, blob);
            var shadowR = BlobQuad("Shadow_Foot_R", root, blob);
            var shadowBody = BlobQuad("Shadow_Body", root, blob);
            var landing = BlobQuad("LandingMarker", root, m.Blob("PP_LandingMarker", new Color(0.35f, 0f, 0f, 0.5f)));

            // 에디터 미리보기용 초기 배치 (Play하면 PedestrianPath가 매 프레임 갱신)
            body.position = waypoints[0];
            footL.transform.position = waypoints[0] + new Vector3(-0.1f, 0.045f, 0f);
            footR.transform.position = waypoints[0] + new Vector3(0.1f, 0.045f, 0f);

            var path = root.gameObject.AddComponent<PedestrianPath>();
            var so = new SerializedObject(path);
            SetObjectArray(so, "waypoints", wps);
            SetInt(so, "loopMode", (int)mode);
            SetFloat(so, "walkSpeed", speed);
            SetFloat(so, "startDelay", startDelay);
            SetObject(so, "body", body);
            SetObject(so, "leftFoot", footL.transform);
            SetObject(so, "rightFoot", footR.transform);
            SetObject(so, "leftLeg", legL.transform);
            SetObject(so, "rightLeg", legR.transform);
            SetObject(so, "leftShadow", shadowL);
            SetObject(so, "rightShadow", shadowR);
            SetObject(so, "bodyShadow", shadowBody);
            SetObject(so, "landingMarker", landing);
            SetObject(so, "leftAudio", footL.GetComponent<AudioSource>());
            SetObject(so, "rightAudio", footR.GetComponent<AudioSource>());
            so.ApplyModifiedPropertiesWithoutUndo();
            return path;
        }

        static GameObject Shoe(string name, Transform parent, Material material)
        {
            var shoe = Box(name, parent, Vector3.zero, new Vector3(0.11f, 0.09f, 0.28f), material);
            shoe.GetComponent<BoxCollider>().isTrigger = true; // 밀어내지 않는 판정용
            var rb = shoe.AddComponent<Rigidbody>();           // 움직이는 Trigger는 Kinematic Rigidbody 권장 (성능)
            rb.isKinematic = true;
            rb.useGravity = false;
            shoe.AddComponent<FootstepHazard>();
            var audio = shoe.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            return shoe;
        }

        public static BicycleHazard Bicycle(string name, Transform parent, Vector3 a, Vector3 b, MaterialLibrary m)
        {
            var root = Group(name, parent);
            var pa = Group("PointA", root); pa.position = a;
            var pb = Group("PointB", root); pb.position = b;

            var bike = Group("Bike", root);
            bike.position = a;
            var frameMat = m.Lit("PP_BikeFrame", new Color(0.12f, 0.36f, 0.32f));
            var tire = m.Lit("PP_BikeTire", new Color(0.08f, 0.08f, 0.08f));
            var rider = m.Lit("PP_RiderCoat", new Color(0.62f, 0.2f, 0.18f));
            var skin = m.Lit("PP_HumanSkin", new Color(0.85f, 0.7f, 0.6f));

            var wheels = new List<Transform>();
            foreach (float z in new[] { -0.52f, 0.52f })
            {
                var w = RemoveCollider(Cylinder(z > 0 ? "Wheel_Front" : "Wheel_Rear", bike, new Vector3(0f, 0.34f, z), 0.68f, 0.04f, tire));
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // 바퀴 축 = 자전거 좌우
                wheels.Add(w.transform);
            }
            RemoveCollider(Box("Frame", bike, new Vector3(0f, 0.55f, 0f), new Vector3(0.04f, 0.05f, 1.0f), frameMat));
            RemoveCollider(Box("SeatPost", bike, new Vector3(0f, 0.72f, -0.2f), new Vector3(0.03f, 0.35f, 0.03f), frameMat));
            RemoveCollider(Box("Handlebar", bike, new Vector3(0f, 0.95f, 0.42f), new Vector3(0.5f, 0.03f, 0.03f), frameMat));
            RemoveCollider(Box("RiderTorso", bike, new Vector3(0f, 1.25f, -0.05f), new Vector3(0.4f, 0.55f, 0.24f), rider));
            RemoveCollider(Sphere("RiderHead", bike, new Vector3(0f, 1.66f, 0.02f), 0.22f, skin));
            RemoveCollider(Box("RiderLegs", bike, new Vector3(0f, 0.7f, -0.05f), new Vector3(0.34f, 0.45f, 0.18f), m.Lit("PP_HumanPants", new Color(0.2f, 0.26f, 0.42f))));

            var hazardGO = new GameObject("HazardBox");
            hazardGO.transform.SetParent(bike, false);
            var box = hazardGO.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.35f, 0f);
            box.size = new Vector3(0.3f, 0.7f, 1.75f);
            var rb = hazardGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var hazard = hazardGO.AddComponent<FootstepHazard>();
            var hso = new SerializedObject(hazard);
            SetString(hso, "hazardName", "자전거");
            SetFloat(hso, "warningRadius", 0.9f);
            hso.ApplyModifiedPropertiesWithoutUndo();

            var roll = bike.gameObject.AddComponent<AudioSource>();
            var bell = bike.gameObject.AddComponent<AudioSource>();
            var shadow = BlobQuad("Shadow", root, m.Blob("PP_BlobShadow", new Color(0f, 0f, 0f, 0.5f)));

            var bicycle = root.gameObject.AddComponent<BicycleHazard>();
            var so = new SerializedObject(bicycle);
            SetObject(so, "pointA", pa);
            SetObject(so, "pointB", pb);
            SetObject(so, "bike", bike);
            SetObjectArray(so, "wheels", wheels.ToArray());
            SetObject(so, "shadow", shadow);
            SetObject(so, "rollingAudio", roll);
            SetObject(so, "bellAudio", bell);
            so.ApplyModifiedPropertiesWithoutUndo();
            return bicycle;
        }

        static RatBlobShadow BlobQuad(string name, Transform parent, Material material)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            RemoveCollider(q);
            q.transform.SetParent(parent, false);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.AddComponent<RatBlobShadow>();
        }

        /// <summary>체크포인트 (바닥 위치 + 바라볼 방향) + 작은 깃발 표시. 깃발 Renderer를 돌려준다.</summary>
        public static Renderer Checkpoint(string name, Transform parent, Vector3 position, float yaw, MaterialLibrary m)
        {
            var cp = Group(name, parent);
            cp.position = position;
            cp.rotation = Quaternion.Euler(0f, yaw, 0f);
            var flagRoot = Group("Flag", cp);
            flagRoot.localPosition = new Vector3(0.12f, 0f, 0.05f);
            RemoveCollider(Cylinder("Pole", flagRoot, new Vector3(0f, 0.12f, 0f), 0.006f, 0.24f, m.Lit("PP_FlagPole", new Color(0.3f, 0.3f, 0.3f))));
            var flag = RemoveCollider(Box("Cloth", flagRoot, new Vector3(0.03f, 0.21f, 0f), new Vector3(0.06f, 0.04f, 0.004f), m.Lit("PP_Flag", Color.white)));
            return flag.GetComponent<Renderer>();
        }

        // ───────────────────────── 도형·라벨 ─────────────────────────

        public static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            if (parent != null) t.SetParent(parent, false);
            return t;
        }

        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>x/z 범위로 박스 만들기 (바닥 y0~y1). 블록아웃 벽·건물에 편리.</summary>
        public static GameObject BoxXZ(string name, Transform parent, float x0, float x1, float z0, float z1, float y0, float y1, Material material)
        {
            return Box(name, parent, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f),
                new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), material);
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 center, float diameter, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>원기둥. 기본 CapsuleCollider는 납작하면 구가 되므로 Convex MeshCollider로 교체.</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 center, float diameter, float height, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            mc.convex = true;
            return go;
        }

        public static TextMeshPro Label(string name, Transform parent, Vector3 position, string text, float fontSize, float width,
            Color color, TMP_FontAsset font = null, Vector3? faceTowards = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            if (faceTowards.HasValue)
            {
                Vector3 d = go.transform.position - faceTowards.Value;
                d.y = 0f;
                if (d.sqrMagnitude > 1e-4f) go.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            }
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(width, fontSize * 0.35f * (1 + text.Count(c => c == '\n')));
            return tmp;
        }

        /// <summary>어두운 배경 + 글자 패널 (월드 공간). 안내·완료 화면용.</summary>
        public static (GameObject, TextMeshPro) Panel(string name, Transform parent, Vector3 position, Quaternion rotation, Vector2 size,
            string text, float fontSize, TMP_FontAsset font, MaterialLibrary m)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            RemoveCollider(bg);
            bg.transform.SetParent(root.transform, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            bg.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = bg.GetComponent<MeshRenderer>();
            r.sharedMaterial = m.Unlit("PP_PanelBackground", new Color(0.08f, 0.08f, 0.1f));
            r.shadowCastingMode = ShadowCastingMode.Off;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(root.transform, false);
            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = size * 0.92f;
            return (root, tmp);
        }

        /// <summary>
        /// Panel()로 만든 패널을 깊이 검사 없이 맨 위에 그리게 바꾼다 (다른 물체에 가려지지 않음).
        /// 그리는 순서는 화면 페이드(Overlay+100)보다 앞이라, 페이드가 켜지면 패널도 덮인다.
        /// 완료 패널용: 목적지 치즈 바퀴에 코를 대고 도착하면 눈앞 패널 자리가 바퀴 속이다.
        /// </summary>
        public const int OverlayPanelQueue = (int)RenderQueue.Overlay + 50;

        public static void MakeOverlay(GameObject panelRoot, MaterialLibrary m)
        {
            var bg = panelRoot.transform.Find("Background");
            var bgRenderer = bg != null ? bg.GetComponent<MeshRenderer>() : null;
            var fadeShader = Shader.Find(PetitParisPaths.FadeShader);
            if (bgRenderer != null && fadeShader != null)
            {
                var mat = m.Custom("PP_PanelBackgroundOverlay", fadeShader, new Color(0.08f, 0.08f, 0.1f, 1f));
                mat.renderQueue = OverlayPanelQueue;
                bgRenderer.sharedMaterial = mat;
            }

            var tmp = panelRoot.GetComponentInChildren<TextMeshPro>(true);
            var overlayShader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");
            if (tmp == null || overlayShader == null || tmp.fontSharedMaterial == null)
            {
                Debug.LogWarning("[PetitParis] 패널 글자를 맨 위에 그리지 못했습니다 (TMP Overlay 셰이더 또는 글꼴 머티리얼 없음): " + panelRoot.name);
                return;
            }
            string path = $"{PetitParisPaths.Materials}/PP_PanelTextOverlay.mat";
            var textMat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (textMat == null)
            {
                textMat = new Material(tmp.fontSharedMaterial);
                AssetDatabase.CreateAsset(textMat, path);
            }
            else
            {
                textMat.CopyPropertiesFromMaterial(tmp.fontSharedMaterial); // 글꼴 아틀라스 등 최신 값
            }
            textMat.shader = overlayShader;
            textMat.renderQueue = OverlayPanelQueue + 10; // 배경 다음
            EditorUtility.SetDirty(textMat);
            tmp.fontSharedMaterial = textMat;
        }

        public static GameObject RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            return go;
        }

        public static void MarkStatic(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<TMP_Text>() != null || t.GetComponent<RatSafeZone>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        // ───────────────────────── 에셋 ─────────────────────────

        public static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// 나눔고딕(OFL) TTF로 한글 TMP 폰트 에셋을 만든다 (Dynamic + 게임 문장 글자 미리 채움).
        /// 실패하면 null → 기본 폰트(영문)로 대체.
        /// </summary>
        public static TMP_FontAsset KoreanFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PetitParisPaths.KoreanFontAsset);
            if (existing != null)
            {
                existing.TryAddCharacters(PetitParisText.AllCharacters(), out _);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(PetitParisPaths.KoreanFontFile);
            if (font == null)
            {
                Debug.LogWarning("[PetitParis] 한글 폰트 파일이 없습니다: " + PetitParisPaths.KoreanFontFile);
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (asset == null)
            {
                Debug.LogWarning("[PetitParis] 한글 TMP 폰트 에셋 생성에 실패했습니다.");
                return null;
            }
            asset.name = Path.GetFileNameWithoutExtension(PetitParisPaths.KoreanFontAsset);
            AssetDatabase.CreateAsset(asset, PetitParisPaths.KoreanFontAsset);
            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
            {
                foreach (var tex in asset.atlasTextures)
                {
                    if (tex == null) continue;
                    tex.name = asset.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(tex, asset);
                }
            }
            asset.TryAddCharacters(PetitParisText.AllCharacters(), out string missingChars);
            if (!string.IsNullOrEmpty(missingChars))
                Debug.LogWarning("[PetitParis] 폰트에 없는 글자: " + missingChars);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static Dictionary<string, InputActionReference> LoadActionReferences(string assetPath)
        {
            var result = new Dictionary<string, InputActionReference>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is InputActionReference r && r.action != null && r.action.actionMap != null)
                    result[$"{r.action.actionMap.name}/{r.action.name}"] = r;
            }
            return result;
        }

        // ───────────────────────── SerializedObject 헬퍼 ─────────────────────────

        public static void Wire(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields) SetObject(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            SetBool(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Prop(SerializedObject so, string name)
        {
            var p = so.FindProperty(name);
            if (p == null)
                Debug.LogError($"[PetitParis] {so.targetObject.GetType().Name}에 '{name}' 필드가 없습니다. 패키지 버전 차이일 수 있습니다.", so.targetObject);
            return p;
        }

        public static void SetObject(SerializedObject so, string name, Object value) { var p = Prop(so, name); if (p != null) p.objectReferenceValue = value; }
        public static void SetInt(SerializedObject so, string name, int value) { var p = Prop(so, name); if (p != null) p.intValue = value; }
        public static void SetFloat(SerializedObject so, string name, float value) { var p = Prop(so, name); if (p != null) p.floatValue = value; }
        public static void SetBool(SerializedObject so, string name, bool value) { var p = Prop(so, name); if (p != null) p.boolValue = value; }
        public static void SetString(SerializedObject so, string name, string value) { var p = Prop(so, name); if (p != null) p.stringValue = value; }

        public static void SetObjectArray(SerializedObject so, string name, Object[] values)
        {
            var p = Prop(so, name);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetActionProperty(SerializedObject so, string name, InputActionReference reference)
        {
            var p = Prop(so, name);
            if (p == null) return;
            var useReference = p.FindPropertyRelative("m_UseReference");
            var refProp = p.FindPropertyRelative("m_Reference");
            if (useReference == null || refProp == null)
            {
                Debug.LogError($"[PetitParis] {name}의 InputActionProperty 구조를 읽지 못했습니다.", so.targetObject);
                return;
            }
            useReference.boolValue = true;
            refProp.objectReferenceValue = reference;
        }

        public static void Fail(string message)
        {
            Debug.LogError("[PetitParis] " + message);
            if (!AssumeYes) EditorUtility.DisplayDialog("PETIT PARIS", message, "확인");
        }
    }

    /// <summary>Assets/PetitParis/Materials에 단색 머티리얼을 만들거나 재사용한다.</summary>
    public class MaterialLibrary
    {
        readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public Material Lit(string name, Color color) =>
            Get(name, FindShader("Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Lit"), color);

        public Material Unlit(string name, Color color) =>
            Get(name, FindShader("Universal Render Pipeline/Unlit", "Universal Render Pipeline/Simple Lit"), color);

        public Material Blob(string name, Color color) => Get(name, FindShader(PetitParisPaths.BlobShader), color);

        public Material Custom(string name, Shader shader, Color color) => Get(name, shader, color);

        /// <summary>질감 + 색조 (Simple Lit, 안개·그림자 지원).</summary>
        public Material LitTex(string name, Texture2D texture, Color tint) => WithTexture(Lit(name, tint), texture);

        /// <summary>스스로 빛나 보이는 질감 (Unlit: 창문 불빛, 강물, 원경).</summary>
        public Material UnlitTex(string name, Texture2D texture, Color tint) => WithTexture(Unlit(name, tint), texture);

        static Material WithTexture(Material mat, Texture2D texture)
        {
            if (texture == null) return mat;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        Material Get(string name, Shader shader, Color color)
        {
            if (cache.TryGetValue(name, out var cached)) return cached;
            string path = $"{PetitParisPaths.Materials}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            cache[name] = mat;
            return mat;
        }

        static Shader FindShader(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            Debug.LogError("[PetitParis] 셰이더를 찾을 수 없습니다: " + string.Join(", ", names));
            return Shader.Find("Hidden/InternalErrorShader");
        }
    }
}
