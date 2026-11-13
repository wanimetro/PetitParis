using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static PetitParis.EditorTools.PetitParisBuildKit;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 파리 거리 블록아웃 생성 도구 (외부 모델 없이 코드 + 자체 생성 질감).
    /// - 질감 좌표를 월드 미터 기준으로 만든 상자(TexBox) → 크기가 달라도 질감이 늘어나지 않음
    /// - 오스만 양식 정면: 창문·창틀·코니스·철제 발코니·덧창·꽃상자·망사르드 지붕·지붕창·굴뚝
    /// - 가게 정면: 쇼윈도(따뜻한 빛), 간판, 줄무늬 차양
    /// - 소품: 파리 가로등, 테라스 줄 전구, 모리스 기둥(광고탑), 파란 거리 표지판
    /// - 원경: 해 질 녘 하늘, 안개, 강물, 먼 지붕선과 돔, 금빛 에펠탑 실루엣
    /// 좌표 단위는 월드 미터(실제 크기).
    /// </summary>
    public static class PetitParisParisKit
    {
        // ───────────────────────── 재질 모음 ─────────────────────────

        public class Palette
        {
            public Material cobble, cobbleDark, slabs, slabsWarm, asphalt, limestone, rustic, plasterCream, plasterOchre, plasterPink;
            public Material zinc, wood, brick, trim, iron, terracotta, awningRed, awningRedTop, awningBlue, awningBlueTop, awningGreen, awningGreenTop;
            public Material windowLit, windowDark, shopWindow, geraniums, posters, morrisGreen, signBlue, signWhite, shopRed, shopBlue, shopGreen, shopBrown;
            public Material lampGlow, bulb, gold, water, cityLights, farHaze, farDome, eiffel, shutterGreen, shutterBlue, darkBack;

            public static Palette Create(MaterialLibrary m)
            {
                var p = new Palette();
                p.cobble = m.LitTex("PP_Cobble", PetitParisTextures.Cobblestone(), Color.white);
                p.cobbleDark = m.LitTex("PP_CobbleDark", PetitParisTextures.Cobblestone(), new Color(0.82f, 0.8f, 0.82f));
                p.slabs = m.LitTex("PP_Slabs", PetitParisTextures.SidewalkSlabs(), Color.white);
                p.slabsWarm = m.LitTex("PP_SlabsWarm", PetitParisTextures.SidewalkSlabs(), new Color(1f, 0.93f, 0.85f));
                p.asphalt = m.LitTex("PP_AsphaltTex", PetitParisTextures.Asphalt(), Color.white);
                p.limestone = m.LitTex("PP_Limestone", PetitParisTextures.Limestone(), Color.white);
                p.rustic = m.LitTex("PP_LimestoneRustic", PetitParisTextures.LimestoneRustic(), Color.white);
                p.plasterCream = m.LitTex("PP_PlasterCream", PetitParisTextures.Plaster("PP_PlasterCreamTex", new Color(0.9f, 0.84f, 0.72f), 1), Color.white);
                p.plasterOchre = m.LitTex("PP_PlasterOchre", PetitParisTextures.Plaster("PP_PlasterOchreTex", new Color(0.86f, 0.68f, 0.45f), 2), Color.white);
                p.plasterPink = m.LitTex("PP_PlasterPink", PetitParisTextures.Plaster("PP_PlasterPinkTex", new Color(0.88f, 0.7f, 0.66f), 3), Color.white);
                p.zinc = m.LitTex("PP_ZincRoof", PetitParisTextures.Zinc(), Color.white);
                p.wood = m.LitTex("PP_WoodTex", PetitParisTextures.Wood(), Color.white);
                p.brick = m.LitTex("PP_BrickTex", PetitParisTextures.Brick(), Color.white);
                p.trim = m.LitTex("PP_StoneTrim", PetitParisTextures.Limestone(), new Color(1.05f, 1.02f, 0.98f));
                p.iron = m.Lit("PP_Iron", new Color(0.09f, 0.11f, 0.1f));
                p.terracotta = m.Lit("PP_Terracotta", new Color(0.72f, 0.38f, 0.24f));
                p.awningRed = m.LitTex("PP_AwningRed", PetitParisTextures.Stripes("PP_StripesRedU", new Color(0.72f, 0.12f, 0.12f), new Color(0.95f, 0.9f, 0.8f), 4, false), Color.white);
                p.awningRedTop = m.LitTex("PP_AwningRedTop", PetitParisTextures.Stripes("PP_StripesRedV", new Color(0.72f, 0.12f, 0.12f), new Color(0.95f, 0.9f, 0.8f), 4, true), Color.white);
                p.awningBlue = m.LitTex("PP_AwningBlue", PetitParisTextures.Stripes("PP_StripesBlueU", new Color(0.16f, 0.26f, 0.55f), new Color(0.95f, 0.92f, 0.85f), 4, false), Color.white);
                p.awningBlueTop = m.LitTex("PP_AwningBlueTop", PetitParisTextures.Stripes("PP_StripesBlueV", new Color(0.16f, 0.26f, 0.55f), new Color(0.95f, 0.92f, 0.85f), 4, true), Color.white);
                p.awningGreen = m.LitTex("PP_AwningGreen", PetitParisTextures.Stripes("PP_StripesGreenU", new Color(0.12f, 0.36f, 0.24f), new Color(0.95f, 0.92f, 0.85f), 4, false), Color.white);
                p.awningGreenTop = m.LitTex("PP_AwningGreenTop", PetitParisTextures.Stripes("PP_StripesGreenV", new Color(0.12f, 0.36f, 0.24f), new Color(0.95f, 0.92f, 0.85f), 4, true), Color.white);
                // 창문·쇼윈도·조명은 Unlit → 해 질 녘에도 스스로 빛나 보인다
                p.windowLit = m.UnlitTex("PP_WindowLitMat", PetitParisTextures.WindowLit(), Color.white);
                p.windowDark = m.LitTex("PP_WindowDarkMat", PetitParisTextures.WindowDark(), Color.white);
                p.shopWindow = m.UnlitTex("PP_ShopWindowMat", PetitParisTextures.ShopWindow(), Color.white);
                p.geraniums = m.LitTex("PP_GeraniumsMat", PetitParisTextures.Geraniums(), Color.white);
                p.posters = m.LitTex("PP_PostersMat", PetitParisTextures.Posters(), Color.white);
                p.morrisGreen = m.Lit("PP_MorrisGreen", new Color(0.12f, 0.24f, 0.18f));
                p.signBlue = m.Lit("PP_SignBlue", new Color(0.1f, 0.2f, 0.5f));
                p.signWhite = m.Lit("PP_SignWhite", new Color(0.92f, 0.92f, 0.9f));
                p.shopRed = m.Lit("PP_ShopPaintRed", new Color(0.45f, 0.08f, 0.08f));
                p.shopBlue = m.Lit("PP_ShopPaintBlue", new Color(0.1f, 0.18f, 0.38f));
                p.shopGreen = m.Lit("PP_ShopPaintGreen", new Color(0.08f, 0.26f, 0.18f));
                p.shopBrown = m.Lit("PP_ShopPaintBrown", new Color(0.3f, 0.16f, 0.1f));
                p.lampGlow = m.Unlit("PP_LampGlow", new Color(1f, 0.82f, 0.5f));
                p.bulb = m.Unlit("PP_Bulb", new Color(1f, 0.88f, 0.6f));
                p.gold = m.Unlit("PP_GoldLetters", new Color(0.95f, 0.78f, 0.4f));
                p.water = m.UnlitTex("PP_SeineWaterTex", PetitParisTextures.WaterLights(), Color.white);
                p.cityLights = m.UnlitTex("PP_CityLightsMat", PetitParisTextures.CityLights(), Color.white);
                p.farHaze = m.Unlit("PP_FarRoofs", new Color(0.4f, 0.37f, 0.5f));
                p.farDome = m.Unlit("PP_FarDome", new Color(0.62f, 0.58f, 0.66f));
                p.eiffel = m.Unlit("PP_EiffelGlow", new Color(1f, 0.7f, 0.36f));
                p.shutterGreen = m.Lit("PP_ShutterGreen", new Color(0.2f, 0.36f, 0.3f));
                p.shutterBlue = m.Lit("PP_ShutterBlue", new Color(0.3f, 0.4f, 0.55f));
                p.darkBack = m.Lit("PP_DarkBack", new Color(0.12f, 0.1f, 0.09f));
                return p;
            }
        }

        // ───────────────────────── 하늘·빛 (해 질 녘) ─────────────────────────

        /// <summary>해 진 직후: 낮은 분홍빛 해, 라벤더 하늘, 옅은 안개. 실시간 조명은 Directional 1개.</summary>
        public static void SetupDusk(MaterialLibrary m)
        {
            var lightGO = new GameObject("Directional Light (Dusk Sun)");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.66f, 0.5f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Hard;
            lightGO.transform.rotation = Quaternion.Euler(11f, 62f, 0f); // 서남서 낮은 해 → 동쪽 건물 윗부분만 노을빛

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = m.Custom("PP_DuskSky", skyShader, Color.white);
                SetIf(sky, "_SunSize", 0.035f);
                SetIf(sky, "_SunSizeConvergence", 6f);
                SetIf(sky, "_AtmosphereThickness", 1.55f);
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.56f, 0.5f, 0.76f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.36f, 0.32f, 0.36f));
                SetIf(sky, "_Exposure", 1.15f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.64f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.8f, 0.64f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.34f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.62f, 0.7f);
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 120f;
        }

        static void SetIf(Material mat, string prop, float value)
        {
            if (mat.HasProperty(prop)) mat.SetFloat(prop, value);
        }

        // ───────────────────────── 월드 미터 UV 상자 ─────────────────────────

        class MeshData
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(v);
                mesh.SetNormals(n);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(t, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>
        /// 상자 면 하나. Cross(u, v) = 바깥 법선이 되도록 축을 고르면 (0,1,2),(0,2,3)이 바깥에서 보이는 앞면.
        /// tile = 질감 1회 반복 길이(m). fit = true면 면마다 질감을 한 번만 꽉 채운다 (창문·간판).
        /// </summary>
        static void Face(MeshData d, Vector3 center, Vector3 n, Vector3 u, Vector3 vAxis, float su, float sv, float tile, bool fit, Vector2 offset)
        {
            int i = d.v.Count;
            Vector3 hu = u * su * 0.5f, hv = vAxis * sv * 0.5f;
            d.v.Add(center - hu - hv); d.v.Add(center + hu - hv); d.v.Add(center + hu + hv); d.v.Add(center - hu + hv);
            for (int k = 0; k < 4; k++) d.n.Add(n);
            float mu = fit ? 1f : su / tile, mv = fit ? 1f : sv / tile;
            Vector2 o = fit ? Vector2.zero : offset;
            d.uv.Add(o); d.uv.Add(o + new Vector2(mu, 0f)); d.uv.Add(o + new Vector2(mu, mv)); d.uv.Add(o + new Vector2(0f, mv));
            d.t.Add(i); d.t.Add(i + 1); d.t.Add(i + 2);
            d.t.Add(i); d.t.Add(i + 2); d.t.Add(i + 3);
        }

        static void AppendBox(MeshData d, Vector3 c, Vector3 s, float tile, bool fit, Vector3 uvWorld)
        {
            Vector3 h = s * 0.5f;
            Vector2 Off(float a, float b) => new Vector2(a / tile, b / tile);
            Face(d, c + Vector3.right * h.x, Vector3.right, Vector3.back, Vector3.up, s.z, s.y, tile, fit, Off(-uvWorld.z, uvWorld.y));
            Face(d, c + Vector3.left * h.x, Vector3.left, Vector3.forward, Vector3.up, s.z, s.y, tile, fit, Off(uvWorld.z, uvWorld.y));
            Face(d, c + Vector3.forward * h.z, Vector3.forward, Vector3.right, Vector3.up, s.x, s.y, tile, fit, Off(uvWorld.x, uvWorld.y));
            Face(d, c + Vector3.back * h.z, Vector3.back, Vector3.left, Vector3.up, s.x, s.y, tile, fit, Off(-uvWorld.x, uvWorld.y));
            Face(d, c + Vector3.up * h.y, Vector3.up, Vector3.right, Vector3.back, s.x, s.z, tile, fit, Off(uvWorld.x, -uvWorld.z));
            Face(d, c + Vector3.down * h.y, Vector3.down, Vector3.right, Vector3.forward, s.x, s.z, tile, fit, Off(uvWorld.x, uvWorld.z));
        }

        /// <summary>질감이 월드 미터로 반복되는 상자. localCenter/size는 parent 기준.</summary>
        public static GameObject TexBox(string name, Transform parent, Vector3 localCenter, Vector3 size, Material mat, float tile = 1f,
            bool collider = false, Vector3? euler = null, bool fit = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
            var d = new MeshData();
            AppendBox(d, Vector3.zero, size, Mathf.Max(0.01f, tile), fit, localCenter);
            go.AddComponent<MeshFilter>().sharedMesh = d.ToMesh(name);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>();
            return go;
        }

        /// <summary>월드 축 정렬 상자 (x/z/y 범위). parent는 원점·무회전이어야 한다.</summary>
        public static GameObject TexBoxXZ(string name, Transform parent, float x0, float x1, float z0, float z1, float y0, float y1,
            Material mat, float tile, bool collider = true)
        {
            return TexBox(name, parent, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f),
                new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), mat, tile, collider);
        }

        /// <summary>철제 난간 하나를 한 메시로 (윗난간 + 아랫난간 + 살). 충돌체 없음.</summary>
        static GameObject Railing(string name, Transform parent, Vector3 localBottomCenter, float length, float height, Material iron)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localBottomCenter;
            var d = new MeshData();
            AppendBox(d, new Vector3(0f, height, 0f), new Vector3(0.05f, 0.05f, length), 1f, false, Vector3.zero);
            AppendBox(d, new Vector3(0f, 0.04f, 0f), new Vector3(0.04f, 0.04f, length), 1f, false, Vector3.zero);
            AppendBox(d, new Vector3(0f, height * 0.78f, 0f), new Vector3(0.025f, 0.025f, length), 1f, false, Vector3.zero);
            int bars = Mathf.Max(2, Mathf.RoundToInt(length / 0.11f));
            for (int i = 0; i <= bars; i++)
            {
                float z = -length * 0.5f + length * i / bars;
                AppendBox(d, new Vector3(0f, height * 0.5f, z), new Vector3(0.018f, height, 0.018f), 1f, false, Vector3.zero);
            }
            go.AddComponent<MeshFilter>().sharedMesh = d.ToMesh(name);
            go.AddComponent<MeshRenderer>().sharedMaterial = iron;
            return go;
        }

        // ───────────────────────── 건물 정면 ─────────────────────────

        public class FacadeSpec
        {
            public int floors = 4;
            public float groundHeight = 4f;
            public float floorHeight = 3f;
            public float windowSpacing = 2f;
            public float litChance = 0.4f;
            public bool shutters;
            public Material shutterMaterial;
            public bool balconies = true;
            public bool flowerBoxes = true;
            public bool mansard = true;
            public int seed = 1;
            public float TopY => groundHeight + floors * floorHeight;
        }

        /// <summary>
        /// 정면 기준 좌표계: 원점 = 정면 중앙의 바닥, +X = 바깥(거리 쪽), +Z = 정면을 따라.
        /// </summary>
        public static Transform FacadeFrame(string name, Transform parent, Vector3 faceCenterGround, Vector3 outward)
        {
            var t = Group(name, parent);
            t.position = faceCenterGround;
            t.rotation = Quaternion.FromToRotation(Vector3.right, new Vector3(outward.x, 0f, outward.z).normalized);
            return t;
        }

        /// <summary>오스만 양식 정면 장식 (창문·창틀·코니스·발코니·덧창·꽃상자·망사르드 지붕). 벽 덩어리는 따로 만든다.</summary>
        public static void FacadeDetails(Transform frame, float length, FacadeSpec spec, Palette p)
        {
            var rng = new System.Random(spec.seed);
            float R() => (float)rng.NextDouble();
            float g = spec.groundHeight, fh = spec.floorHeight, top = spec.TopY;

            TexBox("Cornice_Ground", frame, new Vector3(0.1f, g, 0f), new Vector3(0.26f, 0.22f, length), p.trim, 1.2f);
            int cols = Mathf.Max(1, Mathf.FloorToInt(length / spec.windowSpacing));
            float start = -(cols - 1) * spec.windowSpacing * 0.5f;

            for (int f = 1; f <= spec.floors; f++)
            {
                float y0 = g + (f - 1) * fh;
                bool continuous = spec.balconies && (f == 2 || f == spec.floors) && spec.floors >= 3;
                float wh = f == spec.floors ? 1.5f : 1.9f, ww = 1.0f;
                float wy = y0 + 0.45f + wh * 0.5f;
                if (f > 1) TexBox($"Cornice_{f}", frame, new Vector3(0.05f, y0, 0f), new Vector3(0.12f, 0.09f, length), p.trim, 1.2f);

                for (int c = 0; c < cols; c++)
                {
                    float z = start + c * spec.windowSpacing;
                    bool lit = R() < spec.litChance;
                    TexBox("WindowFrame", frame, new Vector3(0.012f, wy, z), new Vector3(0.04f, wh + 0.2f, ww + 0.2f), p.trim, 1.2f);
                    TexBox("Window", frame, new Vector3(0.036f, wy, z), new Vector3(0.012f, wh, ww), lit ? p.windowLit : p.windowDark, 1f, false, null, true);
                    TexBox("Keystone", frame, new Vector3(0.05f, wy + wh * 0.5f + 0.12f, z), new Vector3(0.08f, 0.2f, 0.22f), p.trim, 1.2f);
                    if (continuous) continue;

                    TexBox("Sill", frame, new Vector3(0.07f, wy - wh * 0.5f - 0.05f, z), new Vector3(0.16f, 0.07f, ww + 0.3f), p.trim, 1.2f);
                    if (spec.shutters)
                    {
                        var shutter = spec.shutterMaterial != null ? spec.shutterMaterial : p.shutterGreen;
                        TexBox("Shutter_L", frame, new Vector3(0.035f, wy, z - ww * 0.5f - 0.28f), new Vector3(0.03f, wh, 0.5f), shutter, 1f);
                        TexBox("Shutter_R", frame, new Vector3(0.035f, wy, z + ww * 0.5f + 0.28f), new Vector3(0.03f, wh, 0.5f), shutter, 1f);
                    }
                    float roll = R();
                    if (spec.flowerBoxes && roll < 0.4f)
                    {
                        TexBox("FlowerBox", frame, new Vector3(0.14f, wy - wh * 0.5f + 0.02f, z), new Vector3(0.2f, 0.16f, ww), p.wood, 0.8f);
                        TexBox("Geraniums", frame, new Vector3(0.14f, wy - wh * 0.5f + 0.15f, z), new Vector3(0.22f, 0.12f, ww + 0.04f), p.geraniums, 0.35f);
                    }
                    else if (spec.balconies && roll < 0.75f)
                    {
                        TexBox("SmallBalcony", frame, new Vector3(0.16f, y0 + 0.42f, z), new Vector3(0.32f, 0.08f, ww + 0.4f), p.trim, 1.2f);
                        Railing("SmallRailing", frame, new Vector3(0.3f, y0 + 0.46f, z), ww + 0.36f, 0.75f, p.iron);
                    }
                }

                if (continuous)
                {
                    TexBox($"Balcony_{f}", frame, new Vector3(0.26f, y0 + 0.4f, 0f), new Vector3(0.52f, 0.11f, length), p.trim, 1.2f);
                    Railing($"Railing_{f}", frame, new Vector3(0.5f, y0 + 0.46f, 0f), length - 0.1f, 0.9f, p.iron);
                }
            }

            TexBox("Cornice_Top", frame, new Vector3(0.16f, top + 0.12f, 0f), new Vector3(0.44f, 0.26f, length + 0.1f), p.trim, 1.2f);
            if (!spec.mansard) return;

            // 망사르드 지붕: 바깥면이 안쪽으로 기울어진 아연판 + 지붕창 + 굴뚝
            TexBox("Mansard", frame, new Vector3(-0.62f, top + 1.7f, 0f), new Vector3(0.22f, 3.3f, length), p.zinc, 1.5f, false, new Vector3(0f, 0f, 22f));
            TexBox("RoofBody", frame, new Vector3(-3.9f, top + 1.55f, 0f), new Vector3(5.6f, 3.1f, length), p.zinc, 1.5f);
            for (int c = 0; c < cols; c += 2)
            {
                float z = start + c * spec.windowSpacing;
                TexBox("Dormer", frame, new Vector3(-0.62f, top + 1.35f, z), new Vector3(0.75f, 1.5f, 1.0f), p.trim, 1.2f);
                TexBox("DormerWindow", frame, new Vector3(-0.235f, top + 1.3f, z), new Vector3(0.012f, 0.95f, 0.62f), R() < 0.5f ? p.windowLit : p.windowDark, 1f, false, null, true);
                TexBox("DormerCap", frame, new Vector3(-0.62f, top + 2.17f, z), new Vector3(0.9f, 0.14f, 1.15f), p.zinc, 1.5f);
            }
            foreach (float zc in new[] { -length / 3f, length / 3f })
            {
                if (length < 3f && zc > 0f) continue;
                TexBox("Chimney", frame, new Vector3(-2.6f, top + 3.8f, zc), new Vector3(0.75f, 1.5f, 1.2f), p.brick, 1f);
                for (int k = -1; k <= 1; k++)
                {
                    var pot = RemoveCollider(Cylinder("ChimneyPot", frame, new Vector3(-2.6f, top + 4.78f, zc + k * 0.32f), 0.2f, 0.46f, p.terracotta));
                    pot.transform.localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>가게 정면 (frame 좌표계, z 범위). 쇼윈도는 따뜻하게 빛나고, 간판 글자와 줄무늬 차양이 붙는다.</summary>
        public static void ShopFront(Transform frame, float zFrom, float zTo, float groundHeight, Material paint, Material awningFront, Material awningTop,
            string sign, TMP_FontAsset font, Palette p)
        {
            float L = Mathf.Abs(zTo - zFrom), zc = (zFrom + zTo) * 0.5f;
            TexBox("Kickplate", frame, new Vector3(0.07f, 0.2f, zc), new Vector3(0.14f, 0.4f, L), paint, 1f, true);
            TexBox("ShopPanel", frame, new Vector3(0.05f, 0.4f + (groundHeight - 1.0f) * 0.5f, zc), new Vector3(0.1f, groundHeight - 1.0f, L), paint, 1f);
            TexBox("ShopGlass", frame, new Vector3(0.106f, 0.55f + (groundHeight - 1.9f) * 0.5f, zc), new Vector3(0.012f, groundHeight - 1.9f, L - 0.5f), p.shopWindow, 1f, false, null, true);
            TexBox("SignBand", frame, new Vector3(0.13f, groundHeight - 0.45f, zc), new Vector3(0.12f, 0.55f, L), paint, 1f);
            var label = Label("Sign", frame, new Vector3(0.2f, groundHeight - 0.45f, zc), sign, 3.2f, L, new Color(0.98f, 0.82f, 0.45f), font, null);
            label.transform.localRotation = Quaternion.LookRotation(Vector3.left);
            // 차양: 바깥쪽이 아래로 18도 기울어짐. 앞 가리개(valance)는 세로 줄무늬
            TexBox("Awning", frame, new Vector3(0.75f, groundHeight - 1.05f, zc), new Vector3(1.45f, 0.04f, L + 0.1f), awningTop, 1f, false, new Vector3(0f, 0f, -18f));
            TexBox("AwningValance", frame, new Vector3(1.44f, groundHeight - 1.4f, zc), new Vector3(0.02f, 0.28f, L + 0.1f), awningFront, 1f);
        }

        // ───────────────────────── 소품 ─────────────────────────

        /// <summary>파리식 주철 가로등 (불빛은 Unlit, 실시간 조명 없음).</summary>
        public static void LampPost(string name, Transform parent, Vector3 position, Palette p)
        {
            var lamp = Group(name, parent);
            lamp.position = position;
            Cylinder("Base", lamp, new Vector3(0f, 0.25f, 0f), 0.24f, 0.5f, p.iron);
            RemoveCollider(Cylinder("Post", lamp, new Vector3(0f, 1.9f, 0f), 0.09f, 2.9f, p.iron));
            TexBox("Lantern", lamp, new Vector3(0f, 3.6f, 0f), new Vector3(0.3f, 0.42f, 0.3f), p.lampGlow, 1f, false, null, true);
            TexBox("LanternCap", lamp, new Vector3(0f, 3.86f, 0f), new Vector3(0.42f, 0.08f, 0.42f), p.iron, 1f);
            TexBox("LanternBase", lamp, new Vector3(0f, 3.36f, 0f), new Vector3(0.22f, 0.06f, 0.22f), p.iron, 1f);
        }

        /// <summary>테라스 위 줄 전구 (a → b, 아래로 처짐). 전구는 Unlit.</summary>
        public static void StringLights(string name, Transform parent, Vector3 a, Vector3 b, float sag, int bulbs, Palette p)
        {
            var root = Group(name, parent);
            Vector3 prev = a;
            for (int i = 0; i <= bulbs; i++)
            {
                float t = i / (float)bulbs;
                Vector3 pos = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t);
                if (i > 0)
                {
                    Vector3 d = pos - prev;
                    var wire = RemoveCollider(Cylinder("Wire", root, Vector3.zero, 0.012f, d.magnitude, p.iron));
                    wire.transform.position = (pos + prev) * 0.5f;
                    wire.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
                }
                if (i > 0 && i < bulbs)
                {
                    var bulb = RemoveCollider(Sphere("Bulb", root, Vector3.zero, 0.07f, p.bulb));
                    bulb.transform.position = pos + Vector3.down * 0.04f;
                }
                prev = pos;
            }
        }

        /// <summary>모리스 기둥 (파리의 원통형 광고탑).</summary>
        public static void MorrisColumn(string name, Transform parent, Vector3 position, Palette p)
        {
            var col = Group(name, parent);
            col.position = position;
            Cylinder("Base", col, new Vector3(0f, 0.15f, 0f), 1.3f, 0.3f, p.morrisGreen);
            Cylinder("Posters", col, new Vector3(0f, 1.6f, 0f), 1.1f, 2.6f, p.posters);
            RemoveCollider(Cylinder("Ring", col, new Vector3(0f, 2.95f, 0f), 1.3f, 0.15f, p.morrisGreen));
            var dome = RemoveCollider(Sphere("Dome", col, new Vector3(0f, 3.05f, 0f), 1.2f, p.morrisGreen));
            dome.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            RemoveCollider(Sphere("Finial", col, new Vector3(0f, 3.42f, 0f), 0.16f, p.morrisGreen));
        }

        /// <summary>파란 법랑 거리 표지판 (frame 좌표계: +X 바깥).</summary>
        public static void StreetSign(string name, Transform frame, Vector3 localPos, string text, TMP_FontAsset font, Palette p)
        {
            TexBox(name + "_Border", frame, localPos, new Vector3(0.02f, 0.34f, 0.74f), p.signWhite, 1f);
            TexBox(name, frame, localPos + new Vector3(0.012f, 0f, 0f), new Vector3(0.02f, 0.28f, 0.68f), p.signBlue, 1f);
            var label = Label(name + "_Text", frame, localPos + new Vector3(0.025f, 0f, 0f), text, 0.75f, 0.64f, Color.white, font, null);
            label.transform.localRotation = Quaternion.LookRotation(Vector3.left);
        }

        /// <summary>멀리 보이는 금빛 에펠탑 실루엣 (Unlit, 저폴리).</summary>
        public static void EiffelSilhouette(string name, Transform parent, Vector3 basePos, float height, Material mat)
        {
            var root = Group(name, parent);
            root.position = basePos;
            float w = height * 0.38f;
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    Vector3 a = new Vector3(sx * w * 0.5f, 0f, sz * w * 0.5f);
                    Vector3 b = new Vector3(sx * w * 0.13f, height * 0.36f, sz * w * 0.13f);
                    var leg = TexBox("Leg", root, (a + b) * 0.5f, new Vector3(w * 0.09f, (b - a).magnitude, w * 0.09f), mat);
                    leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
                }
            TexBox("Platform1", root, new Vector3(0f, height * 0.18f, 0f), new Vector3(w * 0.62f, height * 0.025f, w * 0.62f), mat);
            TexBox("Platform2", root, new Vector3(0f, height * 0.36f, 0f), new Vector3(w * 0.32f, height * 0.02f, w * 0.32f), mat);
            TexBox("Shaft1", root, new Vector3(0f, height * 0.5f, 0f), new Vector3(w * 0.16f, height * 0.28f, w * 0.16f), mat);
            TexBox("Shaft2", root, new Vector3(0f, height * 0.75f, 0f), new Vector3(w * 0.08f, height * 0.24f, w * 0.08f), mat);
            TexBox("Top", root, new Vector3(0f, height * 0.9f, 0f), new Vector3(w * 0.06f, height * 0.04f, w * 0.06f), mat);
            TexBox("Spire", root, new Vector3(0f, height * 0.96f, 0f), new Vector3(w * 0.012f, height * 0.08f, w * 0.012f), mat);
        }

        /// <summary>바닥에 눕힌 납작한 화살표 (+Z가 앞, 앞뒤 면 모두 보임). 길이 약 8cm = 체감 80cm.</summary>
        public static GameObject GuideArrowVisual(Transform parent, Material mat)
        {
            var go = new GameObject("ArrowVisual");
            go.transform.SetParent(parent, false);
            Vector2[] outline =
            {
                new Vector2(-0.009f, -0.04f), new Vector2(0.009f, -0.04f), new Vector2(0.009f, 0.0f),
                new Vector2(0.024f, 0.0f), new Vector2(0f, 0.04f), new Vector2(-0.024f, 0.0f), new Vector2(-0.009f, 0.0f),
            };
            var verts = new List<Vector3>();
            foreach (var o in outline) verts.Add(new Vector3(o.x, 0f, o.y));
            // 몸통(사각형) + 머리(삼각형), 양면
            int[] front = { 0, 6, 2, 0, 2, 1, 3, 5, 4 };
            var tris = new List<int>(front);
            for (int i = 0; i < front.Length; i += 3) { tris.Add(front[i]); tris.Add(front[i + 2]); tris.Add(front[i + 1]); }
            var mesh = new Mesh { name = "GuideArrow" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        /// <summary>강 건너 먼 지붕선: 불 켜진 창문 질감의 블록들과 돔 몇 개 (Unlit).</summary>
        public static void FarSkyline(string name, Transform parent, float zNear, float zFar, float xMin, float xMax, Palette p, int seed)
        {
            var root = Group(name, parent);
            var rng = new System.Random(seed);
            float x = xMin;
            int i = 0;
            while (x < xMax)
            {
                float w = 4f + (float)rng.NextDouble() * 6f;
                float h = 7f + (float)rng.NextDouble() * 9f;
                float z = zNear + (float)rng.NextDouble() * (zFar - zNear);
                TexBox($"Block_{i}", root, new Vector3(x + w * 0.5f, h * 0.5f - 1f, z), new Vector3(w, h, 4f), p.cityLights, 6f);
                TexBox($"Roof_{i}", root, new Vector3(x + w * 0.5f, h - 0.4f, z - 0.6f), new Vector3(w, 1.4f, 3.2f), p.farHaze, 4f, false, new Vector3(-25f, 0f, 0f));
                x += w + 0.5f;
                i++;
            }
            foreach (var (dx, dz, d) in new[] { (-14f, 0.6f, 9f), (22f, 0.3f, 6f) })
            {
                var drum = RemoveCollider(Cylinder("DomeDrum", root, new Vector3(dx, 15f, zNear + (zFar - zNear) * dz), d * 0.75f, 6f, p.farDome));
                var dome = RemoveCollider(Sphere("Dome", root, new Vector3(dx, 18f, zNear + (zFar - zNear) * dz), d, p.farDome));
                dome.transform.localScale = new Vector3(d * 0.8f, d * 0.75f, d * 0.8f);
                RemoveCollider(Cylinder("Lantern", root, new Vector3(dx, 21.6f, zNear + (zFar - zNear) * dz), 0.8f, 2f, p.farDome));
            }
        }
    }
}
