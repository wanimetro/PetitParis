using TMPro;
using UnityEditor;
using UnityEngine;
using static PetitParis.EditorTools.PetitParisBuildKit;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 실제 플레이용 게임 씬 PetitParisVR.unity (블록아웃, 기본 도형 + 단색 머티리얼).
    ///
    /// 동선 (월드 미터, +Z가 북쪽, 경로 길이 약 25m ≈ 걷기 3~4분)
    ///   시작   카페 벽 틈 (-3.25, 0, -0.4) → 동쪽을 보고 시작, 틈 안은 안전 구역
    ///   공간1  카페 테라스 z 0~7      : 테이블 3세트(의자 아래 안전), 보행자 A가 테라스를 한 바퀴씩 돈다
    ///   공간2  빵집 앞 보도 z 7~13    : 보행자 B가 지그재그 왕복, 벤치 아래 안전 구역
    ///   공간3  좁은 골목 z 13~23      : 중간을 자전거가 가로질러 왕복 (z 16.4~17.6)
    ///   목적지 골목 출구 z 23.8       : 난간 사이로 센강
    /// 체크포인트: 시작 → 테라스 끝 의자 아래 → 빵집 벤치 아래 → 자전거 길 앞 벤치 아래
    /// </summary>
    public static class PetitParisLevelBuilder
    {
        static readonly Vector3 Spawn = new Vector3(-3.25f, 0f, -0.4f);
        const float SpawnYaw = 90f; // 동쪽(+X)을 보고 시작

        [MenuItem("PetitParis/3. Build Game Scene (PetitParisVR)", priority = 3)]
        public static void BuildGame()
        {
            if (!PrepareNewScene(PetitParisPaths.Scene, out var scene)) return;
            var m = new MaterialLibrary();
            var font = KoreanFont();

            SetupLighting(true);

            var env = new GameObject("Environment_Blockout").transform;
            BuildGround(env, m);
            BuildCafe(env, m, font);
            BuildBakery(env, m, font);
            BuildAlleyAndCrossStreet(env, m);
            BuildEastFacade(env, m);
            BuildQuaiAndSeine(env, m);
            BuildProps(env, m);
            MarkStatic(env);

            // ── 움직이는 위험 ──
            var npcs = Group("Pedestrians", null);
            Pedestrian("Pedestrian_A_Terrace", npcs,
                new[] { new Vector3(-2.5f, 0f, 0.2f), new Vector3(-2.5f, 0f, 6.9f), new Vector3(0.25f, 0f, 6.9f), new Vector3(0.25f, 0f, 0.2f) },
                PedestrianPath.LoopMode.Loop, 0.9f, 3f, new Color(0.25f, 0.3f, 0.45f), m);
            Pedestrian("Pedestrian_B_Bakery", npcs,
                new[] { new Vector3(-2.2f, 0f, 7.6f), new Vector3(0.3f, 0f, 8.7f), new Vector3(-2.2f, 0f, 9.8f),
                        new Vector3(0.3f, 0f, 10.9f), new Vector3(-2.2f, 0f, 12.0f), new Vector3(0.2f, 0f, 12.9f) },
                PedestrianPath.LoopMode.PingPong, 0.8f, 0f, new Color(0.55f, 0.32f, 0.25f), m);
            Bicycle("Bicycle_CrossStreet", Group("Vehicles", null), new Vector3(-7.0f, 0f, 17.0f), new Vector3(7.0f, 0f, 17.0f), m);

            // ── 게임 진행 ──
            var gameplay = Group("Gameplay", null);
            var cps = Group("Checkpoints", gameplay);
            var flags = new Renderer[4];
            flags[0] = Checkpoint("CP0_Start_WallCrack", cps, Spawn, SpawnYaw, m);
            flags[1] = Checkpoint("CP1_Terrace_UnderChair", cps, new Vector3(-1.5f, 0f, 6.08f), 0f, m);
            flags[2] = Checkpoint("CP2_Bakery_UnderBench", cps, new Vector3(-2.7f, 0f, 10.0f), 0f, m);
            flags[3] = Checkpoint("CP3_Alley_UnderBench", cps, new Vector3(-1.35f, 0f, 15.0f), 0f, m);
            var manager = cps.gameObject.AddComponent<CheckpointManager>();
            var chime = cps.gameObject.AddComponent<AudioSource>();
            var mso = new SerializedObject(manager);
            SetObjectArray(mso, "checkpoints", new Object[] { cps.GetChild(0), cps.GetChild(1), cps.GetChild(2), cps.GetChild(3) });
            SetObjectArray(mso, "markers", flags);
            SetObject(mso, "chimeSource", chime);
            mso.ApplyModifiedPropertiesWithoutUndo();

            // 목적지
            var goal = new GameObject("Goal_SeineExit");
            goal.transform.SetParent(gameplay, false);
            goal.transform.position = new Vector3(-0.5f, 0.2f, 23.8f);
            var goalBox = goal.AddComponent<BoxCollider>();
            goalBox.isTrigger = true;
            goalBox.size = new Vector3(2.6f, 0.4f, 1.0f);
            var goalPoint = Group("GoalPoint", gameplay);
            goalPoint.position = new Vector3(-0.5f, 0f, 23.8f);

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
            var goalHint = Hint("Hint_Goal", hints, new Vector3(-0.5f, 0.45f, 24.7f), PetitParisText.HintGoal, font);
            var gso = new SerializedObject(goalHint);
            SetFloat(gso, "fullDistance", 2.5f);
            SetFloat(gso, "fadeDistance", 5f);
            gso.ApplyModifiedPropertiesWithoutUndo();

            // 조작 안내 패널 (시작 지점 앞, 동쪽을 바라보는 쥐가 읽을 수 있게)
            var (intro, introText) = Panel("IntroPanel", gameplay, new Vector3(-2.72f, 0.25f, -0.4f), Quaternion.LookRotation(Vector3.right),
                new Vector2(0.36f, 0.22f), "<b>PETIT PARIS</b>\n" + PetitParisText.IntroBody, 0.12f, font, m);
            // 완료 패널 (도착하면 카메라 앞에 나타남)
            var (complete, completeText) = Panel("CompletePanel", gameplay, goalPoint.position + Vector3.up * 0.15f, Quaternion.identity,
                new Vector2(0.34f, 0.24f), PetitParisText.CompleteTitle, 0.12f, font, m);
            complete.SetActive(false);

            // ── 리그 ──
            var rig = BuildRig(Spawn, Quaternion.Euler(0f, SpawnYaw, 0f), false, m);
            if (rig == null) return;

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
            Wire(rig.statusPanel, ("gameFlow", flow), ("checkpoints", manager));

            if (!SaveScene(scene, PetitParisPaths.Scene, true)) return;
            Selection.activeGameObject = rig.root;
            Debug.Log($"[PetitParis] 게임 씬을 만들었습니다: {PetitParisPaths.Scene} (빌드 목록 맨 앞). 다음: PetitParis > 4. Validate Scene");
        }

        // ───────────────────────── 구역별 블록아웃 ─────────────────────────

        static void BuildGround(Transform env, MaterialLibrary m)
        {
            var g = Group("Ground", env);
            BoxXZ("Ground_Cobblestone", g, -8f, 8f, -2f, 25.9f, -0.1f, 0f, m.Lit("PP_Ground", new Color(0.42f, 0.4f, 0.38f)));
            // 구역별 바닥 색 (얇은 판, 충돌체 없음)
            Paving("Paving_Terrace", g, -3f, 0.8f, -1f, 7f, m.Lit("PP_PavingTerrace", new Color(0.62f, 0.56f, 0.48f)));
            Paving("Paving_Bakery", g, -3f, 0.8f, 7f, 13.3f, m.Lit("PP_PavingSidewalk", new Color(0.66f, 0.64f, 0.6f)));
            Paving("Paving_Road", g, 0.8f, 4f, -1f, 13.3f, m.Lit("PP_PavingRoad", new Color(0.34f, 0.33f, 0.33f)));
            Paving("Paving_Alley", g, -1.7f, 0.7f, 13.3f, 23f, m.Lit("PP_PavingAlley", new Color(0.5f, 0.48f, 0.46f)));
            Paving("Paving_BikeLane", g, -7.6f, 7.6f, 16.4f, 17.6f, m.Lit("PP_Asphalt", new Color(0.24f, 0.24f, 0.26f)));
            Paving("Paving_Quai", g, -4f, 4.4f, 23f, 25.9f, m.Lit("PP_PavingQuai", new Color(0.7f, 0.66f, 0.58f)));
            // 보도 경계석 (높이 1.5cm, 걸어서 넘을 수 있음)
            Paving("Curb_Terrace", g, 0.78f, 0.86f, -1f, 13.3f, m.Lit("PP_Curb", new Color(0.75f, 0.74f, 0.7f)), 0.015f);
        }

        static void Paving(string name, Transform parent, float x0, float x1, float z0, float z1, Material mat, float height = 0.003f)
        {
            var go = BoxXZ(name, parent, x0, x1, z0, z1, 0f, height, mat);
            if (height < 0.01f) RemoveCollider(go);
        }

        static void BuildCafe(Transform env, MaterialLibrary m, TMP_FontAsset font)
        {
            var cafe = Group("Cafe", env);
            var wall = m.Lit("PP_WallCafe", new Color(0.86f, 0.8f, 0.68f));
            // 벽 틈: 너비 30cm, 높이 25cm, 깊이 40cm (쥐 지름 약 8cm, 눈높이 12cm)
            BoxXZ("Facade_South", cafe, -3.4f, -3.0f, -1.0f, -0.55f, 0f, 7f, wall);
            BoxXZ("Facade_CrackLintel", cafe, -3.4f, -3.0f, -0.55f, -0.25f, 0.25f, 7f, wall);
            BoxXZ("Facade_Main", cafe, -3.4f, -3.0f, -0.25f, 7.0f, 0f, 7f, wall);
            BoxXZ("Crack_Back", cafe, -3.7f, -3.4f, -0.6f, -0.2f, 0f, 0.3f, m.Lit("PP_WallDark", new Color(0.35f, 0.3f, 0.27f)));
            SafeZone("SafeZone_WallCrack", cafe, new Vector3(-3.2f, 0.12f, -0.4f), new Vector3(0.4f, 0.24f, 0.3f), "벽 틈", false, m);
            BoxXZ("SouthEndWall", cafe, -3.7f, 4.4f, -1.4f, -1.0f, 0f, 4f, wall);

            // 가게 전면, 차양, 간판
            BoxXZ("Shopfront", cafe, -3.03f, -2.99f, 0.1f, 6.6f, 0.35f, 2.45f, m.Lit("PP_ShopRed", new Color(0.45f, 0.12f, 0.1f)));
            BoxXZ("Shopfront_Glass", cafe, -3.04f, -3.02f, 0.4f, 6.3f, 0.6f, 2.25f, m.Lit("PP_Glass", new Color(0.55f, 0.68f, 0.74f)));
            BoxXZ("Awning", cafe, -3.0f, -1.75f, 0.1f, 6.6f, 2.55f, 2.6f, m.Lit("PP_AwningRed", new Color(0.7f, 0.15f, 0.14f)));
            BoxXZ("SignBoard", cafe, -2.99f, -2.96f, 2.4f, 4.4f, 2.7f, 3.1f, m.Lit("PP_SignDark", new Color(0.12f, 0.12f, 0.12f)));
            var face = new Vector3(10f, 0f, 3.4f);
            Label("Sign_Cafe", cafe, new Vector3(-2.94f, 2.9f, 3.4f), PetitParisText.SignCafeLatin + "  " + PetitParisText.SignCafe, 2.2f, 2f, new Color(1f, 0.9f, 0.6f), font, face);
            Windows(cafe, -3.0f, 1f, 0.2f, 7.0f, new[] { 3.5f, 5.3f }, m);
            BoxXZ("Roof_Mansard", cafe, -3.6f, -3.2f, -1.0f, 7.0f, 7f, 8.2f, m.Lit("PP_RoofSlate", new Color(0.32f, 0.35f, 0.4f)));
        }

        static void BuildBakery(Transform env, MaterialLibrary m, TMP_FontAsset font)
        {
            var bakery = Group("Bakery", env);
            var wall = m.Lit("PP_WallBakery", new Color(0.9f, 0.82f, 0.6f));
            BoxXZ("Facade", bakery, -3.4f, -3.0f, 7.0f, 13.0f, 0f, 7.5f, wall);
            BoxXZ("Shopfront", bakery, -3.03f, -2.99f, 7.4f, 12.6f, 0.35f, 2.4f, m.Lit("PP_ShopBlue", new Color(0.15f, 0.25f, 0.45f)));
            BoxXZ("Shopfront_Glass", bakery, -3.04f, -3.02f, 7.7f, 12.3f, 0.6f, 2.2f, m.Lit("PP_GlassWarm", new Color(0.85f, 0.75f, 0.55f)));
            BoxXZ("Awning", bakery, -3.0f, -1.9f, 7.4f, 12.6f, 2.5f, 2.55f, m.Lit("PP_AwningBlue", new Color(0.2f, 0.32f, 0.6f)));
            BoxXZ("SignBoard", bakery, -2.99f, -2.96f, 8.6f, 11.4f, 2.65f, 3.15f, m.Lit("PP_SignCream", new Color(0.95f, 0.9f, 0.78f)));
            Label("Sign_Bakery", bakery, new Vector3(-2.94f, 2.9f, 10.0f), PetitParisText.SignBakeryLatin + "  " + PetitParisText.SignBakery, 2.0f, 2.8f,
                new Color(0.2f, 0.25f, 0.5f), font, new Vector3(10f, 0f, 10.0f));
            Windows(bakery, -3.0f, 1f, 7.2f, 13.0f, new[] { 3.6f, 5.4f }, m);
            BoxXZ("Roof_Mansard", bakery, -3.6f, -3.2f, 7.0f, 13.0f, 7.5f, 8.6f, m.Lit("PP_RoofSlate", new Color(0.32f, 0.35f, 0.4f)));
        }

        static void BuildAlleyAndCrossStreet(Transform env, MaterialLibrary m)
        {
            var alley = Group("Alley", env);
            var stoneA = m.Lit("PP_WallAlleyA", new Color(0.78f, 0.74f, 0.66f));
            var stoneB = m.Lit("PP_WallAlleyB", new Color(0.7f, 0.66f, 0.6f));
            // 빵집과 골목 사이 블록
            BoxXZ("Block_West_South", alley, -7.6f, -1.7f, 13.0f, 15.6f, 0f, 6f, stoneA);
            BoxXZ("Block_East_South", alley, 0.7f, 7.6f, 13.3f, 15.6f, 0f, 6f, stoneB);
            // 자전거 길 (가로 골목) 양쪽 건물
            BoxXZ("CrossStreet_SouthWest", alley, -7.6f, -1.7f, 15.6f, 16.4f, 0f, 5f, stoneB);
            BoxXZ("CrossStreet_SouthEast", alley, 0.7f, 7.6f, 15.6f, 16.4f, 0f, 5f, stoneA);
            BoxXZ("CrossStreet_NorthWest", alley, -7.6f, -1.7f, 17.6f, 18.4f, 0f, 5f, stoneA);
            BoxXZ("CrossStreet_NorthEast", alley, 0.7f, 7.6f, 17.6f, 18.4f, 0f, 5f, stoneB);
            BoxXZ("CrossStreet_EndWest", alley, -8f, -7.6f, 15.6f, 18.4f, 0f, 5f, stoneB);
            BoxXZ("CrossStreet_EndEast", alley, 7.6f, 8f, 15.6f, 18.4f, 0f, 5f, stoneB);
            // 북쪽 골목 벽
            BoxXZ("Alley_West_North", alley, -4.4f, -1.7f, 18.4f, 23.0f, 0f, 6f, stoneB);
            BoxXZ("Alley_East_North", alley, 0.7f, 4.8f, 18.4f, 23.0f, 0f, 6f, stoneA);
            Windows(alley, -1.7f, 1f, 18.6f, 23.0f, new[] { 3.0f, 4.8f }, m);
            Windows(alley, 0.7f, -1f, 18.6f, 23.0f, new[] { 3.0f, 4.8f }, m);
            Windows(alley, -1.7f, 1f, 13.2f, 15.6f, new[] { 3.0f, 4.8f }, m);
            Windows(alley, 0.7f, -1f, 13.4f, 15.6f, new[] { 3.0f, 4.8f }, m);
        }

        static void BuildEastFacade(Transform env, MaterialLibrary m)
        {
            var east = Group("EastBuildings", env);
            BoxXZ("Facade_East", east, 4.0f, 4.4f, -1.0f, 13.3f, 0f, 8f, m.Lit("PP_WallEast", new Color(0.82f, 0.78f, 0.72f)));
            BoxXZ("Shopfront_East", east, 3.98f, 4.02f, 0.5f, 12.5f, 0.3f, 2.4f, m.Lit("PP_ShopGreen", new Color(0.16f, 0.3f, 0.22f)));
            Windows(east, 4.0f, -1f, -0.8f, 13.3f, new[] { 3.5f, 5.3f, 7.0f }, m);
            BoxXZ("Roof_East", east, 4.2f, 4.6f, -1.0f, 13.3f, 8f, 9.2f, m.Lit("PP_RoofSlate", new Color(0.32f, 0.35f, 0.4f)));
        }

        static void BuildQuaiAndSeine(Transform env, MaterialLibrary m)
        {
            var quai = Group("Quai", env);
            var stone = m.Lit("PP_QuaiStone", new Color(0.76f, 0.72f, 0.62f));
            BoxXZ("Quai_WestWall", quai, -4.4f, -4.0f, 23.0f, 25.9f, 0f, 1.0f, stone);
            BoxXZ("Quai_EastWall", quai, 4.4f, 4.8f, 23.0f, 25.9f, 0f, 1.0f, stone);
            BoxXZ("Parapet_West", quai, -4.0f, -1.0f, 25.6f, 25.9f, 0f, 0.45f, stone);
            BoxXZ("Parapet_East", quai, 0.0f, 4.4f, 25.6f, 25.9f, 0f, 0.45f, stone);
            // 난간 틈의 보이지 않는 벽 (떨어짐 방지)
            var invisible = BoxXZ("Parapet_Gap_Invisible", quai, -1.0f, 0.0f, 25.85f, 25.9f, 0f, 0.4f, stone);
            invisible.GetComponent<MeshRenderer>().enabled = false;

            var water = m.Unlit("PP_SeineWater", new Color(0.28f, 0.45f, 0.55f));
            RemoveCollider(BoxXZ("Seine_Water", quai, -40f, 40f, 25.9f, 80f, -1.2f, -0.9f, water));
            var far = m.Unlit("PP_FarStone", new Color(0.7f, 0.7f, 0.68f));
            var farHouse = m.Unlit("PP_FarHouses", new Color(0.76f, 0.78f, 0.82f));
            RemoveCollider(BoxXZ("FarBank", quai, -40f, 40f, 60f, 64f, -1f, 0.6f, far));
            float[] heights = { 9f, 12f, 8f, 14f, 10f, 11f, 7f };
            for (int i = 0; i < heights.Length; i++)
                RemoveCollider(BoxXZ($"FarHouse_{i}", quai, -30f + i * 9f, -23f + i * 9f, 64f, 70f, 0.6f, heights[i], farHouse));
            RemoveCollider(BoxXZ("Bridge_Deck", quai, -40f, 40f, 40f, 42.5f, 2.0f, 2.6f, far));
            foreach (float x in new[] { -20f, -8f, 4f, 16f })
                RemoveCollider(BoxXZ($"Bridge_Pier_{x}", quai, x, x + 1.5f, 40.2f, 42.3f, -1f, 2.0f, far));
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
            Crate("Crate_A2", props, new Vector3(-1.45f, 0f, 20.0f), Vector3.one * 0.35f, 0f, m);
            Crate("Crate_A3", props, new Vector3(-1.45f, 0.35f, 20.0f), Vector3.one * 0.3f, 15f, m);
            Cylinder("TrashBin", props, new Vector3(0.42f, 0.375f, 20.6f), 0.42f, 0.75f, m.Lit("PP_BinGreen", new Color(0.2f, 0.32f, 0.22f)));
            PaperCup("Cup_A2", props, new Vector3(-0.2f, 0f, 21.8f), m);
        }

        /// <summary>건물 정면의 창문 (facing: +1 = +X를 향함, -1 = -X를 향함).</summary>
        static void Windows(Transform parent, float facadeX, float facing, float z0, float z1, float[] rows, MaterialLibrary m)
        {
            var glass = m.Lit("PP_WindowGlass", new Color(0.22f, 0.27f, 0.33f));
            var balcony = m.Lit("PP_BalconyIron", new Color(0.1f, 0.1f, 0.12f));
            float x = facadeX + facing * 0.02f;
            var group = Group($"Windows_{facadeX:0.0}_{z0:0.0}", parent);
            for (float z = z0 + 0.8f; z < z1 - 0.5f; z += 1.6f)
            {
                foreach (float y in rows)
                {
                    RemoveCollider(Box("Window", group, new Vector3(x, y, z), new Vector3(0.04f, 1.2f, 0.85f), glass));
                    RemoveCollider(Box("Balcony", group, new Vector3(facadeX + facing * 0.12f, y - 0.62f, z), new Vector3(0.22f, 0.05f, 1.05f), balcony));
                }
            }
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
