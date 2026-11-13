using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 현재 열린 PETIT PARIS 씬과 프로젝트 설정을 점검한다. 아무것도 수정하지 않는다.
    /// 결과는 Console에 줄 단위로 출력된다 (OK / WARN / FAIL / INFO).
    /// </summary>
    public static class PetitParisValidator
    {
        [MenuItem("PetitParis/4. Validate Scene", priority = 4)]
        public static void Validate()
        {
            var report = new Report();
            var scene = EditorSceneManager.GetActiveScene();
            bool isGame = scene.path == PetitParisPaths.Scene;
            bool isSandbox = scene.path == PetitParisPaths.SandboxScene;
            report.Check(isGame || isSandbox, $"활성 씬: {scene.path}", "PETIT PARIS 씬(PetitParisVR / PetitParisSandbox)이 아닙니다.", warnOnly: true);

            CheckRig(report);
            CheckLocomotion(report);
            CheckInput(report);
            CheckGameplay(report, isGame);
            CheckProject(report);
            report.Flush();
        }

        static void CheckRig(Report report)
        {
            var origins = Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            report.Check(origins.Length == 1, $"XR Origin 1개", $"XR Origin이 {origins.Length}개입니다 (정확히 1개여야 함).");
            int mainCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Count(c => c.CompareTag("MainCamera"));
            report.Check(mainCams == 1, "MainCamera 1개", $"MainCamera가 {mainCams}개입니다.");
            int listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            report.Check(listeners == 1, "AudioListener 1개", $"AudioListener가 {listeners}개입니다.");

            var rig = Object.FindAnyObjectByType<RatScaleRig>();
            if (!report.Check(rig != null && rig.IsValid, "RatScaleRig 참조 연결됨", "RatScaleRig가 없거나 Config/Tracking Space/Eye Camera 참조가 비어 있습니다.")) return;
            var cfg = rig.Config;
            report.Check((rig.transform.lossyScale - Vector3.one).sqrMagnitude < 1e-6f, "XR Origin 스케일 1", "XR Origin 스케일이 1이 아닙니다.");
            report.Check(rig.gameObject.layer == PetitParisBuildKit.PlayerLayer, "XR Origin 레이어 = Ignore Raycast (자기 자신 충돌 검사 제외)",
                "XR Origin 레이어가 Ignore Raycast가 아닙니다. 질주 충돌 검사가 자기 몸에 걸릴 수 있습니다.", warnOnly: true);
            report.Check(rig.EyeCamera.transform.IsChildOf(rig.TrackingSpace), "Main Camera가 Tracking Space 아래", "Main Camera가 Tracking Space 밖에 있습니다.");
            report.Check(rig.Body != null, $"CharacterController 반지름 {cfg.bodyRadius * 100f:F1}cm 높이 {cfg.bodyHeight * 100f:F1}cm", "CharacterController가 없습니다.");
            report.Info($"배율 x{cfg.Magnification:F1}, 눈높이 {cfg.targetEyeHeight * 100f:F0}cm, 걷기 {cfg.walkSpeed:F2} m/s(체감 {cfg.ToPerceived(cfg.walkSpeed):F1}), 질주 {cfg.dashSpeed:F2} m/s(체감 {cfg.ToPerceived(cfg.dashSpeed):F1})");

            CheckPoseDriver(report, rig.EyeCamera.GetComponent<TrackedPoseDriver>(), "Main Camera");
            foreach (var name in new[] { "Left Controller", "Right Controller" })
            {
                var t = rig.TrackingSpace.Find(name);
                CheckPoseDriver(report, t != null ? t.GetComponent<TrackedPoseDriver>() : null, name);
            }
        }

        /// <summary>이동 처리 중복 점검: 이동 Provider는 RatLocomotionController 하나뿐이어야 한다.</summary>
        static void CheckLocomotion(Report report)
        {
            var mediators = Object.FindObjectsByType<LocomotionMediator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            report.Check(mediators.Length == 1, "LocomotionMediator 1개", $"LocomotionMediator가 {mediators.Length}개입니다.");
            var transformers = Object.FindObjectsByType<XRBodyTransformer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            report.Check(transformers.Length == 1, "XRBodyTransformer 1개", $"XRBodyTransformer가 {transformers.Length}개입니다.");
            if (transformers.Length == 1)
            {
                var so = new SerializedObject(transformers[0]);
                var useCC = so.FindProperty("m_UseCharacterControllerIfExists");
                report.Check(useCC != null && useCC.boolValue, "XRBodyTransformer: CharacterController 사용 (충돌 처리)",
                    "XRBodyTransformer가 CharacterController를 사용하지 않습니다 → 벽을 통과할 수 있습니다.");
            }

            var providers = Object.FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allowed = new HashSet<string> { nameof(RatLocomotionController), nameof(RatComfortApplier), nameof(SnapTurnProvider) };
            var unexpected = providers.Where(p => !allowed.Contains(p.GetType().Name)).Select(p => $"{p.GetType().Name}({p.name})").ToList();
            report.Check(unexpected.Count == 0, $"이동 Provider: {string.Join(", ", providers.Select(p => p.GetType().Name))}",
                "중복 가능성이 있는 다른 이동 Provider가 있습니다 (ContinuousMove/Gravity/Jump 등): " + string.Join(", ", unexpected));
            int movers = providers.Count(p => p is RatLocomotionController);
            report.Check(movers == 1, "실제 이동(Translation)을 넣는 Provider = RatLocomotionController 1개", $"RatLocomotionController가 {movers}개입니다.");
            foreach (var p in providers)
            {
                var m = new SerializedObject(p).FindProperty("m_Mediator");
                report.Check(m != null && m.objectReferenceValue != null, $"{p.GetType().Name}.Mediator 연결", $"{p.GetType().Name}의 Mediator가 비어 있습니다 (실행 시 자동 탐색).", warnOnly: true);
            }

            var snaps = Object.FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (report.Check(snaps.Length == 1, "SnapTurnProvider 1개", $"SnapTurnProvider가 {snaps.Length}개입니다."))
            {
                var so = new SerializedObject(snaps[0]);
                var left = so.FindProperty("m_LeftHandTurnInput.m_InputSourceMode");
                var right = so.FindProperty("m_RightHandTurnInput.m_InputActionReference");
                var amount = so.FindProperty("m_TurnAmount");
                report.Check(left != null && left.intValue == 0, "스냅 회전: 왼쪽 스틱 사용 안 함 (이동 전용)", "SnapTurn 왼손 입력이 켜져 있습니다 → 왼쪽 스틱이 이동과 회전을 동시에 합니다.");
                var turnRef = right != null ? right.objectReferenceValue as InputActionReference : null;
                report.Check(turnRef != null && turnRef.action != null && turnRef.action.name == "Turn", "스냅 회전: 오른쪽 스틱 = Rat/Turn", "SnapTurn 오른손 입력이 Rat/Turn이 아닙니다.");
                if (amount != null) report.Info($"스냅 회전 각도(직렬화 값) {amount.floatValue:0}도 — 실행 시 RatComfortSettings 값으로 덮어씀");
            }

            var comfort = Object.FindAnyObjectByType<RatComfortApplier>();
            if (report.Check(comfort != null, "RatComfortApplier 있음", "RatComfortApplier가 없습니다 (비네트·회전 각도 설정 미적용)."))
            {
                var so = new SerializedObject(comfort);
                var settings = so.FindProperty("comfort")?.objectReferenceValue as RatComfortSettings;
                if (report.Check(settings != null, "RatComfortSettings 연결", "RatComfortApplier에 RatComfortSettings가 없습니다."))
                    report.Info($"편안함: 회전 {settings.snapTurnDegrees:0}도, 질주 {(settings.enableDash ? "on" : "off")}, 점프 {(settings.enableJump ? "on" : "off")}, " +
                                $"질주 비네트 {(settings.vignetteOnDash ? "on" : "off")}, 속도상한 {(settings.limitSpeed ? settings.maxPerceivedSpeed.ToString("0.0") + " m/s" : "off")}");
            }

            bool vignetteLinked = false;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "TunnelingVignetteController") continue;
                var list = new SerializedObject(mb).FindProperty("m_LocomotionVignetteProviders");
                if (list == null) continue;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).FindPropertyRelative("m_Locomotion")?.objectReferenceValue is RatComfortApplier) vignetteLinked = true;
            }
            report.Check(vignetteLinked, "Comfort Vignette ← RatComfortApplier 연결", "TunnelingVignetteController에 RatComfortApplier가 연결되지 않았습니다 (비네트 미동작).", warnOnly: true);

            var loco = Object.FindAnyObjectByType<RatLocomotionController>();
            if (loco != null)
            {
                var so = new SerializedObject(loco);
                foreach (var f in new[] { "rig", "input", "comfort", "dash", "jump", "crouch" })
                    report.Check(so.FindProperty(f)?.objectReferenceValue != null, $"RatLocomotionController.{f} 연결", $"RatLocomotionController.{f}가 비어 있습니다.");
            }
        }

        static void CheckInput(Report report)
        {
            var reader = Object.FindAnyObjectByType<RatInputReader>();
            if (!report.Check(reader != null && reader.Actions != null, $"입력 에셋: {(reader != null ? AssetDatabase.GetAssetPath(reader.Actions) : "-")}", "RatInputReader 또는 Input Action Asset이 없습니다.")) return;
            var asset = reader.Actions;
            var missing = RatInputReader.RequiredActionNames.Where(n => asset.FindAction(n, false) == null).ToList();
            report.Check(missing.Count == 0, $"필수 액션 {RatInputReader.RequiredActionNames.Length}개 모두 있음", "없는 액션: " + string.Join(", ", missing));
            foreach (var name in RatInputReader.RequiredActionNames)
            {
                var a = asset.FindAction(name, false);
                if (a == null) continue;
                bool hasXR = a.bindings.Any(b => b.path.StartsWith("<XRController>") || b.path.StartsWith("<XRHMD>"));
                report.Check(hasXR, $"{name}: {string.Join(" | ", a.bindings.Where(b => !b.isComposite && !b.isPartOfComposite).Select(b => b.path))}",
                    $"{name}에 XR 컨트롤러/HMD 바인딩이 없습니다.");
            }
        }

        static void CheckGameplay(Report report, bool isGame)
        {
            var danger = Object.FindAnyObjectByType<RatDangerDetector>();
            var respawn = Object.FindAnyObjectByType<RatRespawn>();
            var checkpoints = Object.FindAnyObjectByType<CheckpointManager>();
            report.Check(danger != null, "RatDangerDetector 있음", "RatDangerDetector가 없습니다.");
            report.Check(respawn != null, "RatRespawn 있음", "RatRespawn이 없습니다.");
            if (report.Check(checkpoints != null && checkpoints.Count > 0, $"체크포인트 {checkpoints?.Count ?? 0}개", "CheckpointManager 또는 체크포인트가 없습니다."))
                report.Check(respawn == null || new SerializedObject(respawn).FindProperty("checkpoints")?.objectReferenceValue != null,
                    "RatRespawn → CheckpointManager 연결", "RatRespawn에 CheckpointManager가 비어 있습니다 (실행 시 자동 탐색).", warnOnly: true);

            var zones = Object.FindObjectsByType<RatSafeZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int badZones = zones.Count(z => z.GetComponent<Collider>() == null || !z.GetComponent<Collider>().isTrigger);
            report.Check(zones.Length > 0, $"안전 구역 {zones.Length}개", "안전 구역(RatSafeZone)이 없습니다.");
            report.Check(badZones == 0, "안전 구역 Collider = Trigger", $"Trigger가 아닌 안전 구역 {badZones}개 (실행 시 자동으로 Trigger로 바뀜).", warnOnly: true);

            var hazards = Object.FindObjectsByType<FootstepHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int solid = hazards.Count(h => !h.GetComponent<BoxCollider>().isTrigger);
            report.Check(hazards.Length > 0, $"위험 물체(FootstepHazard) {hazards.Length}개", "위험 물체가 없습니다.", warnOnly: true);
            report.Check(solid == 0, "위험 물체 Collider = Trigger (쥐를 밀어내지 않음)", $"Trigger가 아닌 위험 물체 {solid}개 → 쥐를 밀 수 있습니다.");

            var walkers = Object.FindObjectsByType<PedestrianPath>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            report.Info($"보행자 {walkers.Length}명, 자전거 {Object.FindObjectsByType<BicycleHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}대");

            if (isGame)
            {
                var flow = Object.FindAnyObjectByType<PetitParisGameFlow>();
                if (report.Check(flow != null, "PetitParisGameFlow 있음", "PetitParisGameFlow가 없습니다."))
                {
                    var so = new SerializedObject(flow);
                    foreach (var f in new[] { "goalVolume", "introPanel", "completePanel", "completeText", "objectiveMarker" })
                        report.Check(so.FindProperty(f)?.objectReferenceValue != null, $"GameFlow.{f} 연결", $"GameFlow.{f}가 비어 있습니다.");
                }
                report.Check(walkers.Length == 2, "보행자 2명", $"보행자가 {walkers.Length}명입니다 (기획: 2명).", warnOnly: true);
            }
        }

        static void CheckProject(Report report)
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            string Version(string id) => packages.FirstOrDefault(p => p.name == id)?.version;
            foreach (var id in new[] { "com.unity.xr.interaction.toolkit", "com.unity.xr.core-utils", "com.unity.inputsystem", "com.unity.xr.management", "com.unity.xr.openxr", "com.unity.render-pipelines.universal" })
            {
                string v = Version(id);
                report.Check(v != null, $"{id} {v}", $"{id} 미설치", warnOnly: id == "com.unity.xr.management" || id == "com.unity.xr.openxr");
            }
            report.Check(Version("com.unity.xr.interaction.toolkit") == "3.3.2", "XRI 3.3.2 (코드 작성 기준 버전)", $"XRI 버전이 {Version("com.unity.xr.interaction.toolkit")}입니다. 코드는 3.3.2 기준으로 작성되었습니다.", warnOnly: true);
            report.Check(Object.FindObjectsByType<RatLocomotionController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length <= 1, "이동 컨트롤러 중복 없음", "RatLocomotionController가 여러 개입니다.");

            report.Check(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android, "빌드 타깃 Android",
                $"빌드 타깃 {EditorUserBuildSettings.activeBuildTarget}. Build Profiles에서 Android로 Switch Platform 하세요.", warnOnly: true);
            report.Check((int)PlayerSettings.Android.minSdkVersion >= PetitParisProjectSetup.MinAndroidApi,
                $"Android Min API {(int)PlayerSettings.Android.minSdkVersion}", $"Android Min API가 {PetitParisProjectSetup.MinAndroidApi} 미만 (메뉴 0번).", warnOnly: true);
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            report.Check(apis.Length > 0 && apis[0] == GraphicsDeviceType.Vulkan, "Android Graphics API = Vulkan", "Android Graphics API 첫 번째가 Vulkan이 아닙니다 (메뉴 0번).", warnOnly: true);
            var buildScenes = EditorBuildSettings.scenes;
            report.Check(buildScenes.Length > 0 && buildScenes[0].path == PetitParisPaths.Scene && buildScenes[0].enabled,
                "Build 씬 목록 첫 번째 = PetitParisVR", "PetitParisVR이 Build 씬 목록 맨 앞에 없습니다 (메뉴 3번).", warnOnly: true);
        }

        static void CheckPoseDriver(Report report, TrackedPoseDriver driver, string owner)
        {
            if (!report.Check(driver != null, $"{owner}: TrackedPoseDriver", $"{owner}에 TrackedPoseDriver가 없습니다.")) return;
            var so = new SerializedObject(driver);
            foreach (var prop in new[] { "m_PositionInput", "m_RotationInput", "m_TrackingStateInput" })
            {
                var r = so.FindProperty(prop)?.FindPropertyRelative("m_Reference");
                var reference = r != null ? r.objectReferenceValue as InputActionReference : null;
                report.Check(reference != null && reference.action != null,
                    $"{owner}.{prop} → {(reference != null && reference.action != null ? reference.action.name : "-")}", $"{owner}.{prop} 입력 참조가 비어 있습니다.");
            }
        }

        class Report
        {
            readonly List<string> lines = new List<string>();
            int fails, warns;

            public bool Check(bool ok, string okText, string failText, bool warnOnly = false)
            {
                if (ok) lines.Add("OK   " + okText);
                else if (warnOnly) { lines.Add("WARN " + failText); warns++; }
                else { lines.Add("FAIL " + failText); fails++; }
                return ok;
            }

            public void Info(string text) => lines.Add("INFO " + text);

            public void Flush()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[PetitParis] 검증 결과: FAIL {fails} / WARN {warns}");
                foreach (var l in lines) sb.AppendLine(l);
                if (fails > 0) Debug.LogError(sb.ToString());
                else if (warns > 0) Debug.LogWarning(sb.ToString());
                else Debug.Log(sb.ToString());
                EditorUtility.DisplayDialog("PETIT PARIS 검증", $"FAIL {fails}개, WARN {warns}개\n자세한 내용은 Console을 확인하세요.", "확인");
            }
        }
    }
}
