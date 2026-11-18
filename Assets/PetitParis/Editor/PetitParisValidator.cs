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
            var origins = Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include);
            report.Check(origins.Length == 1, $"XR Origin 1개", $"XR Origin이 {origins.Length}개입니다 (정확히 1개여야 함).");
            int mainCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).Count(c => c.CompareTag("MainCamera"));
            report.Check(mainCams == 1, "MainCamera 1개", $"MainCamera가 {mainCams}개입니다.");
            int listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length;
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

            CheckSpawnOpenness(report, rig.transform.position + Vector3.up * cfg.targetEyeHeight);

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
            var mediators = Object.FindObjectsByType<LocomotionMediator>(FindObjectsInactive.Include);
            report.Check(mediators.Length == 1, "LocomotionMediator 1개", $"LocomotionMediator가 {mediators.Length}개입니다.");
            var transformers = Object.FindObjectsByType<XRBodyTransformer>(FindObjectsInactive.Include);
            report.Check(transformers.Length == 1, "XRBodyTransformer 1개", $"XRBodyTransformer가 {transformers.Length}개입니다.");
            if (transformers.Length == 1)
            {
                var so = new SerializedObject(transformers[0]);
                var useCC = so.FindProperty("m_UseCharacterControllerIfExists");
                report.Check(useCC != null && useCC.boolValue, "XRBodyTransformer: CharacterController 사용 (충돌 처리)",
                    "XRBodyTransformer가 CharacterController를 사용하지 않습니다 → 벽을 통과할 수 있습니다.");
            }

            var providers = Object.FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include);
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

            var snaps = Object.FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include);
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
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb.GetType().Name != "TunnelingVignetteController") continue;
                var list = new SerializedObject(mb).FindProperty("m_LocomotionVignetteProviders");
                if (list == null) continue;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).FindPropertyRelative("m_LocomotionProvider")?.objectReferenceValue is RatComfortApplier) vignetteLinked = true;
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

            var zones = Object.FindObjectsByType<RatSafeZone>(FindObjectsInactive.Include);
            int badZones = zones.Count(z => z.GetComponent<Collider>() == null || !z.GetComponent<Collider>().isTrigger);
            report.Check(zones.Length > 0, $"안전 구역 {zones.Length}개", "안전 구역(RatSafeZone)이 없습니다.");
            report.Check(badZones == 0, "안전 구역 Collider = Trigger", $"Trigger가 아닌 안전 구역 {badZones}개 (실행 시 자동으로 Trigger로 바뀜).", warnOnly: true);

            var hazards = Object.FindObjectsByType<FootstepHazard>(FindObjectsInactive.Include);
            int solid = hazards.Count(h => !h.GetComponent<BoxCollider>().isTrigger);
            report.Check(hazards.Length > 0, $"위험 물체(FootstepHazard) {hazards.Length}개", "위험 물체가 없습니다.", warnOnly: true);
            report.Check(solid == 0, "위험 물체 Collider = Trigger (쥐를 밀어내지 않음)", $"Trigger가 아닌 위험 물체 {solid}개 → 쥐를 밀 수 있습니다.");

            var walkers = Object.FindObjectsByType<PedestrianPath>(FindObjectsInactive.Include);
            report.Info($"보행자 {walkers.Length}명, 자전거 {Object.FindObjectsByType<BicycleHazard>(FindObjectsInactive.Include).Length}대");

            if (isGame)
            {
                var flow = Object.FindAnyObjectByType<PetitParisGameFlow>();
                if (report.Check(flow != null, "PetitParisGameFlow 있음", "PetitParisGameFlow가 없습니다."))
                {
                    var so = new SerializedObject(flow);
                    foreach (var f in new[] { "goalVolume", "introPanel", "completePanel", "completeText", "objectiveMarker" })
                        report.Check(so.FindProperty(f)?.objectReferenceValue != null, $"GameFlow.{f} 연결", $"GameFlow.{f}가 비어 있습니다.");
                }
                report.Check(walkers.Length == 6, "보행자 6명 (테라스 3명, 빵집 셰프, 빗자루 웨이터, 주방 셰프)", $"보행자가 {walkers.Length}명입니다 (기획: 6명).", warnOnly: true);
                report.Check(Object.FindAnyObjectByType<ChefSwat>() != null, "주방 요리사 주걱 반응(ChefSwat) 있음", "ChefSwat가 없습니다 (메뉴 3번).", warnOnly: true);

                // AI 리뷰 P1 수정 사항 점검
                var flowSo = flow != null ? new SerializedObject(flow) : null;
                report.Check(flowSo != null && flowSo.FindProperty("menuPanel")?.objectReferenceValue != null && flowSo.FindProperty("menuText")?.objectReferenceValue != null,
                    "X/Tab 메뉴(조작법·편안함 모드) 연결 (ISSUE-06)", "GameFlow.menuPanel/menuText가 비어 있습니다 (메뉴 3번으로 다시 생성).", warnOnly: true);
                var waiterA = walkers.FirstOrDefault(w => w.name == "Pedestrian_A_Waiter");
                if (waiterA != null)
                {
                    float delay = new SerializedObject(waiterA).FindProperty("startDelay")?.floatValue ?? 0f;
                    report.Check(delay >= 20f, $"시작 구간: 웨이터 A {delay:0}초 뒤 출발 (ISSUE-07)", $"웨이터 A 시작 지연이 {delay:0}초입니다 (20초 이상 권장).", warnOnly: true);
                }
                foreach (var a in Object.FindObjectsByType<PedestrianAwareness>(FindObjectsInactive.Include))
                {
                    float speed = new SerializedObject(a).FindProperty("chaseSpeed")?.floatValue ?? 0f;
                    report.Check(speed <= 0.45f, $"{a.name} 추격 속도 {speed:0.00} m/s (쥐 질주 0.6보다 느림, ISSUE-01)",
                        $"{a.name} 추격 속도 {speed:0.00} m/s — 쥐 질주로 따돌리기 어렵습니다.", warnOnly: true);
                }
                var plank = GameObject.Find("Plank_0");
                if (plank != null)
                    report.Check(plank.transform.lossyScale.x >= 0.24f, $"주방 널빤지 폭 {plank.transform.lossyScale.x * 100f:0}cm (ISSUE-02)",
                        "주방 널빤지가 25cm보다 좁습니다.", warnOnly: true);
                report.Check(checkpoints != null && checkpoints.Count >= 8, $"체크포인트 {checkpoints?.Count ?? 0}개 (자전거 길 뒤·주방 2개 포함)",
                    "주방 체크포인트가 없습니다 (메뉴 3번으로 다시 생성).", warnOnly: true);
                var rigRoot = Object.FindAnyObjectByType<RatScaleRig>();
                report.Check(rigRoot != null && rigRoot.GetComponent<RatFallRecovery>() != null, "큰 낙하 완충(RatFallRecovery) 있음 (ISSUE-02)",
                    "RatFallRecovery가 리그에 없습니다 (메뉴 3번으로 다시 생성).", warnOnly: true);
                report.Check(rigRoot != null && rigRoot.GetComponent<RatHaptics>() != null, "컨트롤러 진동(RatHaptics) 있음 (ISSUE-15)",
                    "RatHaptics가 리그에 없습니다.", warnOnly: true);
                report.Check(rigRoot != null && rigRoot.GetComponent<RatArmSwing>() != null, "양손 흔들기 달리기(RatArmSwing) 있음",
                    "RatArmSwing이 리그에 없습니다 (실행 중 자동 추가되지만 메뉴 3번으로 다시 생성 권장).", warnOnly: true);
                var amb = Object.FindAnyObjectByType<RatAmbience>();
                report.Check(amb != null && new SerializedObject(amb).FindProperty("cityLoop")?.objectReferenceValue != null,
                    "배경음(거리 소리·음악) 있음 (Q2-04)", "RatAmbience가 없거나 AudioSource가 연결되지 않았습니다 (메뉴 3번).", warnOnly: true);
                report.Check(Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1 && !AudioListener.pause,
                    "AudioListener 1개, 일시 정지 아님", "AudioListener가 1개가 아니거나 AudioListener.pause가 켜져 있습니다.", warnOnly: true);
                // Quest 2 실기기 테스트 (Q2-02, Q2-03)
                var lane = GameObject.Find("Paving_BikeLane");
                var laneMat = lane != null ? lane.GetComponent<MeshRenderer>()?.sharedMaterial : null;
                report.Check(laneMat != null && laneMat.name != "PP_AsphaltTex" && GameObject.Find("Paving_Alley") == null,
                    "자전거 길: 밝은 재질, 골목 포석과 겹치지 않음 (Q2-02)",
                    "자전거 길이 어두운 아스팔트이거나 골목 포석(Paving_Alley)과 겹칩니다 → Quest 2에서 검게 깜빡임 (메뉴 3번으로 다시 생성).", warnOnly: true);
                int broomHides = zones.Count(z => { var c = z.Volume != null ? z.Volume.bounds.center : z.transform.position; return c.x > -1.7f && c.x < 0.7f && c.z > 18.4f && c.z < 23.2f; });
                report.Check(broomHides >= 4, $"빗자루 추격 구간 숨을 곳 {broomHides}곳 (Q2-03)", $"빗자루 추격 구간 숨을 곳이 {broomHides}곳뿐입니다 (4곳 이상 권장).", warnOnly: true);
                var hudCheck = Object.FindAnyObjectByType<RatHud>();
                report.Check(hudCheck != null && new SerializedObject(hudCheck).FindProperty("backdrop")?.objectReferenceValue != null,
                    "HUD 배경은 메시지가 있을 때만 (ISSUE-04)", "RatHud.backdrop이 비어 있습니다.", warnOnly: true);

                // 재미 요소
                var aware = Object.FindObjectsByType<PedestrianAwareness>(FindObjectsInactive.Include);
                report.Check(aware.Length == walkers.Length, $"쥐를 알아채는 보행자 {aware.Length}명", $"PedestrianAwareness가 {aware.Length}개입니다 (보행자 {walkers.Length}명).", warnOnly: true);
                foreach (var a in aware)
                    report.Check(new SerializedObject(a).FindProperty("bubble")?.objectReferenceValue != null, $"{a.name} 말풍선 연결", $"{a.name}의 말풍선(bubble)이 비어 있습니다.", warnOnly: true);
                var crumbs = Object.FindObjectsByType<CrumbPickup>(FindObjectsInactive.Exclude);
                int golden = crumbs.Count(c => c.IsGolden);
                report.Check(crumbs.Length > 0, $"빵 부스러기 {crumbs.Length}개 (총점 {crumbs.Sum(c => c.Points)}, 황금 크루아상 {golden}개)", "빵 부스러기(CrumbPickup)가 없습니다.", warnOnly: true);
                var collection = Object.FindAnyObjectByType<CrumbCollection>();
                if (report.Check(collection != null, "CrumbCollection(점수·별점) 있음", "CrumbCollection이 없습니다."))
                {
                    var cso = new SerializedObject(collection);
                    foreach (var f in new[] { "rig", "danger", "respawn", "checkpoints", "gameFlow", "popup" })
                        report.Check(cso.FindProperty(f)?.objectReferenceValue != null, $"CrumbCollection.{f} 연결", $"CrumbCollection.{f}가 비어 있습니다.", warnOnly: true);
                }
                var hud = Object.FindAnyObjectByType<RatHud>();
                report.Check(hud != null && new SerializedObject(hud).FindProperty("scoreText")?.objectReferenceValue != null, "시야 안내판(RatHud) 연결", "RatHud가 없거나 글자가 연결되지 않았습니다.", warnOnly: true);
                var chase = Object.FindAnyObjectByType<ChaseEvent>();
                report.Check(chase != null && new SerializedObject(chase).FindProperty("chaser")?.objectReferenceValue != null, "빗자루 웨이터 추격 이벤트 연결", "ChaseEvent가 없거나 chaser가 비어 있습니다.", warnOnly: true);
                foreach (var f in Object.FindObjectsByType<FallingHazard>(FindObjectsInactive.Include))
                {
                    var fso = new SerializedObject(f);
                    report.Check(fso.FindProperty("items")?.arraySize > 0 && fso.FindProperty("zone")?.objectReferenceValue != null, $"{f.name}: 떨어질 물건·구역 연결", $"{f.name}의 items 또는 zone이 비어 있습니다.", warnOnly: true);
                }
                report.Check(Object.FindAnyObjectByType<KitchenDoorGate>() != null, "주방 뒷문 게이트(센강 난간 CP5에서 열림) 있음",
                    "KitchenDoorGate가 없습니다 (메뉴 3번으로 다시 생성).", warnOnly: true);
                report.Check(Object.FindAnyObjectByType<SeineBarge>() != null, "센강 유람선 있음", "SeineBarge가 없습니다.", warnOnly: true);
                var seineVideo = GameObject.Find("SeineVideoScreen");
                report.Info(seineVideo != null ? "센강 실사 영상 배경 사용 중 (출처를 docs/CREDITS.md에 적었는지 확인)" : "센강 실사 영상 없음 → 코드 원경 (ThirdParty/Backdrops/SeineVideo.mp4를 넣으면 사용)");
                report.Info($"비둘기 무리 {Object.FindObjectsByType<PigeonFlock>(FindObjectsInactive.Include).Length}개, 이야기 구역 {Object.FindObjectsByType<RatStoryTrigger>(FindObjectsInactive.Include).Length}개");
                var goal = flow != null ? new SerializedObject(flow).FindProperty("goalVolume")?.objectReferenceValue as Collider : null;
                report.Check(goal != null && goal.bounds.min.y > 0.5f, "목적지 = 주방 식탁 위 치즈", "목적지(goalVolume)가 식탁 위(높이 0.9m)가 아닙니다.", warnOnly: true);
                CheckGoalReachableAtWheel(report, goal);
                var completePanel = flow != null ? new SerializedObject(flow).FindProperty("completePanel")?.objectReferenceValue as Transform : null;
                if (completePanel != null)
                {
                    var renderers = completePanel.GetComponentsInChildren<Renderer>(true);
                    bool allOverlay = renderers.Length > 0 && renderers.All(r => r.sharedMaterial != null && r.sharedMaterial.renderQueue >= (int)RenderQueue.Overlay);
                    report.Check(allOverlay, "완료 패널 = 맨 위에 그림 (치즈 바퀴에 가려지지 않음)",
                        "완료 패널이 일반 깊이 검사로 그려집니다. 치즈 바퀴에 코를 대고 도착하면 패널이 바퀴 속에 가려집니다 (메뉴 3번으로 다시 생성).");
                }

                var guide = Object.FindAnyObjectByType<RatGuideArrow>();
                if (report.Check(guide != null, "내비게이션 화살표(RatGuideArrow) 있음", "RatGuideArrow가 없습니다.", warnOnly: true))
                {
                    var gso = new SerializedObject(guide);
                    var route = gso.FindProperty("route");
                    report.Check(route != null && route.arraySize >= 2, $"안내 경로 점 {route?.arraySize ?? 0}개", "안내 경로(route)가 2점 미만입니다.");
                    report.Check(gso.FindProperty("arrow")?.objectReferenceValue != null, "화살표 오브젝트 연결", "RatGuideArrow.arrow가 비어 있습니다.");
                }
                report.Check(RenderSettings.skybox != null && RenderSettings.fog, "해 질 녘 하늘·안개 설정", "Skybox 또는 안개가 꺼져 있습니다 (메뉴 3번으로 다시 생성).", warnOnly: true);
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
            report.Check(Version("com.unity.xr.interaction.toolkit") == "3.6.1", "XRI 3.6.1 (프로젝트 기준 버전)", $"XRI 버전이 {Version("com.unity.xr.interaction.toolkit")}입니다. 프로젝트 기준 버전은 3.6.1입니다.", warnOnly: true);
            report.Check(Object.FindObjectsByType<RatLocomotionController>(FindObjectsInactive.Include).Length <= 1, "이동 컨트롤러 중복 없음", "RatLocomotionController가 여러 개입니다.");

            report.Check(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android, "빌드 타깃 Android",
                $"빌드 타깃 {EditorUserBuildSettings.activeBuildTarget}. Build Profiles에서 Android로 Switch Platform 하세요.", warnOnly: true);
            report.Check((int)PlayerSettings.Android.minSdkVersion >= PetitParisProjectSetup.MinAndroidApi,
                $"Android Min API {(int)PlayerSettings.Android.minSdkVersion}", $"Android Min API가 {PetitParisProjectSetup.MinAndroidApi} 미만 (메뉴 0번).", warnOnly: true);
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            report.Check(apis.Length > 0 && apis[0] == GraphicsDeviceType.Vulkan, "Android Graphics API = Vulkan", "Android Graphics API 첫 번째가 Vulkan이 아닙니다 (메뉴 0번).", warnOnly: true);
            var buildScenes = EditorBuildSettings.scenes;
            report.Check(buildScenes.Length > 0 && buildScenes[0].path == PetitParisPaths.Scene && buildScenes[0].enabled,
                "Build 씬 목록 첫 번째 = PetitParisVR", "PetitParisVR이 Build 씬 목록 맨 앞에 없습니다 (메뉴 3번).", warnOnly: true);
            CheckQuestBuild(report);
        }

        // ───────── Quest 2 Android 빌드 준비 (AI 리뷰 ISSUE-20/21) ─────────
        // OpenXR·XR Management 패키지는 이 레포의 manifest에 없다 (설치하면 생긴다). 패키지 타입을 직접 참조하면
        // 미설치 상태에서 컴파일이 깨지므로, 설정 파일(Assets/XR/…)의 내용을 글자로 읽어 점검한다.

        const string XrGeneralSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        const string OpenXrSettingsPath = "Assets/XR/Settings/OpenXR Package Settings.asset";

        static void CheckQuestBuild(Report report)
        {
            report.Info("── Quest 2 빌드 준비 ──");
            bool hasGeneral = System.IO.File.Exists(XrGeneralSettingsPath);
            report.Check(hasGeneral, "XR Plug-in Management 설정 파일 있음",
                "XR Plug-in Management 설정이 없습니다: Edit > Project Settings > XR Plug-in Management를 열고 Install, Android 탭에서 OpenXR 체크.");
            if (hasGeneral)
            {
                string text = System.IO.File.ReadAllText(XrGeneralSettingsPath);
                report.Check(text.Contains("OpenXRLoader"), "Android XR 로더 = OpenXR",
                    "XR 로더 설정에 OpenXR이 없습니다: XR Plug-in Management > Android 탭에서 OpenXR을 체크하세요.");
            }

            bool hasOpenXr = System.IO.File.Exists(OpenXrSettingsPath);
            report.Check(hasOpenXr, "OpenXR 설정 파일 있음", "OpenXR 설정 파일이 없습니다 (OpenXR 패키지 설치 후 XR Plug-in Management > OpenXR을 한 번 열면 생깁니다).");
            if (hasOpenXr)
            {
                string text = System.IO.File.ReadAllText(OpenXrSettingsPath);
                report.Check(FeatureEnabled(text, "OculusTouchControllerProfile Android"), "Android: Oculus Touch Controller Profile 켜짐 (Quest 2 컨트롤러)",
                    "Android OpenXR > Enabled Interaction Profiles에 Oculus Touch Controller Profile이 없습니다. Quest 2 Touch 컨트롤러는 이 프로필이 필요합니다 (Touch Plus는 Quest 3용).");
                report.Check(FeatureEnabled(text, "MetaQuestFeature Android"), "Android: Meta Quest Support 켜짐",
                    "Android OpenXR > OpenXR Feature Groups에서 Meta Quest Support를 체크하세요.");
                if (text.Contains("m_Name: MetaQuestFeature Android"))
                    report.Check(text.Contains("quest2") || text.Contains("Quest2") || text.Contains("targetDevices"),
                        "Meta Quest Support 대상 기기 설정 있음", "Meta Quest Support의 Target Devices에서 Quest 2를 체크했는지 확인하세요.", warnOnly: true);
            }

            report.Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "Android ARM64",
                "Android Target Architecture가 ARM64만이 아닙니다 (메뉴 0번).", warnOnly: true);
            report.Check(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
                "Android IL2CPP", "Android Scripting Backend가 IL2CPP가 아닙니다 (메뉴 0번, ARM64에 필요).", warnOnly: true);
            report.Check(PlayerSettings.colorSpace == ColorSpace.Linear, "Color Space Linear", "Color Space가 Linear가 아닙니다 (메뉴 0번).", warnOnly: true);

            // 비네트 프리팹: 샘플 버전 폴더와 상관없이 찾는다
            string vignette = PetitParisBuildKit.FindVignettePrefabPath();
            report.Check(vignette != null, $"Comfort Vignette 프리팹: {vignette}",
                "TunnelingVignette 프리팹이 없습니다: Package Manager > XR Interaction Toolkit > Samples > Starter Assets를 Import 하세요.", warnOnly: true);

            // Android 품질 단계가 쓰는 URP 에셋 (Quest 2 기준 값)
            var urp = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PetitParisProjectSetup.MobileUrpAssetPath);
            if (urp != null)
            {
                var so = new SerializedObject(urp);
                bool hdr = so.FindProperty("m_SupportsHDR")?.boolValue ?? true;
                int msaa = so.FindProperty("m_MSAA")?.intValue ?? 1;
                float scale = so.FindProperty("m_RenderScale")?.floatValue ?? 0f;
                float shadow = so.FindProperty("m_ShadowDistance")?.floatValue ?? 999f;
                report.Check(!hdr && msaa == 4 && Mathf.Approximately(scale, 1f) && shadow <= 15f,
                    $"Mobile URP (Quest 2): HDR 끔, MSAA {msaa}x, Render Scale {scale:0.##}, 그림자 {shadow:0}m",
                    $"Mobile URP 값이 Quest 2 권장과 다릅니다 (HDR {(hdr ? "켬" : "끔")}, MSAA {msaa}x, Scale {scale:0.##}, 그림자 {shadow:0}m). 메뉴 5번.", warnOnly: true);
            }

            var comfort = AssetDatabase.LoadAssetAtPath<RatComfortSettings>(PetitParisPaths.Comfort);
            if (comfort != null)
                report.Check(comfort.vignetteInAir && comfort.vignetteOnRun && comfort.settingsVersion >= RatComfortSettings.CurrentVersion,
                    "편안함: 공중·달리기 비네트 켜짐 (설정 v" + comfort.settingsVersion + ")",
                    "RatComfortSettings가 이전 값입니다 (공중/달리기 비네트 꺼짐). 메뉴 3번으로 씬을 다시 만들면 옮겨집니다.", warnOnly: true);
        }

        /// <summary>OpenXR 설정 YAML에서 m_Name이 name인 블록의 m_enabled가 1인지.</summary>
        static bool FeatureEnabled(string yaml, string name)
        {
            int i = yaml.IndexOf("m_Name: " + name, System.StringComparison.Ordinal);
            if (i < 0) return false;
            int end = yaml.IndexOf("--- !u!", i, System.StringComparison.Ordinal);
            string block = end > 0 ? yaml.Substring(i, end - i) : yaml.Substring(i);
            return block.Contains("m_enabled: 1");
        }

        /// <summary>
        /// 시작 지점이 좁은 틈에 갇혀 있지 않은지 본다. 눈높이에서 수평 8방향으로 레이를 쏴 0.5m 넘게 트인 방향을 센다.
        /// 틈 안에서 시작하면 고개를 돌릴 때 화면 전체가 벽 한 색으로 덮인다 ("갈색 화면" 문제, 2026-11-14).
        /// </summary>
        static void CheckSpawnOpenness(Report report, Vector3 eye)
        {
            const float clearDistance = 0.5f;
            const int minOpenDirections = 3;
            Physics.SyncTransforms();
            int mask = ~(1 << PetitParisBuildKit.PlayerLayer);
            int open = 0;
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                if (!Physics.Raycast(eye, dir, clearDistance, mask, QueryTriggerInteraction.Ignore)) open++;
            }
            report.Check(open >= minOpenDirections, $"시작 지점 시야 트임: 8방향 중 {open}방향이 {clearDistance}m 이상 트임",
                $"시작 지점 {eye} 주변 8방향 중 {open}방향만 {clearDistance}m 이상 트여 있습니다. 좁은 틈 안에서 시작하면 돌아볼 때 화면이 벽으로 덮입니다.");
        }

        /// <summary>
        /// 식탁 위 치즈 바퀴에는 충돌체가 있어 쥐가 바퀴 가장자리에서 멈춘다.
        /// 동서남북 어느 쪽에서 바퀴에 닿아도 쥐 몸 중심이 도착 구역 안이어야 한다 (2026-11-14: 서쪽에서 닿으면 구역 밖이라 도착이 안 됐음).
        /// </summary>
        static void CheckGoalReachableAtWheel(Report report, Collider goal)
        {
            var wheelGO = GameObject.Find("GoalCheeseWheel");
            var wheel = wheelGO != null ? wheelGO.GetComponent<Collider>() : null;
            var rig = Object.FindAnyObjectByType<RatScaleRig>();
            if (goal == null || wheel == null || rig == null || !rig.IsValid)
            {
                report.Check(false, "", "치즈 바퀴(GoalCheeseWheel) 충돌체 또는 도착 구역을 찾지 못해 도착 가능 여부를 점검하지 못했습니다.", warnOnly: true);
                return;
            }
            Bounds w = wheel.bounds;
            float ratRadius = rig.Config.bodyRadius;
            float bodyY = w.min.y + rig.Config.bodyHeight * 0.5f;
            int inside = 0;
            var dirs = new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (var d in dirs)
            {
                float reach = Mathf.Abs(Vector3.Dot(w.extents, d)) + ratRadius + 0.005f; // 바퀴 가장자리에 닿은 쥐 몸 중심
                Vector3 p = new Vector3(w.center.x, bodyY, w.center.z) + d * reach;
                if (goal.bounds.Contains(p)) inside++;
            }
            report.Check(inside == dirs.Length, "치즈 바퀴에 어느 방향에서 닿아도 도착 구역 안",
                $"치즈 바퀴에 닿은 위치 4방향 중 {inside}방향만 도착 구역 안입니다. 바퀴에 막혀 도착하지 못할 수 있습니다.");
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
                if (!PetitParisBuildKit.AssumeYes) EditorUtility.DisplayDialog("PETIT PARIS 검증", $"FAIL {fails}개, WARN {warns}개\n자세한 내용은 Console을 확인하세요.", "확인");
            }
        }
    }
}
