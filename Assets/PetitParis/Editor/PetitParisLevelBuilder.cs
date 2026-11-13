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
            BuildProps(env, m);
            BuildParisProps(env, p);
            MarkStatic(env);

            // ── 움직이는 위험 ──
            var npcs = Group("Pedestrians", null);
            Pedestrian("Pedestrian_A_Waiter", npcs,
                new[] { new Vector3(-2.5f, 0f, 0.2f), new Vector3(-2.5f, 0f, 6.9f), new Vector3(0.25f, 0f, 6.9f), new Vector3(0.25f, 0f, 0.2f) },
                PedestrianPath.LoopMode.Loop, 0.9f, 3f, new Color(0.25f, 0.3f, 0.45f), m, PedestrianStyle.Waiter);
            Pedestrian("Pedestrian_B_Chef", npcs,
                new[] { new Vector3(-2.2f, 0f, 7.6f), new Vector3(0.3f, 0f, 8.7f), new Vector3(-2.2f, 0f, 9.8f),
                        new Vector3(0.3f, 0f, 10.9f), new Vector3(-2.2f, 0f, 12.0f), new Vector3(0.2f, 0f, 12.9f) },
                PedestrianPath.LoopMode.PingPong, 0.8f, 0f, new Color(0.55f, 0.32f, 0.25f), m, PedestrianStyle.Chef);
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

            // 내비게이션 화살표 경로 (시작 → 의자 옆 안전한 길 → 체크포인트들 → 출구)
            var route = Group("GuideRoute", gameplay);
            Vector3[] routePoints =
            {
                new Vector3(-2.75f, 0f, -0.4f), new Vector3(-2.2f, 0f, 0.55f), new Vector3(-1.15f, 0f, 1.0f), new Vector3(-1.15f, 0f, 5.9f),
                new Vector3(-1.5f, 0f, 6.08f), new Vector3(-1.15f, 0f, 6.6f), new Vector3(-1.3f, 0f, 7.6f), new Vector3(-2.45f, 0f, 9.4f),
                new Vector3(-2.7f, 0f, 10.0f), new Vector3(-2.4f, 0f, 11.0f), new Vector3(-1.2f, 0f, 12.6f), new Vector3(-0.5f, 0f, 13.8f),
                new Vector3(-1.1f, 0f, 14.6f), new Vector3(-1.35f, 0f, 15.0f), new Vector3(-0.5f, 0f, 15.8f), new Vector3(-0.5f, 0f, 18.2f),
                new Vector3(-0.5f, 0f, 21.0f), new Vector3(-0.5f, 0f, 23.8f),
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
            Wire(rig.statusPanel, ("gameFlow", flow), ("checkpoints", manager));
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
            SafeZone("SafeZone_WallCrack", cafe, new Vector3(-3.2f, 0.12f, -0.4f), new Vector3(0.4f, 0.24f, 0.3f), "벽 틈", false, m);

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
            TexBoxXZ("Block_NorthWest", alley, -7.6f, -1.7f, 17.6f, 23.0f, 0f, northSpec.TopY, p.plasterCream, 2f);
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
            Crate("Crate_A2", props, new Vector3(-1.45f, 0f, 20.0f), Vector3.one * 0.35f, 0f, m);
            Crate("Crate_A3", props, new Vector3(-1.45f, 0.35f, 20.0f), Vector3.one * 0.3f, 15f, m);
            Cylinder("TrashBin", props, new Vector3(0.42f, 0.375f, 20.6f), 0.42f, 0.75f, m.Lit("PP_BinGreen", new Color(0.2f, 0.32f, 0.22f)));
            PaperCup("Cup_A2", props, new Vector3(-0.2f, 0f, 21.8f), m);
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
