using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PetitParis.EditorTools.PetitParisBuildKit;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 재미 요소를 씬에 만드는 도구: 빵 부스러기, 말풍선, 알아채는 보행자, 빗자루, 비둘기, 떨어지는 바게트, 널빤지.
    /// 좌표는 월드 미터(실제 크기).
    /// </summary>
    public static class PetitParisGameplayKit
    {
        // ───────────────────────── 빵 부스러기 ─────────────────────────

        public static int PointsFor(CrumbPickup.Kind kind) => kind switch
        {
            CrumbPickup.Kind.Crumb => 1,
            CrumbPickup.Kind.Croissant => 3,
            CrumbPickup.Kind.Cheese => 5,
            _ => 10,
        };

        /// <summary>
        /// 주울 수 있는 조각. groundPosition = 바닥(또는 널빤지 윗면) 위치. 모양은 바닥에서 약 4cm 떠 있다.
        /// 크기: 부스러기 2.5cm, 크루아상 7cm, 치즈 5cm, 황금 크루아상 9cm (쥐 눈높이 12cm 기준으로 잘 보이게).
        /// </summary>
        public static CrumbPickup Crumb(string name, Transform parent, Vector3 groundPosition, CrumbPickup.Kind kind, MaterialLibrary m)
        {
            var root = Group(name, parent);
            root.position = groundPosition + Vector3.up * 0.04f;
            var visual = Group("Visual", root);
            var crust = m.Lit("PP_Crust", new Color(0.86f, 0.58f, 0.26f));
            var crumbMat = m.Lit("PP_CrumbInside", new Color(0.98f, 0.86f, 0.6f));
            var gold = m.Unlit("PP_GoldenCroissant", new Color(1f, 0.82f, 0.25f));

            switch (kind)
            {
                case CrumbPickup.Kind.Crumb:
                    RemoveCollider(Box("Crumb", visual, Vector3.zero, new Vector3(0.026f, 0.018f, 0.022f), crust)).transform.localRotation = Quaternion.Euler(12f, 30f, 8f);
                    RemoveCollider(Box("Crumb2", visual, new Vector3(0.012f, -0.004f, 0.006f), new Vector3(0.014f, 0.012f, 0.012f), crumbMat));
                    break;
                case CrumbPickup.Kind.Croissant:
                case CrumbPickup.Kind.GoldenCroissant:
                {
                    bool golden = kind == CrumbPickup.Kind.GoldenCroissant;
                    float s = golden ? 1.3f : 1f;
                    var mat = golden ? gold : crust;
                    // 초승달: 가운데 큰 덩어리 + 양쪽으로 휘어 작아지는 덩어리
                    for (int i = -2; i <= 2; i++)
                    {
                        float a = i * 28f * Mathf.Deg2Rad;
                        float r = 0.028f * s;
                        float size = (0.034f - Mathf.Abs(i) * 0.007f) * s;
                        var seg = RemoveCollider(Sphere($"Seg{i}", visual, new Vector3(Mathf.Sin(a) * r, 0f, -Mathf.Cos(a) * r + r * 0.7f), size, mat));
                        seg.transform.localScale = new Vector3(size * 1.2f, size * 0.8f, size);
                    }
                    if (golden)
                    {
                        // 빛나는 후광 (바닥 위 둥근 빛)
                        var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        glow.name = "Glow";
                        RemoveCollider(glow);
                        glow.transform.SetParent(root, false);
                        glow.transform.localPosition = new Vector3(0f, -0.035f, 0f);
                        glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        glow.transform.localScale = Vector3.one * 0.2f;
                        var r = glow.GetComponent<MeshRenderer>();
                        r.sharedMaterial = m.Blob("PP_GoldGlow", new Color(1f, 0.85f, 0.3f, 0.55f));
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    break;
                }
                case CrumbPickup.Kind.Cheese:
                {
                    var cheese = m.Lit("PP_CheeseYellow", new Color(1f, 0.8f, 0.28f));
                    var wedge = RemoveCollider(Box("Wedge", visual, Vector3.zero, new Vector3(0.05f, 0.032f, 0.035f), cheese));
                    wedge.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    RemoveCollider(Sphere("Hole", visual, new Vector3(0.012f, 0.012f, -0.018f), 0.01f, m.Lit("PP_CheeseHole", new Color(0.85f, 0.62f, 0.18f))));
                    break;
                }
            }

            var pickup = root.gameObject.AddComponent<CrumbPickup>();
            var so = new SerializedObject(pickup);
            SetInt(so, "kind", (int)kind);
            SetInt(so, "points", PointsFor(kind));
            SetObject(so, "visual", visual);
            SetFloat(so, "pickupRadius", kind == CrumbPickup.Kind.Crumb ? 0.07f : 0.085f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return pickup;
        }

        // ───────────────────────── 말풍선·알아채는 보행자 ─────────────────────────

        /// <summary>머리 위 말풍선 (흰 판 + 검은 글자). TMP 오브젝트를 켜고 끄면 판도 함께 보인다.</summary>
        public static TextMeshPro SpeechBubble(Transform body, float height, TMP_FontAsset font, MaterialLibrary m)
        {
            var go = new GameObject("SpeechBubble", typeof(RectTransform));
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = "!";
            tmp.fontSize = 2.2f;
            tmp.color = new Color(0.12f, 0.08f, 0.06f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(1.5f, 0.55f);

            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BubbleBackground";
            RemoveCollider(bg);
            bg.transform.SetParent(go.transform, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            bg.transform.localScale = new Vector3(1.65f, 0.62f, 1f);
            var r = bg.GetComponent<MeshRenderer>();
            r.sharedMaterial = m.Unlit("PP_BubbleWhite", new Color(1f, 0.98f, 0.92f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            return tmp;
        }

        /// <summary>보행자에 "쥐를 알아채는" 반응을 붙인다.</summary>
        public static PedestrianAwareness Awareness(PedestrianPath path, string shout, float noticeRadius, float chaseSpeed,
            TMP_FontAsset font, MaterialLibrary m, Bounds? customArea = null)
        {
            var body = path.Body;
            var bubble = SpeechBubble(body != null ? body : path.transform, 2.15f, font, m);
            var voice = (body != null ? body : path.transform).gameObject.AddComponent<AudioSource>();
            var aw = path.gameObject.AddComponent<PedestrianAwareness>();
            var so = new SerializedObject(aw);
            SetObject(so, "bubble", bubble);
            SetObject(so, "voice", voice);
            SetString(so, "shout", shout);
            SetFloat(so, "noticeRadius", noticeRadius);
            SetFloat(so, "chaseSpeed", chaseSpeed);
            if (customArea.HasValue)
            {
                SetBool(so, "useCustomArea", true);
                var p = so.FindProperty("customArea");
                if (p != null) p.boundsValue = customArea.Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return aw;
        }

        // ───────────────────────── 빗자루 ─────────────────────────

        /// <summary>보행자 몸(Body)에 빗자루를 쥐여 준다. 빗자루 머리는 바닥을 쓸며, 닿으면 실패(FootstepHazard).</summary>
        public static BroomSweep Broom(PedestrianPath path, MaterialLibrary m)
        {
            var body = path.Body;
            var pivot = Group("BroomPivot", body);
            pivot.localPosition = new Vector3(0.12f, 1.0f, 0.22f); // 손 위치
            var wood = m.Lit("PP_BroomHandle", new Color(0.55f, 0.36f, 0.2f));
            var straw = m.Lit("PP_BroomStraw", new Color(0.86f, 0.72f, 0.38f));

            Vector3 headLocal = new Vector3(0f, -0.965f, 0.55f);
            var handle = RemoveCollider(Cylinder("Handle", pivot, headLocal * 0.5f, 0.03f, headLocal.magnitude, wood));
            handle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, -headLocal.normalized);

            var head = Box("BroomHead", pivot, headLocal + new Vector3(0f, 0.01f, 0f), new Vector3(0.42f, 0.07f, 0.1f), straw);
            head.GetComponent<BoxCollider>().isTrigger = true;
            var rb = head.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var hazard = head.AddComponent<FootstepHazard>();
            var hso = new SerializedObject(hazard);
            SetString(hso, "hazardName", "빗자루");
            SetFloat(hso, "warningRadius", 0.35f);
            hso.ApplyModifiedPropertiesWithoutUndo();

            var audio = head.AddComponent<AudioSource>();
            var sweep = path.gameObject.AddComponent<BroomSweep>();
            Wire(sweep, ("pivot", pivot), ("sweepAudio", audio));
            return sweep;
        }

        // ───────────────────────── 비둘기 ─────────────────────────

        public static PigeonFlock Pigeons(string name, Transform parent, Vector3 center, int count, float spread, MaterialLibrary m, int seed)
        {
            var root = Group(name, parent);
            root.position = center;
            var rng = new System.Random(seed);
            var grey = m.Lit("PP_PigeonGrey", new Color(0.52f, 0.54f, 0.6f));
            var dark = m.Lit("PP_PigeonDark", new Color(0.3f, 0.32f, 0.38f));
            var neck = m.Lit("PP_PigeonNeck", new Color(0.35f, 0.5f, 0.45f));
            var beak = m.Lit("PP_PigeonBeak", new Color(0.9f, 0.55f, 0.3f));

            var flock = root.gameObject.AddComponent<PigeonFlock>();
            var so = new SerializedObject(flock);
            var birds = so.FindProperty("birds");
            birds.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt((float)rng.NextDouble()) * spread;
                var bird = Group($"Pigeon_{i}", root);
                bird.position = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                bird.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                // 실제 비둘기 크기 (몸길이 약 30cm) → 쥐에게는 커다란 새
                var b = RemoveCollider(Sphere("Body", bird, new Vector3(0f, 0.13f, 0f), 0.2f, grey));
                b.transform.localScale = new Vector3(0.15f, 0.14f, 0.26f);
                RemoveCollider(Sphere("Neck", bird, new Vector3(0f, 0.2f, 0.09f), 0.09f, neck));
                var head = Group("HeadPivot", bird);
                head.localPosition = new Vector3(0f, 0.25f, 0.12f);
                RemoveCollider(Sphere("Head", head, Vector3.zero, 0.07f, grey));
                var bk = RemoveCollider(Box("Beak", head, new Vector3(0f, -0.005f, 0.045f), new Vector3(0.015f, 0.012f, 0.03f), beak));
                bk.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                var tail = RemoveCollider(Box("Tail", bird, new Vector3(0f, 0.12f, -0.16f), new Vector3(0.09f, 0.02f, 0.1f), dark));
                tail.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                foreach (float x in new[] { -0.03f, 0.03f })
                    RemoveCollider(Box("Leg", bird, new Vector3(x, 0.03f, 0.01f), new Vector3(0.008f, 0.06f, 0.008f), beak));
                var wingL = Group("WingL", bird);
                wingL.localPosition = new Vector3(-0.07f, 0.16f, 0f);
                RemoveCollider(Box("Wing", wingL, new Vector3(-0.07f, 0f, -0.02f), new Vector3(0.15f, 0.015f, 0.17f), dark));
                var wingR = Group("WingR", bird);
                wingR.localPosition = new Vector3(0.07f, 0.16f, 0f);
                RemoveCollider(Box("Wing", wingR, new Vector3(0.07f, 0f, -0.02f), new Vector3(0.15f, 0.015f, 0.17f), dark));

                var e = birds.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("root").objectReferenceValue = bird;
                e.FindPropertyRelative("head").objectReferenceValue = head;
                e.FindPropertyRelative("wingL").objectReferenceValue = wingL;
                e.FindPropertyRelative("wingR").objectReferenceValue = wingR;
            }
            SetObject(so, "flutterAudio", root.gameObject.AddComponent<AudioSource>());
            so.ApplyModifiedPropertiesWithoutUndo();
            return flock;
        }

        // ───────────────────────── 떨어지는 바게트 ─────────────────────────

        /// <summary>구역(월드 x/z 범위) 안에 쥐가 있으면 위에서 바게트가 떨어진다. 떨어진 자리에 보너스 부스러기.</summary>
        public static FallingHazard FallingBaguettes(string name, Transform parent, float x0, float x1, float z0, float z1, float dropHeight,
            int poolSize, MaterialLibrary m)
        {
            var root = Group(name, parent);
            root.position = new Vector3((x0 + x1) * 0.5f, 0.2f, (z0 + z1) * 0.5f);
            var zone = root.gameObject.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(Mathf.Abs(x1 - x0), 0.4f, Mathf.Abs(z1 - z0));

            var crust = m.Lit("PP_BaguetteCrust", new Color(0.82f, 0.52f, 0.22f));
            var score = m.Lit("PP_BaguetteScore", new Color(0.96f, 0.82f, 0.55f));
            var blob = m.Blob("PP_FallShadow", new Color(0.15f, 0f, 0f, 0.6f));
            var items = new Transform[poolSize];
            var shadows = new RatBlobShadow[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                // 진짜 바게트 크기: 길이 60cm, 지름 6cm (쥐에게는 통나무)
                var item = Group($"Baguette_{i}", root);
                var loaf = Box("Loaf", item, Vector3.zero, new Vector3(0.065f, 0.6f, 0.065f), crust);
                loaf.GetComponent<BoxCollider>().isTrigger = true;
                var rb = loaf.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                var hazard = loaf.AddComponent<FootstepHazard>();
                var hso = new SerializedObject(hazard);
                SetString(hso, "hazardName", "바게트");
                SetFloat(hso, "warningRadius", 0.15f);
                hso.ApplyModifiedPropertiesWithoutUndo();
                for (int k = -2; k <= 2; k++)
                {
                    var cut = RemoveCollider(Box("Score", item, new Vector3(0.03f, k * 0.11f, 0f), new Vector3(0.012f, 0.06f, 0.035f), score));
                    cut.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                }
                RemoveCollider(Sphere("TipA", item, new Vector3(0f, 0.3f, 0f), 0.06f, crust));
                RemoveCollider(Sphere("TipB", item, new Vector3(0f, -0.3f, 0f), 0.06f, crust));
                item.gameObject.SetActive(false);
                items[i] = item;

                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = $"FallShadow_{i}";
                RemoveCollider(q);
                q.transform.SetParent(root, false);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = blob;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                shadows[i] = q.AddComponent<RatBlobShadow>();
            }

            var template = Crumb("BonusCrumbTemplate", root, root.position + Vector3.down * 0.2f, CrumbPickup.Kind.Crumb, m);
            template.gameObject.SetActive(false);

            var fall = root.gameObject.AddComponent<FallingHazard>();
            var so = new SerializedObject(fall);
            SetObject(so, "zone", zone);
            SetObjectArray(so, "items", items);
            SetObjectArray(so, "shadows", shadows);
            SetFloat(so, "dropHeight", dropHeight);
            SetObject(so, "crumbTemplate", template);
            // 소리 위치를 낙하 지점으로 옮기므로 구역(root)이 아닌 별도 자식에 둔다 (root에 두면 구역째 움직인다)
            var audioGO = new GameObject("DropAudio");
            audioGO.transform.SetParent(root, false);
            SetObject(so, "audioSource", audioGO.AddComponent<AudioSource>());
            so.ApplyModifiedPropertiesWithoutUndo();
            return fall;
        }

        // ───────────────────────── 널빤지 (오르막) ─────────────────────────

        /// <summary>
        /// 두 점을 잇는 널빤지. fromTop/toTop = 널빤지 윗면 중심선의 양 끝 (월드).
        /// 양 끝을 조금 늘려 바닥·상자 윗면과 틈이 없게 한다. 쥐가 걸어 올라갈 수 있는 경사(약 30도 이하)로 놓는다.
        /// </summary>
        public static GameObject Plank(string name, Transform parent, Vector3 fromTop, Vector3 toTop, float width, Material mat)
        {
            const float thickness = 0.025f, overlap = 0.03f;
            Vector3 d = toTop - fromTop;
            float length = d.magnitude;
            Vector3 dir = d / length;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up); // 윗면이 하늘을 보도록
            Vector3 up = rot * Vector3.up;
            Vector3 center = (fromTop + toTop) * 0.5f - up * (thickness * 0.5f);
            var go = Box(name, parent, Vector3.zero, new Vector3(width, thickness, length + overlap * 2f), mat);
            go.transform.SetPositionAndRotation(center, rot);
            return go;
        }

        /// <summary>널빤지 윗면 위의 점 (t = 0~1).</summary>
        public static Vector3 OnPlank(Vector3 fromTop, Vector3 toTop, float t) => Vector3.Lerp(fromTop, toTop, t);

        /// <summary>나무 상자 더미 (높이 0.3m 상자를 count개 쌓음). x/z 범위는 월드.</summary>
        public static void CrateStack(string name, Transform parent, float x0, float x1, float z0, float z1, int count, MaterialLibrary m)
        {
            var g = Group(name, parent);
            var wood = m.LitTex("PP_CrateWood", PetitParisTextures.Wood(), new Color(1f, 0.9f, 0.75f));
            for (int i = 0; i < count; i++)
            {
                float inset = i % 2 == 1 ? 0.01f : 0f;
                PetitParisParisKit.TexBoxXZ($"Crate_{i}", g, x0 + inset, x1 - inset, z0 + inset, z1 - inset, i * 0.3f, (i + 1) * 0.3f, wood, 0.6f);
            }
        }
    }
}
