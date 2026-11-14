using TMPro;
using UnityEditor;
using UnityEngine;
using static PetitParis.EditorTools.PetitParisBuildKit;
using static PetitParis.EditorTools.PetitParisParisKit;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 실제 플레이용 게임 씬 PetitParisVR.unity (해 질 녘 파리 거리. 외부 모델 없이 코드 + 자체 생성 질감).
    ///
    /// 동선 (월드 미터, +Z가 북쪽, 경로 길이 약 25m ≈ 걷기 3~4분)
    ///   시작   카페 벽 틈 입구 바로 앞 (-2.85, 0, -0.4) → 동쪽을 보고 시작, 틈 안과 입구 앞은 안전 구역
    ///          (틈 안에서 시작하면 돌아볼 때 화면 전체가 틈 벽으로 덮인다 — "갈색 화면" 문제)
    ///   공간1  카페 테라스 z 0~7      : 테이블 3세트(의자 아래 안전), 보행자 A가 테라스를 한 바퀴씩 돈다
    ///   공간2  빵집 앞 보도 z 7~13    : 보행자 B가 지그재그 왕복, 벤치 아래 안전 구역
    ///   공간3  좁은 골목 z 13~23      : 중간을 자전거가 가로질러 왕복 (z 16.4~17.6), 북쪽에서 빗자루 웨이터 추격
    ///   공간4  센강 강변 z 23~26      : 센강과 에펠탑, 비둘기 떼
    ///   공간5  비스트로 주방 (x -7.4~-1.9, z 17.8~22.8): 뒷문으로 들어가 널빤지 → 상자 → 식탁 위 치즈 (목적지)
    /// 체크포인트: 시작 → 테라스 끝 의자 아래 → 빵집 벤치 아래 → 자전거 길 앞 벤치 아래 → 강변(주방 뒷문 앞)
    ///
    /// 재미 요소
    /// - 빵 부스러기(1)·크루아상(3)·치즈(5)·황금 크루아상(10, 3개). 위험한 곳일수록 큰 조각. 체크포인트 전에 밟히면 떨어뜨림
    /// - 사람이 쥐를 보면 "!" → "앗, 쥐다!" → 쿵쿵 밟으며 쫓아옴 (웅크리면 덜 들킴, 숨으면 놓침)
    /// - 빵집 앞: 위에서 바게트가 떨어짐 (그림자 예고, 떨어진 자리에 보너스 부스러기)
    /// - 골목: 빗자루 웨이터 추격 이벤트 + 긴장 음악. 팔레트·상자 밑에 숨기
    /// - 강변: 비둘기 떼가 날아오름 / 주방: 요리사가 오가는 바닥 위 널빤지 오르기
    /// </summary>
    public static class PetitParisLevelBuilder
    {
        // 벽면(x = -3.0)에서 15cm 앞. 틈(x -3.4~-3.0) 안이 아니라 입구 바로 바깥이다.
        static readonly Vector3 Spawn = new Vector3(-2.85f, 0f, -0.4f);
        const float SpawnYaw = 90f; // 동쪽(+X)을 보고 시작
        /// <summary>목적지: 주방 식탁 위 치즈 바퀴 옆 (식탁 윗면 높이 0.9m).</summary>
        static readonly Vector3 CheeseGoal = new Vector3(-4.45f, 0.9f, 20.3f);
        /// <summary>식탁 위 큰 치즈 바퀴(충돌체 있음)의 바닥 중심. 도착 구역도 이 점을 중심으로 둔다.</summary>
        static readonly Vector3 GoalWheelCenter = CheeseGoal + new Vector3(-0.25f, 0f, 0f);
        const float KitchenFloorTop = 0.003f;

        [MenuItem("PetitParis/3. Build Game Scene (PetitParisVR)", priority = 3)]
        public static void BuildGame()
        {
            if (!PrepareNewScene(PetitParisPaths.Scene, out var scene)) return;
            var m = new MaterialLibrary();
            var font = KoreanFont();
            PetitParisTextures.ClearCache();
            var p = Palette.Create(m);

            SetupDusk(m);

            var env = new GameObject("Environment_Paris").transform;
            BuildGround(env, p);
            BuildCafe(env, p, m, font);
            BuildBakery(env, p, font);
            BuildEastBuildings(env, p, font);
            BuildAlleyAndCrossStreet(env, p);
            BuildQuaiAndSeine(env, p);
            BuildKitchen(env, p, m, font);
            BuildProps(env, m);
            BuildParisProps(env, p);
            MarkStatic(env);

            // ── 움직이는 위험 ──
            var npcs = Group("Pedestrians", null);
            var waiterA = Pedestrian("Pedestrian_A_Waiter", npcs,
                new[] { new Vector3(-2.5f, 0f, 0.2f), new Vector3(-2.5f, 0f, 6.9f), new Vector3(0.25f, 0f, 6.9f), new Vector3(0.25f, 0f, 0.2f) },
                PedestrianPath.LoopMode.Loop, 0.9f, 3f, new Color(0.25f, 0.3f, 0.45f), m, PedestrianStyle.Waiter);
            var chefB = Pedestrian("Pedestrian_B_Chef", npcs,
                new[] { new Vector3(-2.2f, 0f, 7.6f), new Vector3(0.3f, 0f, 8.7f), new Vector3(-2.2f, 0f, 9.8f),
                        new Vector3(0.3f, 0f, 10.9f), new Vector3(-2.2f, 0f, 12.0f), new Vector3(0.2f, 0f, 12.9f) },
                PedestrianPath.LoopMode.PingPong, 0.8f, 0f, new Color(0.55f, 0.32f, 0.25f), m, PedestrianStyle.Chef);
            Bicycle("Bicycle_CrossStreet", Group("Vehicles", null), new Vector3(-7.0f, 0f, 17.0f), new Vector3(7.0f, 0f, 17.0f), m);

            // 빗자루 웨이터 (골목 북쪽에서 천천히 쓸다가, 쥐가 오면 추격)
            var porter = Pedestrian("Pedestrian_D_BroomWaiter", npcs,
                new[] { new Vector3(0.15f, 0f, 21.7f), new Vector3(-1.05f, 0f, 22.1f) },
                PedestrianPath.LoopMode.PingPong, 0.35f, 0f, new Color(0.25f, 0.3f, 0.45f), m, PedestrianStyle.Waiter);
            SetPathFloat(porter, "pauseAtWaypoint", 2.0f);
            var broom = PetitParisGameplayKit.Broom(porter, m);
            var porterAware = PetitParisGameplayKit.Awareness(porter, PetitParisText.ShoutWaiter, 0.9f, 0.6f, font, m,
                new Bounds(new Vector3(-0.5f, 0f, 20.75f), new Vector3(2.2f, 20f, 5.7f)));

            // 주방 요리사 (뒷문 앞 통로를 오간다)
            var kitchenChef = Pedestrian("Pedestrian_C_KitchenChef", npcs,
                new[] { new Vector3(-2.3f, 0f, 22.2f), new Vector3(-6.8f, 0f, 22.2f) },
                PedestrianPath.LoopMode.PingPong, 0.75f, 1.0f, new Color(0.55f, 0.32f, 0.25f), m, PedestrianStyle.Chef);
            SetPathFloat(kitchenChef, "pauseAtWaypoint", 1.2f);

            // 사람들이 쥐를 알아챈다
            PetitParisGameplayKit.Awareness(waiterA, PetitParisText.ShoutRat, 1.0f, 0.55f, font, m);
            PetitParisGameplayKit.Awareness(chefB, PetitParisText.ShoutRat, 1.0f, 0.55f, font, m);
            PetitParisGameplayKit.Awareness(kitchenChef, PetitParisText.ShoutChef, 1.1f, 0.6f, font, m,
                new Bounds(new Vector3(-4.55f, 0f, 22.2f), new Vector3(4.9f, 20f, 0.8f)));

            // ── 게임 진행 ──
            var gameplay = Group("Gameplay", null);
            var cps = Group("Checkpoints", gameplay);
            var flags = new Renderer[5];
            flags[0] = Checkpoint("CP0_Start_WallCrack", cps, Spawn, SpawnYaw, m);
            flags[1] = Checkpoint("CP1_Terrace_UnderChair", cps, new Vector3(-1.5f, 0f, 6.08f), 0f, m);
            flags[2] = Checkpoint("CP2_Bakery_UnderBench", cps, new Vector3(-2.7f, 0f, 10.0f), 0f, m);
            flags[3] = Checkpoint("CP3_Alley_UnderBench", cps, new Vector3(-1.35f, 0f, 15.0f), 0f, m);
            flags[4] = Checkpoint("CP4_Quai_KitchenDoor", cps, new Vector3(-1.9f, 0f, 23.9f), 270f, m);
            var manager = cps.gameObject.AddComponent<CheckpointManager>();
            var chime = cps.gameObject.AddComponent<AudioSource>();
            var mso = new SerializedObject(manager);
            SetObjectArray(mso, "checkpoints", new Object[] { cps.GetChild(0), cps.GetChild(1), cps.GetChild(2), cps.GetChild(3), cps.GetChild(4) });
            SetObjectArray(mso, "markers", flags);
            SetObject(mso, "chimeSource", chime);
            mso.ApplyModifiedPropertiesWithoutUndo();

            // 목적지: 주방 식탁 위 치즈 (쥐 몸 중심이 식탁 윗면 위 치즈 주변에 들어오면 완료)
            // 치즈 바퀴에는 충돌체가 있어 쥐가 바퀴 가장자리에서 멈춘다 → 구역을 바퀴 중심에 두고, 어느 방향에서 닿아도 안에 들어오게 넓힌다
            var goal = new GameObject("Goal_KitchenCheese");
            goal.transform.SetParent(gameplay, false);
            goal.transform.position = GoalWheelCenter + new Vector3(0f, 0.1f, 0f);
            var goalBox = goal.AddComponent<BoxCollider>();
            goalBox.isTrigger = true;
            goalBox.size = new Vector3(1.0f, 0.2f, 1.0f);
            var goalPoint = Group("GoalPoint", gameplay);
            goalPoint.position = CheeseGoal;

            // 목표 표시 (작은 금색 마름모)
            var marker = Group("ObjectiveMarker", gameplay);
            var diamond = RemoveCollider(Box("Diamond", marker, Vector3.zero, Vector3.one * 0.03f, m.Unlit("PP_Objective", new Color(1f, 0.82f, 0.3f))));
            diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var objective = marker.gameObject.AddComponent<RatObjectiveMarker>();

            // 안내 표지 (가까이 가면 나타남)
            var hints = Group("Hints", gameplay);
            Hint("Hint_Footsteps", hints, new Vector3(-2.65f, 0.2f, 0.75f), PetitParisText.HintFootsteps, font);
            Hint("Hint_Hide", hints, new Vector3(-1.5f, 0.2f, 0.7f), PetitParisText.HintHide, font);
            Hint("Hint_Bakery", hints, new Vector3(-2.0f, 0.2f, 7.45f), PetitParisText.HintBakery, font);
            Hint("Hint_Bike", hints, new Vector3(-0.5f, 0.2f, 15.95f), PetitParisText.HintBike, font);
            var goalHint = Hint("Hint_Seine", hints, new Vector3(-0.5f, 0.45f, 24.9f), PetitParisText.HintSeine, font);
            var gso = new SerializedObject(goalHint);
            SetFloat(gso, "fullDistance", 2.5f);
            SetFloat(gso, "fadeDistance", 5f);
            gso.ApplyModifiedPropertiesWithoutUndo();
            Hint("Hint_Crumbs", hints, new Vector3(-2.2f, 0.2f, 0.2f), PetitParisText.HintCrumbs, font);
            Hint("Hint_Stealth", hints, new Vector3(-1.0f, 0.2f, 1.3f), PetitParisText.HintCrouchStealth, font);
            Hint("Hint_Pallet", hints, new Vector3(-1.0f, 0.2f, 18.9f), PetitParisText.HintPallet, font);
            Hint("Hint_KitchenDoor", hints, new Vector3(-2.95f, 0.3f, 23.45f), PetitParisText.HintKitchenDoor, font);
            Hint("Hint_KitchenClimb", hints, new Vector3(-2.2f, 0.25f, 22.25f), PetitParisText.HintKitchenClimb, font);

            // 이야기 구역 (처음 들어갈 때 한 줄 메시지)
            var story = Group("StoryTriggers", gameplay);
            var storySfx = story.gameObject.AddComponent<AudioSource>();
            StoryTrigger("Story_Seine", story, new Vector3(-0.5f, 0.2f, 23.7f), new Vector3(3f, 0.4f, 0.8f), PetitParisText.MessageSeine, storySfx);
            StoryTrigger("Story_Kitchen", story, new Vector3(-2.7f, 0.2f, 22.4f), new Vector3(1.4f, 0.4f, 0.8f), PetitParisText.MessageKitchen, storySfx);

            // ── 재미 요소 ──
            var crumbsRoot = Group("Crumbs", gameplay);
            PlaceCrumbs(crumbsRoot, m);
            var events = Group("Events", gameplay);
            PetitParisGameplayKit.FallingBaguettes("FallingBaguettes_Bakery", events, -2.95f, 0.7f, 7.4f, 12.9f, 2.6f, 3, m);
            PetitParisGameplayKit.Pigeons("Pigeons_Quai_W", events, new Vector3(-1.9f, 0f, 24.6f), 5, 0.45f, m, 3);
            PetitParisGameplayKit.Pigeons("Pigeons_Quai_E", events, new Vector3(2.4f, 0f, 24.5f), 6, 0.6f, m, 5);
            PetitParisGameplayKit.Pigeons("Pigeons_Road", events, new Vector3(2.4f, 0f, 3.2f), 4, 0.5f, m, 7);

            var chaseGO = new GameObject("ChaseEvent_BroomWaiter");
            chaseGO.transform.SetParent(events, false);
            chaseGO.transform.position = new Vector3(-0.5f, 0.2f, 18.6f);
            var chaseBox = chaseGO.AddComponent<BoxCollider>();
            chaseBox.isTrigger = true;
            chaseBox.size = new Vector3(2.4f, 0.4f, 0.6f);
            var chase = chaseGO.AddComponent<ChaseEvent>();
            Wire(chase, ("trigger", chaseBox), ("chaser", porterAware), ("music", chaseGO.AddComponent<AudioSource>()), ("broom", broom));

            var collectionGO = new GameObject("CrumbCollection");
            collectionGO.transform.SetParent(gameplay, false);
            var collection = collectionGO.AddComponent<CrumbCollection>();
            var popup = Label("CrumbPopup", collectionGO.transform, Vector3.zero, "+1", 0.35f, 0.4f, Color.white, font, null);
            popup.fontStyle = FontStyles.Bold;

            // 시야 아래쪽 안내판 (점수 + 메시지). PC 모드에서도 보인다
            var hudGO = new GameObject("RatHud");
            hudGO.transform.SetParent(gameplay, false);
            var hud = hudGO.AddComponent<RatHud>();
            var hudScore = Label("Score", hudGO.transform, new Vector3(0f, -0.012f, 0f), "", 0.13f, 0.3f, new Color(1f, 0.92f, 0.7f), font, null);
            var hudMessage = Label("Message", hudGO.transform, new Vector3(0f, 0.02f, 0f), "", 0.12f, 0.34f, new Color(1f, 0.85f, 0.45f), font, null);
            hudScore.fontStyle = FontStyles.Bold;
            hudMessage.fontStyle = FontStyles.Bold;
            // 글자 뒤 부드러운 어두운 배경 (밝은 하늘·벽 위에서도 읽히게)
            var hudBack = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Quad));
            hudBack.name = "Backdrop";
            hudBack.transform.SetParent(hudGO.transform, false);
            hudBack.transform.localPosition = new Vector3(0f, 0.004f, 0.004f);
            hudBack.transform.localScale = new Vector3(0.42f, 0.085f, 1f);
            var hudBackR = hudBack.GetComponent<MeshRenderer>();
            hudBackR.sharedMaterial = m.Blob("PP_HudBackdrop", new Color(0.05f, 0.04f, 0.06f, 0.55f));
            hudBackR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hudBackR.receiveShadows = false;

            // 조작 안내 패널 (시작 지점 53cm 앞, 동쪽을 바라보는 쥐가 읽을 수 있게). Spawn을 옮기면 같이 따라간다.
            var (intro, introText) = Panel("IntroPanel", gameplay, Spawn + new Vector3(0.53f, 0.25f, 0f), Quaternion.LookRotation(Vector3.right),
                new Vector2(0.38f, 0.27f), "<b>PETIT PARIS</b>\n" + PetitParisText.IntroBody, 0.12f, font, m);
            // 완료 패널 (도착하면 카메라 앞에 나타남)
            var (complete, completeText) = Panel("CompletePanel", gameplay, goalPoint.position + Vector3.up * 0.15f, Quaternion.identity,
                new Vector2(0.34f, 0.24f), PetitParisText.CompleteTitle, 0.12f, font, m);
            MakeOverlay(complete, m); // 치즈 바퀴 등에 가려지지 않게
            complete.SetActive(false);

            // 내비게이션 화살표 경로 (시작 → 의자 옆 안전한 길 → 체크포인트들 → 출구)
            var route = Group("GuideRoute", gameplay);
            Vector3[] routePoints =
            {
                new Vector3(-2.75f, 0f, -0.4f), new Vector3(-2.2f, 0f, 0.55f), new Vector3(-1.15f, 0f, 1.0f), new Vector3(-1.15f, 0f, 5.9f),
                new Vector3(-1.5f, 0f, 6.08f), new Vector3(-1.15f, 0f, 6.6f), new Vector3(-1.3f, 0f, 7.6f), new Vector3(-2.45f, 0f, 9.4f),
                new Vector3(-2.7f, 0f, 10.0f), new Vector3(-2.4f, 0f, 11.0f), new Vector3(-1.2f, 0f, 12.6f), new Vector3(-0.5f, 0f, 13.8f),
                new Vector3(-1.1f, 0f, 14.6f), new Vector3(-1.35f, 0f, 15.0f), new Vector3(-0.5f, 0f, 15.8f), new Vector3(-0.5f, 0f, 18.2f),
                new Vector3(-0.5f, 0f, 21.0f), new Vector3(-0.5f, 0f, 23.5f), new Vector3(-1.9f, 0f, 23.9f), new Vector3(-2.95f, 0f, 23.5f),
                new Vector3(-2.95f, 0f, 22.5f), new Vector3(-2.2f, 0f, 22.0f), new Vector3(-2.15f, 0f, 21.5f), new Vector3(-5.55f, 0f, 21.5f),
                new Vector3(-5.5f, 0f, 21.0f), CheeseGoal,
            };
            var routeNodes = new Transform[routePoints.Length];
            for (int i = 0; i < routePoints.Length; i++)
            {
                routeNodes[i] = Group($"R{i:00}", route);
                routeNodes[i].position = routePoints[i];
            }
            var guideGO = new GameObject("GuideArrow");
            guideGO.transform.SetParent(gameplay, false);
            var arrowVisual = GuideArrowVisual(guideGO.transform, m.Unlit("PP_GuideArrow", new Color(1f, 0.84f, 0.32f)));
            arrowVisual.transform.localScale = Vector3.one * 1.4f; // 체감 약 1.1m 길이
            var guide = guideGO.AddComponent<RatGuideArrow>();

            // 쥐 속도: 사용자 요청으로 더 빠르게 (걷기 체감 2m/s, 질주 체감 5.5m/s). 이미 있는 설정 에셋도 올려 준다
            var speedCfg = GetOrCreateAsset<RatScaleConfig>(PetitParisPaths.Config);
            if (speedCfg.walkSpeed < 0.2f || speedCfg.dashSpeed < 0.55f)
            {
                speedCfg.walkSpeed = Mathf.Max(speedCfg.walkSpeed, 0.2f);
                speedCfg.dashSpeed = Mathf.Max(speedCfg.dashSpeed, 0.55f);
                EditorUtility.SetDirty(speedCfg);
            }

            // ── 리그 ──
            var rig = BuildRig(Spawn, Quaternion.Euler(0f, SpawnYaw, 0f), false, m);
            if (rig == null) return;
            rig.camera.clearFlags = CameraClearFlags.Skybox; // 해 질 녘 하늘
            PrefabUtility.RecordPrefabInstancePropertyModifications(rig.camera);

            var flowGO = new GameObject("GameFlow");
            flowGO.transform.SetParent(gameplay, false);
            var flow = flowGO.AddComponent<PetitParisGameFlow>();
            var flowSfx = flowGO.AddComponent<AudioSource>();
            Wire(flow, ("rig", rig.scaleRig), ("input", rig.reader), ("locomotion", rig.locomotion), ("respawn", rig.respawn),
                ("danger", rig.danger), ("checkpoints", manager), ("fader", rig.fader), ("objectiveMarker", objective), ("sfx", flowSfx),
                ("goalVolume", goalBox), ("goalMarkerTarget", goalPoint), ("introPanel", intro), ("introText", introText),
                ("completePanel", complete.transform), ("completeText", completeText));
            Wire(manager, ("rig", rig.scaleRig));
            Wire(objective, ("visual", diamond.transform), ("rig", rig.scaleRig));
            Wire(rig.respawn, ("checkpoints", manager));
            Wire(rig.statusPanel, ("gameFlow", flow), ("checkpoints", manager), ("crumbs", collection));
            Wire(flow, ("crumbs", collection));
            var collectionSfx = collectionGO.AddComponent<AudioSource>();
            Wire(collection, ("rig", rig.scaleRig), ("danger", rig.danger), ("respawn", rig.respawn), ("checkpoints", manager),
                ("gameFlow", flow), ("sfx", collectionSfx), ("popup", popup));
            Wire(hud, ("rig", rig.scaleRig), ("gameFlow", flow), ("crumbs", collection), ("scoreText", hudScore), ("messageText", hudMessage));
            Wire(guide, ("rig", rig.scaleRig), ("respawn", rig.respawn), ("gameFlow", flow), ("arrow", arrowVisual.transform));
            var gsoArrow = new SerializedObject(guide);
            SetObjectArray(gsoArrow, "route", routeNodes);
            gsoArrow.ApplyModifiedPropertiesWithoutUndo();

            if (!SaveScene(scene, PetitParisPaths.Scene, true)) return;
            Selection.activeGameObject = rig.root;
            Debug.Log($"[PetitParis] 게임 씬을 만들었습니다: {PetitParisPaths.Scene} (빌드 목록 맨 앞). 다음: PetitParis > 4. Validate Scene");
        }

        // ───────────────────────── 구역별 파리 거리 (게임 동선은 그대로, 외형만 파리풍) ─────────────────────────

        static void BuildGround(Transform env, Palette p)
        {
            var g = Group("Ground", env);
            TexBoxXZ("Ground_Cobblestone", g, -8f, 8f, -2f, 25.9f, -0.1f, 0f, p.cobble, 0.9f);
            Paving("Paving_Terrace", g, -3f, 0.8f, -1f, 7f, p.slabsWarm, 1.2f);
            Paving("Paving_Bakery", g, -3f, 0.8f, 7f, 13.3f, p.slabs, 1.2f);
            Paving("Paving_Road", g, 0.8f, 4f, -1f, 13.3f, p.cobbleDark, 0.9f);
            Paving("Paving_Alley", g, -1.7f, 0.7f, 13.3f, 23f, p.cobble, 0.8f);
            Paving("Paving_BikeLane", g, -7.6f, 7.6f, 16.4f, 17.6f, p.asphalt, 2f);
            Paving("Paving_Quai", g, -4f, 4.4f, 23f, 25.9f, p.slabsWarm, 1.5f);
            // 보도 경계석 (높이 1.5cm, 걸어서 넘을 수 있음)
            Paving("Curb_Terrace", g, 0.78f, 0.9f, -1f, 13.3f, p.trim, 0.6f, 0.015f);
        }

        static void Paving(string name, Transform parent, float x0, float x1, float z0, float z1, Material mat, float tile, float height = 0.003f)
        {
            TexBoxXZ(name, parent, x0, x1, z0, z1, 0f, height, mat, tile, height >= 0.01f);
        }

        static void BuildCafe(Transform env, Palette p, MaterialLibrary m, TMP_FontAsset font)
        {
            var cafe = Group("Cafe", env);
            // 1층 + 벽 틈 (너비 30cm, 높이 25cm, 깊이 40cm — 쥐 지름 약 8cm, 눈높이 12cm)
            TexBoxXZ("Ground_South", cafe, -3.4f, -3.0f, -1.0f, -0.55f, 0f, 4f, p.rustic, 1.2f);
            TexBoxXZ("Ground_CrackLintel", cafe, -3.4f, -3.0f, -0.55f, -0.25f, 0.25f, 4f, p.rustic, 1.2f);
            TexBoxXZ("Ground_Main", cafe, -3.4f, -3.0f, -0.25f, 7.0f, 0f, 4f, p.rustic, 1.2f);
            TexBoxXZ("Ground_Mass", cafe, -9f, -3.4f, -1.0f, 7.0f, 0f, 4f, p.rustic, 1.2f);
            TexBoxXZ("Crack_Back", cafe, -3.42f, -3.4f, -0.6f, -0.2f, 0f, 0.3f, p.darkBack, 1f, false);
            // 틈 안(x -3.4~-3.0) + 시작 지점이 있는 입구 앞(~-2.75)까지
            SafeZone("SafeZone_WallCrack", cafe, new Vector3(-3.075f, 0.12f, -0.4f), new Vector3(0.65f, 0.24f, 0.3f), "벽 틈", false, m);

            var spec = new FacadeSpec { floors = 4, seed = 11, litChance = 0.5f };
            TexBoxXZ("Upper_Mass", cafe, -9f, -3.0f, -1.0f, 7.0f, 4f, spec.TopY, p.limestone, 1.2f);
            var frame = FacadeFrame("Facade_Cafe", cafe, new Vector3(-3.0f, 0f, 3.0f), Vector3.right);
            FacadeDetails(frame, 8f, spec, p);
            ShopFront(frame, 0.1f - 3.0f, 6.6f - 3.0f, 4f, p.shopRed, p.awningRed, p.awningRedTop, PetitParisText.SignCafeName, font, p);
            StreetSign("StreetSign_Rue", frame, new Vector3(0f, 2.9f, -3.55f), PetitParisText.StreetName, font, p);

            // 시작 지점 뒤(남쪽)를 막는 건물
            var southSpec = new FacadeSpec { floors = 3, seed = 13, shutters = true, shutterMaterial = p.shutterBlue, litChance = 0.45f };
            TexBoxXZ("SouthBuilding", cafe, -9f, 10f, -7f, -1.0f, 0f, southSpec.TopY, p.plasterPink, 2f);
            FacadeDetails(FacadeFrame("Facade_South", cafe, new Vector3(0.5f, 0f, -1.0f), Vector3.forward), 7f, southSpec, p);
        }

        static void BuildBakery(Transform env, Palette p, TMP_FontAsset font)
        {
            var bakery = Group("Bakery", env);
            var spec = new FacadeSpec { floors = 5, seed = 23, shutters = true, shutterMaterial = p.shutterBlue, litChance = 0.45f };
            TexBoxXZ("Ground_Mass", bakery, -9f, -3.0f, 7.0f, 13.0f, 0f, 4f, p.rustic, 1.2f);
            TexBoxXZ("Upper_Mass", bakery, -9f, -3.0f, 7.0f, 13.0f, 4f, spec.TopY, p.plasterOchre, 2f);
            var frame = FacadeFrame("Facade_Bakery", bakery, new Vector3(-3.0f, 0f, 10f), Vector3.right);
            FacadeDetails(frame, 6f, spec, p);
            ShopFront(frame, 7.4f - 10f, 12.6f - 10f, 4f, p.shopBlue, p.awningBlue, p.awningBlueTop, PetitParisText.SignBakeryLatin, font, p);
        }

        static void BuildEastBuildings(Transform env, Palette p, TMP_FontAsset font)
        {
            var east = Group("EastBuildings", env);
            var spec = new FacadeSpec { floors = 5, seed = 37, litChance = 0.4f };
            TexBoxXZ("Ground_Mass", east, 4.0f, 10f, -1.0f, 13.3f, 0f, 4f, p.rustic, 1.2f);
            TexBoxXZ("Upper_Mass", east, 4.0f, 10f, -1.0f, 13.3f, 4f, spec.TopY, p.limestone, 1.2f);
            const float zc = 6.15f;
            var frame = FacadeFrame("Facade_East", east, new Vector3(4.0f, 0f, zc), Vector3.left);
            FacadeDetails(frame, 14.3f, spec, p);
            // 바깥이 -X인 정면: 정면 좌표 z = -(월드 z - 중심)
            ShopFront(frame, -(0.8f - zc), -(5.8f - zc), 4f, p.shopGreen, p.awningGreen, p.awningGreenTop, PetitParisText.SignFlowers, font, p);
            ShopFront(frame, -(7.5f - zc), -(12.5f - zc), 4f, p.shopBrown, p.awningRed, p.awningRedTop, PetitParisText.SignBooks, font, p);
        }

        static void BuildAlleyAndCrossStreet(Transform env, Palette p)
        {
            var alley = Group("Alley", env);
            var southSpec = new FacadeSpec { floors = 3, seed = 41, shutters = true, shutterMaterial = p.shutterGreen, litChance = 0.55f, windowSpacing = 1.8f };
            var northSpec = new FacadeSpec { floors = 4, seed = 43, shutters = true, shutterMaterial = p.shutterBlue, litChance = 0.55f, windowSpacing = 1.8f };

            // 덩어리 (충돌체). 좁은 골목(x -1.7~0.7)과 자전거가 지나는 가로 골목(z 16.4~17.6)은 비워 둔다
            TexBoxXZ("Block_SouthWest", alley, -7.6f, -1.7f, 13.0f, 16.4f, 0f, southSpec.TopY, p.plasterPink, 2f);
            TexBoxXZ("Block_SouthEast", alley, 0.7f, 7.6f, 13.3f, 16.4f, 0f, southSpec.TopY, p.plasterOchre, 2f);
            // Block_NorthWest(x -7.6~-1.7, z 17.6~23.0)는 속이 빈 비스트로 주방 건물 → BuildKitchen
            TexBoxXZ("Block_NorthEast", alley, 0.7f, 7.6f, 17.6f, 23.0f, 0f, northSpec.TopY, p.plasterPink, 2f);
            TexBoxXZ("CrossStreet_EndWest", alley, -8f, -7.6f, 16.4f, 17.6f, 0f, 6f, p.plasterCream, 2f);
            TexBoxXZ("CrossStreet_EndEast", alley, 7.6f, 8f, 16.4f, 17.6f, 0f, 6f, p.plasterCream, 2f);

            FacadeDetails(FacadeFrame("Facade_AlleyWestSouth", alley, new Vector3(-1.7f, 0f, 14.7f), Vector3.right), 3.4f, southSpec, p);
            FacadeDetails(FacadeFrame("Facade_AlleyEastSouth", alley, new Vector3(0.7f, 0f, 14.85f), Vector3.left), 3.1f, southSpec, p);
            FacadeDetails(FacadeFrame("Facade_RoadEnd", alley, new Vector3(2.35f, 0f, 13.3f), Vector3.back), 3.3f, southSpec, p);
            FacadeDetails(FacadeFrame("Facade_CrossSouthWest", alley, new Vector3(-4.65f, 0f, 16.4f), Vector3.forward), 5.9f, southSpec, p);
            FacadeDetails(FacadeFrame("Facade_CrossSouthEast", alley, new Vector3(4.15f, 0f, 16.4f), Vector3.forward), 6.9f, southSpec, p);
            FacadeDetails(FacadeFrame("Facade_CrossNorthWest", alley, new Vector3(-4.65f, 0f, 17.6f), Vector3.back), 5.9f, northSpec, p);
            FacadeDetails(FacadeFrame("Facade_CrossNorthEast", alley, new Vector3(4.15f, 0f, 17.6f), Vector3.back), 6.9f, northSpec, p);
            FacadeDetails(FacadeFrame("Facade_AlleyWestNorth", alley, new Vector3(-1.7f, 0f, 20.3f), Vector3.right), 5.4f, northSpec, p);
            FacadeDetails(FacadeFrame("Facade_AlleyEastNorth", alley, new Vector3(0.7f, 0f, 20.3f), Vector3.left), 5.4f, northSpec, p);
        }

        static void BuildQuaiAndSeine(Transform env, Palette p)
        {
            var quai = Group("Quai", env);
            TexBoxXZ("Quai_WestWall", quai, -4.4f, -4.0f, 23.0f, 25.9f, 0f, 1.0f, p.limestone, 1.2f);
            TexBoxXZ("Quai_EastWall", quai, 4.4f, 4.8f, 23.0f, 25.9f, 0f, 1.0f, p.limestone, 1.2f);
            TexBoxXZ("Parapet_West", quai, -4.0f, -1.0f, 25.6f, 25.9f, 0f, 0.45f, p.limestone, 1.2f);
            TexBoxXZ("Parapet_East", quai, 0.0f, 4.4f, 25.6f, 25.9f, 0f, 0.45f, p.limestone, 1.2f);
            var invisible = TexBoxXZ("Parapet_Gap_Invisible", quai, -1.0f, 0.0f, 25.85f, 25.9f, 0f, 0.4f, p.limestone, 1f);
            invisible.GetComponent<MeshRenderer>().enabled = false; // 난간 틈으로 떨어지지 않게
            LampPost("QuaiLamp_W", quai, new Vector3(-2.8f, 0f, 25.25f), p);
            LampPost("QuaiLamp_E", quai, new Vector3(2.6f, 0f, 25.25f), p);

            // 센강과 강 건너 풍경 (Unlit + 안개 → 해 질 녘 원경)
            TexBoxXZ("Seine_Water", quai, -40f, 40f, 25.9f, 80f, -1.2f, -0.9f, p.water, 6f, false);
            TexBoxXZ("Seine_QuaiFace", quai, -40f, 40f, 25.9f, 26.1f, -1.0f, 0f, p.limestone, 1.2f, false);
            TexBoxXZ("FarBank", quai, -40f, 40f, 58f, 62f, -1f, 0.8f, p.farHaze, 4f, false);
            FarSkyline("FarSkyline", quai, 63f, 70f, -38f, 38f, p, 7);
            EiffelSilhouette("EiffelTower", quai, new Vector3(-16f, -1f, 66f), 24f, p.eiffel);

            // 다리와 다리 위 가로등 불빛
            TexBoxXZ("Bridge_Deck", quai, -40f, 40f, 40f, 42.5f, 2.0f, 2.6f, p.limestone, 2f, false);
            foreach (float x in new[] { -20f, -8f, 4f, 16f })
                TexBoxXZ($"Bridge_Pier_{x}", quai, x, x + 1.5f, 40.2f, 42.3f, -1f, 2.0f, p.limestone, 2f, false);
            for (float x = -30f; x <= 30f; x += 5f)
                RemoveCollider(Sphere("Bridge_Light", quai, new Vector3(x, 3.1f, 40.1f), 0.35f, p.bulb));

            AddPhotoBackdropIfPresent(quai);
        }

        /// <summary>
        /// 사진 배경(선택): Assets/PetitParis/ThirdParty/Backdrops/ 안에 이름이 ParisBackdrop인 이미지가 있으면
        /// 강 건너 맨 뒤에 큰 판으로 세운다. 쥐 시점에서는 양안 시차가 아주 작아서 먼 사진도 입체처럼 자연스럽다.
        /// 라이선스가 확인된 사진만 넣고 docs/CREDITS.md에 출처를 적는다.
        /// </summary>
        static void AddPhotoBackdropIfPresent(Transform parent)
        {
            const string folder = PetitParisPaths.Root + "/ThirdParty/Backdrops";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            var guids = AssetDatabase.FindAssets("ParisBackdrop t:Texture2D", new[] { folder });
            if (guids.Length == 0) return;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (tex == null) return;
            var mat = new MaterialLibrary().UnlitTex("PP_PhotoBackdrop", tex, Color.white);
            float width = 150f, height = width * tex.height / Mathf.Max(1f, tex.width);
            var quad = RemoveCollider(GameObject.CreatePrimitive(PrimitiveType.Quad));
            quad.name = "PhotoBackdrop";
            quad.transform.SetParent(parent, false);
            quad.transform.position = new Vector3(0f, height * 0.5f - 6f, 76f);
            quad.transform.localScale = new Vector3(width, height, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Debug.Log("[PetitParis] 사진 배경을 배치했습니다: " + AssetDatabase.GetAssetPath(tex));
        }

        /// <summary>파리 소품: 가로등, 테라스 줄 전구, 모리스 기둥.</summary>
        static void BuildParisProps(Transform env, Palette p)
        {
            var props = Group("ParisProps", env);
            foreach (float z in new[] { 2.0f, 4.1f, 6.2f })
            {
                LampPost($"Lamp_Terrace_{z}", props, new Vector3(0.72f, 0f, z), p);
                StringLights($"StringLights_{z}", props, new Vector3(-2.95f, 3.65f, z), new Vector3(0.72f, 3.5f, z), 0.35f, 10, p);
            }
            LampPost("Lamp_Bakery_1", props, new Vector3(0.72f, 0f, 8.5f), p);
            LampPost("Lamp_Bakery_2", props, new Vector3(0.72f, 0f, 11.2f), p);
            MorrisColumn("MorrisColumn", props, new Vector3(2.5f, 0f, 6.6f), p);
        }

        static void BuildProps(Transform env, MaterialLibrary m)
        {
            var props = Group("Props", env);
            // 카페 테라스: 테이블 3세트 (의자 아래 안전 구역 6곳)
            CafeSet("CafeSet_1", props, new Vector3(-1.5f, 0f, 1.6f), m);
            CafeSet("CafeSet_2", props, new Vector3(-1.5f, 0f, 3.6f), m);
            CafeSet("CafeSet_3", props, new Vector3(-1.5f, 0f, 5.6f), m);
            Planter("Planter_T1", props, new Vector3(0.62f, 0f, 0.8f), m);
            Planter("Planter_T2", props, new Vector3(0.62f, 0f, 3.0f), m);
            Planter("Planter_T3", props, new Vector3(0.62f, 0f, 5.2f), m);
            PaperCup("Cup_T1", props, new Vector3(-0.9f, 0f, 2.6f), m);
            PaperCup("Cup_T2", props, new Vector3(-0.3f, 0f, 4.4f), m);
            Crate("Box_T1", props, new Vector3(-2.85f, 0f, 2.6f), Vector3.one * 0.22f, 12f, m);

            // 빵집 앞
            Bench("Bench_Bakery", props, new Vector3(-2.7f, 0f, 10.0f), -90f, 1.0f, m, true);
            Crate("Crate_B1", props, new Vector3(-2.8f, 0f, 8.2f), Vector3.one * 0.3f, 0f, m);
            Crate("Crate_B2", props, new Vector3(-2.8f, 0f, 11.4f), Vector3.one * 0.3f, 6f, m);
            Crate("Crate_B3", props, new Vector3(-2.8f, 0.3f, 11.4f), Vector3.one * 0.28f, -8f, m);
            Planter("Planter_B1", props, new Vector3(0.62f, 0f, 9.6f), m);
            Planter("Planter_B2", props, new Vector3(0.62f, 0f, 12.2f), m);
            PaperCup("Cup_B1", props, new Vector3(-1.2f, 0f, 8.0f), m);
            PaperCup("Cup_B2", props, new Vector3(0.45f, 0f, 11.6f), m);
            PaperCup("Cup_B3", props, new Vector3(-0.6f, 0f, 12.6f), m);
            var board = Group("SandwichBoard_Pain", props);
            board.localPosition = new Vector3(0.5f, 0f, 7.25f);
            Box("Board", board, new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.7f, 0.05f), m.Lit("PP_SignDark", new Color(0.12f, 0.12f, 0.12f)));
            Label("Board_Text", board, new Vector3(0f, 0.42f, -0.03f), "PAIN\nCROISSANT", 0.9f, 0.5f, Color.white, null, null);

            // 골목
            Bench("Bench_Alley", props, new Vector3(-1.35f, 0f, 15.0f), -90f, 1.0f, m, true);
            Crate("Crate_A1", props, new Vector3(-1.45f, 0f, 14.0f), Vector3.one * 0.35f, 10f, m);
            PaperCup("Cup_A1", props, new Vector3(0.4f, 0f, 14.4f), m);
            // 빗자루 웨이터 추격 때 숨을 곳: 팔레트(바닥과 틈 16cm), 옆으로 눕힌 종이 상자
            var pallet = Group("Pallet_Alley", props);
            pallet.localPosition = new Vector3(-1.38f, 0f, 19.25f);
            var palletWood = m.LitTex("PP_PalletWood", PetitParisTextures.Wood(), new Color(0.95f, 0.85f, 0.7f));
            foreach (float x in new[] { -0.26f, 0f, 0.26f })
                Box("Block", pallet, new Vector3(x, 0.08f, 0f), new Vector3(0.06f, 0.16f, 0.58f), palletWood);
            for (int b = 0; b < 5; b++)
                Box("Board", pallet, new Vector3(0f, 0.175f, -0.24f + b * 0.12f), new Vector3(0.6f, 0.03f, 0.09f), palletWood);
            SafeZone("SafeZone_UnderPallet", pallet, new Vector3(-0.13f, 0.08f, 0f), new Vector3(0.18f, 0.16f, 0.5f), "팔레트 밑", false, m);
            SafeZone("SafeZone_UnderPallet2", pallet, new Vector3(0.13f, 0.08f, 0f), new Vector3(0.18f, 0.16f, 0.5f), "팔레트 밑", false, m);

            var box = Group("CardboardBox_Alley", props);
            box.localPosition = new Vector3(0.4f, 0f, 22.5f);
            var card = m.Lit("PP_Cardboard", new Color(0.72f, 0.55f, 0.36f));
            Box("Top", box, new Vector3(0f, 0.31f, 0f), new Vector3(0.45f, 0.02f, 0.4f), card);
            Box("Back", box, new Vector3(0.215f, 0.16f, 0f), new Vector3(0.02f, 0.32f, 0.4f), card);
            Box("SideN", box, new Vector3(0f, 0.16f, 0.19f), new Vector3(0.45f, 0.32f, 0.02f), card);
            Box("SideS", box, new Vector3(0f, 0.16f, -0.19f), new Vector3(0.45f, 0.32f, 0.02f), card);
            SafeZone("SafeZone_InBox", box, new Vector3(0.02f, 0.14f, 0f), new Vector3(0.36f, 0.28f, 0.32f), "종이 상자 안", false, m);

            Crate("Crate_A2", props, new Vector3(-1.45f, 0f, 20.0f), Vector3.one * 0.35f, 0f, m);
            Crate("Crate_A3", props, new Vector3(-1.45f, 0.35f, 20.0f), Vector3.one * 0.3f, 15f, m);
            Cylinder("TrashBin", props, new Vector3(0.42f, 0.375f, 20.6f), 0.42f, 0.75f, m.Lit("PP_BinGreen", new Color(0.2f, 0.32f, 0.22f)));
            PaperCup("Cup_A2", props, new Vector3(-0.2f, 0f, 21.8f), m);
        }

        static void SetPathFloat(PedestrianPath path, string field, float value)
        {
            var so = new SerializedObject(path);
            SetFloat(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void StoryTrigger(string name, Transform parent, Vector3 center, Vector3 size, string message, AudioSource sfx)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var trigger = go.AddComponent<RatStoryTrigger>();
            var so = new SerializedObject(trigger);
            SetObject(so, "volume", box);
            SetString(so, "message", message);
            SetObject(so, "sfx", sfx);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ───────────────────────── 빵 부스러기 배치 (위험할수록 큰 조각) ─────────────────────────

        static void PlaceCrumbs(Transform root, MaterialLibrary m)
        {
            const CrumbPickup.Kind C = CrumbPickup.Kind.Crumb, R = CrumbPickup.Kind.Croissant, S = CrumbPickup.Kind.Cheese, G = CrumbPickup.Kind.GoldenCroissant;
            int i = 0;
            void Put(CrumbPickup.Kind kind, float x, float y, float z) =>
                PetitParisGameplayKit.Crumb($"{kind}_{i++:00}", root, new Vector3(x, y, z), kind, m);

            // 테라스: 안내 경로 위 부스러기 줄 + 웨이터가 도는 길 위 크루아상 + 벽과 의자 사이 황금 크루아상
            Put(C, -2.3f, 0f, 0.35f); Put(C, -1.8f, 0f, 0.75f); Put(C, -1.15f, 0f, 1.6f); Put(C, -1.15f, 0f, 2.6f);
            Put(C, -1.15f, 0f, 3.6f); Put(C, -1.15f, 0f, 4.6f); Put(C, -1.15f, 0f, 5.6f);
            Put(R, -2.5f, 0f, 3.0f); Put(R, 0.25f, 0f, 4.6f);
            Put(S, -1.5f, 0f, 2.6f);  // 두 테이블 사이 (보행자 길과 가까움)
            Put(G, -2.45f, 0f, 5.2f); // 웨이터가 지나가는 벽 쪽 길 한가운데

            // 빵집 앞: 지그재그 요리사 + 떨어지는 바게트
            Put(C, -1.6f, 0f, 8.0f); Put(C, -2.05f, 0f, 8.7f); Put(C, -2.3f, 0f, 11.3f); Put(C, -1.8f, 0f, 11.9f); Put(C, -1.0f, 0f, 12.9f);
            Put(R, -0.9f, 0f, 9.3f); Put(R, 0.0f, 0f, 11.0f);
            Put(S, -1.0f, 0f, 10.4f);
            Put(G, 0.3f, 0f, 8.7f);   // 요리사가 방향을 바꾸는 자리

            // 골목: 자전거 길 + 빗자루 웨이터
            Put(C, -0.8f, 0f, 14.2f); Put(C, -0.5f, 0f, 15.6f); Put(C, -0.5f, 0f, 16.1f);
            Put(R, -0.5f, 0f, 17.0f); // 자전거 길 한가운데
            Put(G, 3.0f, 0f, 16.95f); // 자전거 길 동쪽 끝까지 다녀와야 함
            Put(C, -0.5f, 0f, 18.6f); Put(C, -0.25f, 0f, 19.4f); Put(C, -0.6f, 0f, 20.3f); Put(C, -0.4f, 0f, 21.2f);
            Put(S, 0.3f, 0f, 21.0f);  // 빗자루 웨이터 바로 옆

            // 강변: 비둘기 사이
            Put(C, -1.5f, 0f, 24.5f); Put(C, -2.2f, 0f, 24.3f); Put(C, 1.5f, 0f, 24.8f);
            Put(R, 2.9f, 0f, 25.0f);

            // 주방: 널빤지·상자 위 부스러기 줄, 요리사 통로의 크루아상, 식탁 위 치즈 조각
            foreach (var (from, to) in KitchenPlanks)
                Put(C, Mathf.Lerp(from.x, to.x, 0.5f), Mathf.Lerp(from.y, to.y, 0.5f), from.z);
            Put(C, -3.15f, 0.3f, 21.5f); Put(C, -4.35f, 0.6f, 21.5f); Put(C, -5.55f, 0.9f, 21.5f);
            Put(R, -4.6f, 0f, 22.2f);
            Put(S, -5.2f, 0.9f, 20.85f); Put(S, -3.95f, 0.9f, 19.9f);
        }

        // ───────────────────────── 비스트로 주방 (마지막 구간) ─────────────────────────

        /// <summary>널빤지: (아랫면 시작 윗면 점, 끝 윗면 점). 상자 A(0.3m) → B(0.6m) → C(0.9m) → 식탁(0.9m).</summary>
        static readonly (Vector3 from, Vector3 to)[] KitchenPlanks =
        {
            (new Vector3(-2.15f, KitchenFloorTop, 21.5f), new Vector3(-2.95f, 0.3f, 21.5f)),
            (new Vector3(-3.35f, 0.3f, 21.5f), new Vector3(-4.15f, 0.6f, 21.5f)),
            (new Vector3(-4.55f, 0.6f, 21.5f), new Vector3(-5.35f, 0.9f, 21.5f)),
        };

        static void BuildKitchen(Transform env, Palette p, MaterialLibrary m, TMP_FontAsset font)
        {
            var k = Group("Kitchen_Bistro", env);
            const float x0 = -7.6f, x1 = -1.7f, z0 = 17.6f, z1 = 23.0f, wall = 0.2f, ceil = 3.2f;
            const float doorX0 = -3.4f, doorX1 = -2.5f, doorTop = 2.1f;
            var northSpec = new FacadeSpec { floors = 4 };

            // ── 건물 껍데기 (바깥은 기존 골목 건물과 같은 회반죽) ──
            TexBoxXZ("Wall_East", k, x1 - wall, x1, z0, z1, 0f, ceil, p.plasterCream, 2f);
            TexBoxXZ("Wall_West", k, x0, x0 + wall, z0, z1, 0f, ceil, p.plasterCream, 2f);
            TexBoxXZ("Wall_South", k, x0 + wall, x1 - wall, z0, z0 + wall, 0f, ceil, p.plasterCream, 2f);
            TexBoxXZ("Wall_North_W", k, x0 + wall, doorX0, z1 - wall, z1, 0f, ceil, p.plasterCream, 2f);
            TexBoxXZ("Wall_North_E", k, doorX1, x1 - wall, z1 - wall, z1, 0f, ceil, p.plasterCream, 2f);
            TexBoxXZ("Wall_North_Lintel", k, doorX0, doorX1, z1 - wall, z1, doorTop, ceil, p.plasterCream, 2f);
            TexBoxXZ("Upper_Mass", k, x0, x1, z0, z1, ceil, northSpec.TopY, p.plasterCream, 2f);

            // ── 실내 마감: 흑백 타일 바닥, 흰 타일 벽 ──
            var floorTile = PolyHaven(m, "PP_KitchenFloor", "checkered_pavement_tiles", 2f, 2.4f, new Color(1.45f, 1.4f, 1.35f),
                () => PetitParisTextures.Checker("PP_KitchenFloorTex", new Color(0.12f, 0.12f, 0.13f), new Color(0.93f, 0.91f, 0.86f), 8));
            var wallTile = m.LitTex("PP_KitchenWallTile", PetitParisTextures.Checker("PP_KitchenWallTileTex", new Color(0.97f, 0.97f, 0.95f), new Color(0.88f, 0.9f, 0.9f), 16), Color.white);
            var steel = m.Lit("PP_Steel", new Color(0.7f, 0.72f, 0.75f));
            var darkSteel = m.Lit("PP_DarkSteel", new Color(0.22f, 0.22f, 0.24f));
            var copper = m.Lit("PP_Copper", new Color(0.82f, 0.46f, 0.26f));
            var woodTop = m.LitTex("PP_ButcherBlock", PetitParisTextures.Wood(), new Color(1f, 0.85f, 0.65f));
            var plankMat = PolyHaven(m, "PP_PlankWood", "brown_planks_05", 1f, 0.6f, new Color(1f, 0.85f, 0.65f), () => PetitParisTextures.Wood());
            var glow = m.Unlit("PP_StoveGlow", new Color(1f, 0.55f, 0.18f));
            var cheese = m.Lit("PP_CheeseWheel", new Color(1f, 0.8f, 0.3f));
            var cheeseRind = m.Lit("PP_CheeseRind", new Color(0.9f, 0.62f, 0.2f));

            float ix0 = x0 + wall, ix1 = x1 - wall, iz0 = z0 + wall, iz1 = z1 - wall;
            TexBoxXZ("Floor_Tiles", k, ix0, ix1, iz0, iz1, 0f, KitchenFloorTop, floorTile, 2.4f, false);
            TexBoxXZ("Floor_Doorway", k, doorX0, doorX1, iz1, z1, 0f, KitchenFloorTop, floorTile, 2.4f, false);
            const float lining = 0.01f, tileTop = 1.6f;
            TexBoxXZ("Tile_East", k, ix1 - lining, ix1, iz0, iz1, 0f, tileTop, wallTile, 1.6f, false);
            TexBoxXZ("Tile_West", k, ix0, ix0 + lining, iz0, iz1, 0f, tileTop, wallTile, 1.6f, false);
            TexBoxXZ("Tile_South", k, ix0, ix1, iz0, iz0 + lining, 0f, tileTop, wallTile, 1.6f, false);
            TexBoxXZ("Tile_North_W", k, ix0, doorX0, iz1 - lining, iz1, 0f, tileTop, wallTile, 1.6f, false);
            TexBoxXZ("Tile_North_E", k, doorX1, ix1, iz1 - lining, iz1, 0f, tileTop, wallTile, 1.6f, false);

            // ── 남쪽 조리대 (아래 선반 밑은 쥐가 숨을 수 있는 틈 16cm) ──
            var counter = Group("Counter_South", k);
            TexBoxXZ("Top", counter, -7.3f, -2.0f, iz0, 18.45f, 0.86f, 0.9f, steel, 1f);
            TexBoxXZ("LowShelf", counter, -7.3f, -2.0f, iz0, 18.45f, 0.16f, 0.19f, steel, 1f);
            TexBoxXZ("Back", counter, -7.3f, -2.0f, iz0, iz0 + 0.03f, 0.19f, 0.86f, darkSteel, 1f);
            foreach (float x in new[] { -7.25f, -5.5f, -3.8f, -2.05f })
                foreach (float z in new[] { iz0 + 0.04f, 18.41f })
                    TexBoxXZ("Leg", counter, x - 0.025f, x + 0.025f, z - 0.025f, z + 0.025f, 0f, 0.86f, steel, 1f);
            SafeZone("SafeZone_UnderCounter", counter, new Vector3(-4.65f, 0.08f, 18.15f), new Vector3(5.1f, 0.16f, 0.5f), "조리대 밑", false, m);
            for (int i = 0; i < 5; i++)
            {
                RemoveCollider(Cylinder("Pot", counter, new Vector3(-6.8f + i * 1.1f, 0.9f + 0.1f, 18.15f), 0.24f, 0.2f, i % 2 == 0 ? copper : steel));
                RemoveCollider(Sphere("Tomato", counter, new Vector3(-6.3f + i * 1.1f, 0.94f, 18.2f), 0.08f, m.Lit("PP_Tomato", new Color(0.85f, 0.15f, 0.1f))));
            }

            // ── 서쪽 화덕 (오븐 문과 화구가 주황빛) ──
            var stove = Group("Stove", k);
            TexBoxXZ("Body", stove, ix0, -6.65f, 19.0f, 21.2f, 0f, 0.9f, darkSteel, 1f);
            TexBoxXZ("OvenGlow", stove, -6.65f, -6.64f, 19.35f, 20.85f, 0.2f, 0.7f, glow, 1f, false);
            foreach (float z in new[] { 19.4f, 20.1f, 20.8f })
            {
                RemoveCollider(Cylinder("Burner", stove, new Vector3(-7.0f, 0.905f, z), 0.22f, 0.01f, glow));
                RemoveCollider(Cylinder("CopperPot", stove, new Vector3(-7.0f, 1.03f, z), 0.3f, 0.24f, copper));
            }
            TexBoxXZ("Hood", stove, ix0, -6.5f, 18.9f, 21.3f, 2.0f, 2.4f, steel, 1f, false);
            var fire = new GameObject("StoveLight").AddComponent<Light>();
            fire.transform.SetParent(stove, false);
            fire.transform.position = new Vector3(-6.4f, 0.6f, 20.1f);
            fire.type = LightType.Point;
            fire.color = new Color(1f, 0.55f, 0.25f);
            fire.range = 2.5f;
            fire.intensity = 1.5f;
            fire.shadows = LightShadows.None;

            // ── 북쪽 벽 선반 (병, 치즈 바퀴) ──
            var shelf = Group("Shelves_North", k);
            foreach (float y in new[] { 1.1f, 1.6f })
            {
                TexBoxXZ("Shelf", shelf, -7.3f, -3.9f, 22.42f, iz1, y, y + 0.03f, plankMat, 0.6f, false);
                for (int i = 0; i < 7; i++)
                {
                    float x = -7.1f + i * 0.48f;
                    if (i % 3 == 0) RemoveCollider(Cylinder("CheeseWheel", shelf, new Vector3(x, y + 0.09f, 22.6f), 0.28f, 0.12f, cheese));
                    else RemoveCollider(Cylinder("Jar", shelf, new Vector3(x, y + 0.13f, 22.6f), 0.11f, 0.2f, i % 2 == 0 ? copper : m.Lit("PP_JarGlass", new Color(0.55f, 0.7f, 0.55f))));
                }
            }

            // ── 가운데 식탁 (목적지: 치즈) ──
            var table = Group("PrepTable", k);
            TexBoxXZ("Top", table, -5.6f, -3.6f, 19.6f, 21.0f, 0.86f, 0.9f, woodTop, 0.8f);
            foreach (float x in new[] { -5.52f, -3.68f })
                foreach (float z in new[] { 19.68f, 20.92f })
                    TexBoxXZ("Leg", table, x - 0.04f, x + 0.04f, z - 0.04f, z + 0.04f, 0f, 0.86f, woodTop, 0.8f);
            SafeZone("SafeZone_UnderTable", table, new Vector3(-4.6f, 0.2f, 20.3f), new Vector3(1.7f, 0.4f, 1.1f), "식탁 밑", false, m);
            var wheel = Cylinder("GoalCheeseWheel", table, GoalWheelCenter + new Vector3(0f, 0.075f, 0f), 0.42f, 0.15f, cheese);
            RemoveCollider(Cylinder("Rind", table, GoalWheelCenter + new Vector3(0f, 0.075f, 0f), 0.43f, 0.12f, cheeseRind));
            wheel.name = "GoalCheeseWheel";
            RemoveCollider(Box("CheeseWedge", table, new Vector3(CheeseGoal.x + 0.05f, 0.93f, CheeseGoal.z + 0.12f), new Vector3(0.14f, 0.06f, 0.09f), cheese));
            RemoveCollider(Box("Baguette", table, new Vector3(-5.0f, 0.935f, 19.85f), new Vector3(0.6f, 0.065f, 0.065f), m.Lit("PP_BaguetteCrust", new Color(0.82f, 0.52f, 0.22f))));
            var light = new GameObject("KitchenLight").AddComponent<Light>();
            light.transform.SetParent(table, false);
            light.transform.position = new Vector3(-4.6f, 2.5f, 20.3f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.82f, 0.6f);
            light.range = 6f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;

            // 식탁 위 매달린 구리 냄비 걸이
            var rack = Group("PotRack", k);
            TexBoxXZ("Bar", rack, -5.4f, -3.8f, 20.27f, 20.33f, 2.3f, 2.34f, darkSteel, 1f, false);
            for (int i = 0; i < 5; i++)
            {
                float x = -5.2f + i * 0.35f;
                TexBoxXZ("Hook", rack, x - 0.005f, x + 0.005f, 20.295f, 20.305f, 2.05f, 2.3f, darkSteel, 1f, false);
                RemoveCollider(Cylinder("HangingPot", rack, new Vector3(x, 1.95f, 20.3f), 0.2f + (i % 2) * 0.06f, 0.16f, copper));
            }

            // ── 오르막: 널빤지 → 상자 A(0.3) → 널빤지 → 상자 B(0.6) → 널빤지 → 상자 C(0.9) → 식탁 ──
            var climb = Group("Climb", k);
            for (int i = 0; i < KitchenPlanks.Length; i++)
                PetitParisGameplayKit.Plank($"Plank_{i}", climb, KitchenPlanks[i].from, KitchenPlanks[i].to, 0.15f, plankMat);
            PetitParisGameplayKit.CrateStack("Crates_A", climb, -3.35f, -2.95f, 21.3f, 21.7f, 1, m);
            PetitParisGameplayKit.CrateStack("Crates_B", climb, -4.55f, -4.15f, 21.3f, 21.7f, 2, m);
            PetitParisGameplayKit.CrateStack("Crates_C", climb, -5.75f, -5.35f, 21.3f, 21.7f, 3, m);
            TexBoxXZ("Bridge_CuttingBoard", climb, -5.62f, -5.38f, 20.9f, 21.32f, 0.9f, 0.915f, woodTop, 0.5f);
            // 밀가루 포대 (장식)
            var sack = RemoveCollider(Sphere("FlourSack", climb, new Vector3(-6.3f, 0.25f, 21.45f), 0.5f, m.Lit("PP_FlourSack", new Color(0.92f, 0.88f, 0.78f))));
            sack.transform.localScale = new Vector3(0.45f, 0.5f, 0.38f);

            // ── 뒷문 (바깥으로 열린 문) + 바깥 간판·등 ──
            var door = Group("BackDoor", k);
            var doorLeaf = Box("DoorLeaf", door, Vector3.zero, new Vector3(0.05f, doorTop - 0.02f, doorX1 - doorX0 - 0.02f), m.Lit("PP_DoorGreen", new Color(0.18f, 0.32f, 0.26f)));
            doorLeaf.transform.position = new Vector3(doorX0 + 0.03f, doorTop * 0.5f, z1 + 0.06f + (doorX1 - doorX0) * 0.5f); // 강변 쪽으로 열린 문 (주방 통로를 막지 않게)
            TexBoxXZ("Frame_W", door, doorX0 - 0.08f, doorX0, z1, z1 + 0.05f, 0f, doorTop + 0.08f, p.trim, 1f, false);
            TexBoxXZ("Frame_E", door, doorX1, doorX1 + 0.08f, z1, z1 + 0.05f, 0f, doorTop + 0.08f, p.trim, 1f, false);
            TexBoxXZ("Frame_Top", door, doorX0 - 0.08f, doorX1 + 0.08f, z1, z1 + 0.05f, doorTop, doorTop + 0.08f, p.trim, 1f, false);
            var sign = Label("BistroSign", door, new Vector3((doorX0 + doorX1) * 0.5f, doorTop + 0.55f, z1 + 0.06f), PetitParisText.SignBistro, 2.2f, 2.2f,
                new Color(0.98f, 0.82f, 0.45f), font, null);
            sign.transform.rotation = Quaternion.LookRotation(Vector3.back); // 강변(북쪽)에서 읽힘
            RemoveCollider(Sphere("DoorLamp", door, new Vector3(doorX1 + 0.35f, doorTop + 0.2f, z1 + 0.15f), 0.18f, p.lampGlow));
            Crate("Crate_Door", door, new Vector3(doorX1 + 0.5f, 0f, z1 + 0.3f), Vector3.one * 0.35f, 8f, m);
        }

        static RatHintSign Hint(string name, Transform parent, Vector3 position, string text, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            // 밝은 벽·바닥 위에서 읽히도록 짙은 남색 글자 (외곽선은 머티리얼 인스턴스를 만들어 쓰지 않음)
            var tmp = Label("Text", go.transform, Vector3.zero, text, 0.1f, 0.4f, new Color(0.1f, 0.12f, 0.22f), font, null);
            var hint = go.AddComponent<RatHintSign>();
            Wire(hint, ("text", tmp));
            return hint;
        }
    }
}
