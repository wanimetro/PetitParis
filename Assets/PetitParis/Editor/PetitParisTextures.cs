using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PetitParis.EditorTools
{
    /// <summary>
    /// 파리 거리용 질감을 코드로 생성한다 (외부 이미지 없음 → 라이선스 문제 없음).
    /// 모두 이음매 없이 반복(타일)되며, Quest용으로 256~512px PNG로 저장한다.
    /// 저장 위치: Assets/PetitParis/Textures/Generated/
    /// </summary>
    public static class PetitParisTextures
    {
        const string Folder = PetitParisPaths.Root + "/Textures/Generated";
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>씬 빌드 시작 때 호출: 질감을 다시 생성하도록 캐시를 비운다 (코드 수정 반영).</summary>
        public static void ClearCache() => cache.Clear();

        public static Texture2D Cobblestone() => Make("PP_Cobblestone", 512, CobblePixel);
        public static Texture2D SidewalkSlabs() => Make("PP_SidewalkSlabs", 256, SlabPixel);
        public static Texture2D Asphalt() => Make("PP_Asphalt", 256, (u, v) => Tint(new Color(0.2f, 0.2f, 0.22f), 0.85f + 0.3f * Fbm(u, v, 16, 4, 7)));
        public static Texture2D Limestone() => Make("PP_Limestone", 256, (u, v) => AshlarPixel(u, v, 4, 2, new Color(0.88f, 0.82f, 0.7f), 11));
        public static Texture2D LimestoneRustic() => Make("PP_LimestoneRustic", 256, (u, v) => AshlarPixel(u, v, 3, 2, new Color(0.8f, 0.74f, 0.62f), 13, 0.03f));
        public static Texture2D Plaster(string name, Color baseColor, int seed) => Make(name, 256, (u, v) => PlasterPixel(u, v, baseColor, seed));
        public static Texture2D Zinc() => Make("PP_Zinc", 256, ZincPixel);
        public static Texture2D Wood() => Make("PP_Wood", 256, WoodPixel);
        public static Texture2D Brick() => Make("PP_Brick", 256, (u, v) => AshlarPixel(u, v, 8, 4, new Color(0.62f, 0.32f, 0.22f), 17, 0.04f));
        /// <summary>줄무늬. alongV = false면 u 방향으로 색이 바뀌고(세로 줄), true면 v 방향으로 바뀐다.</summary>
        public static Texture2D Stripes(string name, Color a, Color b, int count, bool alongV) =>
            Make(name, 128, (u, v) => StripePixel(alongV ? v : u, alongV ? u : v, a, b, count));
        public static Texture2D Checker(string name, Color a, Color b, int count) =>
            Make(name, 128, (u, v) => ((Mathf.FloorToInt(u * count) + Mathf.FloorToInt(v * count)) & 1) == 0 ? a : b);
        public static Texture2D Rattan() => Make("PP_Rattan", 128, RattanPixel);
        public static Texture2D Marble() => Make("PP_Marble", 256, MarblePixel);
        public static Texture2D WindowLit() => Make("PP_WindowLit", 128, (u, v) => WindowPixel(u, v, true));
        public static Texture2D WindowDark() => Make("PP_WindowDark", 128, (u, v) => WindowPixel(u, v, false));
        public static Texture2D ShopWindow() => Make("PP_ShopWindow", 256, ShopWindowPixel);
        public static Texture2D Geraniums() => Make("PP_Geraniums", 128, GeraniumPixel);
        public static Texture2D Posters() => Make("PP_Posters", 256, PosterPixel);
        public static Texture2D WaterLights() => Make("PP_WaterLights", 256, WaterPixel);
        public static Texture2D CityLights() => Make("PP_CityLights", 256, CityLightsPixel);

        // ───────────────────────── 생성·저장 ─────────────────────────

        static Texture2D Make(string name, int size, Func<float, float, Color> pixel)
        {
            if (cache.TryGetValue(name, out var cached) && cached != null) return cached;
            if (!AssetDatabase.IsValidFolder(PetitParisPaths.Root + "/Textures"))
                AssetDatabase.CreateFolder(PetitParisPaths.Root, "Textures");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder(PetitParisPaths.Root + "/Textures", "Generated");

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = pixel((x + 0.5f) / size, (y + 0.5f) / size);
            tex.SetPixels(pixels);
            tex.Apply();

            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.sRGBTexture = true;
                importer.maxTextureSize = size;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            var result = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            cache[name] = result;
            return result;
        }

        // ───────────────────────── 노이즈 (주기적 → 이음매 없음) ─────────────────────────

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static float ValueNoise(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int Wrap(int a) => ((a % period) + period) % period;
            float a00 = Hash(Wrap(x0), Wrap(y0), seed), a10 = Hash(Wrap(x0 + 1), Wrap(y0), seed);
            float a01 = Hash(Wrap(x0), Wrap(y0 + 1), seed), a11 = Hash(Wrap(x0 + 1), Wrap(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a00, a10, fx), Mathf.Lerp(a01, a11, fx), fy);
        }

        static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(u, v, period << i, seed + i * 31) * amp;
                norm += amp;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        static Color Tint(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        // ───────────────────────── 질감별 픽셀 ─────────────────────────

        /// <summary>파리 포석: 엇갈린 줄의 둥근 돌, 돌마다 색이 조금씩 다르고 틈은 어둡다.</summary>
        static Color CobblePixel(float u, float v)
        {
            const int rows = 8, cols = 6;
            float y = v * rows;
            int row = Mathf.FloorToInt(y);
            float x = u * cols + ((row & 1) == 1 ? 0.5f : 0f);
            int col = Mathf.FloorToInt(x);
            float fx = x - col, fy = y - row;
            int colWrapped = ((col % cols) + cols) % cols;

            float r = Hash(colWrapped, row, 3);
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) * 1.3f, Mathf.Min(fy, 1f - fy));
            float stone = Mathf.SmoothStep(0.04f, 0.16f, edge);
            Color[] palette =
            {
                new Color(0.62f, 0.6f, 0.57f), new Color(0.56f, 0.54f, 0.54f), new Color(0.66f, 0.61f, 0.55f),
                new Color(0.52f, 0.52f, 0.56f), new Color(0.64f, 0.58f, 0.52f),
            };
            Color baseColor = palette[Mathf.FloorToInt(r * palette.Length) % palette.Length];
            float dome = 0.85f + 0.25f * Mathf.SmoothStep(0.05f, 0.4f, edge);
            float grain = 0.85f + 0.3f * Fbm(u, v, 32, 3, 5);
            Color stoneColor = Tint(baseColor, dome * grain);
            Color gap = new Color(0.26f, 0.23f, 0.21f);
            return Color.Lerp(gap, stoneColor, stone);
        }

        static Color SlabPixel(float u, float v)
        {
            const int n = 3;
            float fx = u * n % 1f, fy = v * n % 1f;
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
            int cx = Mathf.FloorToInt(u * n), cy = Mathf.FloorToInt(v * n);
            float k = 0.9f + 0.12f * Hash(cx, cy, 9) + 0.15f * (Fbm(u, v, 16, 3, 2) - 0.5f);
            Color slab = Tint(new Color(0.66f, 0.63f, 0.58f), k);
            return Color.Lerp(new Color(0.36f, 0.34f, 0.32f), slab, Mathf.SmoothStep(0.005f, 0.02f, edge));
        }

        /// <summary>마름돌 쌓기 (오스만 석회암 / 벽돌): 엇갈린 블록 + 줄눈.</summary>
        static Color AshlarPixel(float u, float v, int rows, int cols, Color baseColor, int seed, float joint = 0.018f)
        {
            float y = v * rows;
            int row = Mathf.FloorToInt(y);
            float x = u * cols + ((row & 1) == 1 ? 0.5f : 0f);
            int col = ((Mathf.FloorToInt(x) % cols) + cols) % cols;
            float fx = x % 1f, fy = y - row;
            // 줄눈까지 거리를 블록 높이 단위로 통일 (블록 폭/높이 = rows/cols)
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) * rows / cols, Mathf.Min(fy, 1f - fy));
            float k = 0.92f + 0.1f * Hash(col, row, seed) + 0.18f * (Fbm(u, v, 8, 4, seed) - 0.5f);
            Color block = Tint(baseColor, k);
            Color mortar = Tint(baseColor, 0.72f);
            return Color.Lerp(mortar, block, Mathf.SmoothStep(joint * 0.4f, joint, edge));
        }

        static Color PlasterPixel(float u, float v, Color baseColor, int seed)
        {
            float k = 0.9f + 0.14f * Fbm(u, v, 4, 5, seed);
            float stain = Mathf.SmoothStep(0.6f, 0.8f, Fbm(u * 1f, v, 2, 3, seed + 100)) * 0.1f;
            return Tint(baseColor, k - stain);
        }

        static Color ZincPixel(float u, float v)
        {
            float seam = u * 8f % 1f;
            float k = 0.9f + 0.15f * Fbm(u * 0.25f, v, 4, 3, 21);
            Color c = Tint(new Color(0.42f, 0.47f, 0.53f), k);
            if (seam < 0.04f) c = Tint(c, 1.25f);
            else if (seam < 0.08f) c = Tint(c, 0.75f);
            return c;
        }

        static Color WoodPixel(float u, float v)
        {
            const int planks = 6;
            int p = Mathf.FloorToInt(v * planks);
            float fy = v * planks % 1f;
            float grain = Mathf.Sin((u * 20f + Fbm(u, v, 8, 3, p) * 6f) * Mathf.PI) * 0.5f + 0.5f;
            Color c = Tint(new Color(0.46f, 0.3f, 0.18f), 0.8f + 0.2f * grain + 0.1f * Hash(p, 0, 4));
            return fy < 0.04f ? Tint(c, 0.55f) : c;
        }

        static Color StripePixel(float u, float v, Color a, Color b, int count)
        {
            bool first = Mathf.FloorToInt(u * count * 2) % 2 == 0;
            float fabric = 0.93f + 0.07f * ValueNoise(u, v, 64, 8);
            return Tint(first ? a : b, fabric);
        }

        static Color RattanPixel(float u, float v)
        {
            const int n = 8;
            int cx = Mathf.FloorToInt(u * n), cy = Mathf.FloorToInt(v * n);
            float fx = u * n % 1f, fy = v * n % 1f;
            bool horizontal = ((cx + cy) & 1) == 0;
            float strand = horizontal ? Mathf.Sin(fy * Mathf.PI) : Mathf.Sin(fx * Mathf.PI);
            return Tint(new Color(0.78f, 0.6f, 0.36f), 0.55f + 0.5f * strand);
        }

        static Color MarblePixel(float u, float v)
        {
            float n = Fbm(u, v, 4, 5, 33);
            float vein = Mathf.Abs(Mathf.Sin((u + v) * Mathf.PI * 2f + n * 7f));
            float k = Mathf.SmoothStep(0.0f, 0.08f, vein);
            return Color.Lerp(new Color(0.55f, 0.55f, 0.58f), new Color(0.94f, 0.93f, 0.9f), 0.3f + 0.7f * k);
        }

        /// <summary>파리 창문: 나무 창틀 십자 + 유리 (켜진 창은 따뜻한 실내 빛과 커튼).</summary>
        static Color WindowPixel(float u, float v, bool lit)
        {
            float frame = 0.06f;
            bool border = u < frame || u > 1f - frame || v < frame || v > 1f - frame;
            bool mullion = Mathf.Abs(u - 0.5f) < 0.025f || Mathf.Abs(v - 0.62f) < 0.02f || Mathf.Abs(v - 0.3f) < 0.015f;
            Color frameColor = lit ? new Color(0.9f, 0.86f, 0.78f) : new Color(0.75f, 0.72f, 0.66f);
            if (border || mullion) return frameColor;
            if (lit)
            {
                bool curtain = u < 0.18f || u > 0.82f;
                Color warm = Color.Lerp(new Color(1f, 0.62f, 0.3f), new Color(1f, 0.85f, 0.55f), v);
                return curtain ? Tint(new Color(0.95f, 0.75f, 0.55f), 0.85f + 0.15f * Mathf.Sin(u * 80f)) : warm;
            }
            float sky = Mathf.SmoothStep(0.2f, 1f, v);
            return Color.Lerp(new Color(0.12f, 0.13f, 0.2f), new Color(0.36f, 0.38f, 0.52f), sky * 0.7f + 0.3f * (u - v + 0.5f) * 0.3f);
        }

        static Color ShopWindowPixel(float u, float v)
        {
            bool mullion = (u * 4f % 1f) < 0.03f || v < 0.04f || v > 0.96f;
            if (mullion) return new Color(0.2f, 0.12f, 0.08f);
            Color warm = Color.Lerp(new Color(1f, 0.55f, 0.25f), new Color(1f, 0.88f, 0.6f), v);
            // 진열대의 빵·컵 실루엣
            bool shelf = Mathf.Abs(v - 0.35f) < 0.02f || Mathf.Abs(v - 0.62f) < 0.02f;
            float bread = Mathf.Sin(u * 60f) > 0.6f && (Mathf.Abs(v - 0.4f) < 0.04f || Mathf.Abs(v - 0.67f) < 0.035f) ? 1f : 0f;
            if (shelf) return new Color(0.45f, 0.28f, 0.15f);
            return Color.Lerp(warm, new Color(0.75f, 0.45f, 0.18f), bread);
        }

        static Color GeraniumPixel(float u, float v)
        {
            float n = Fbm(u, v, 8, 3, 41);
            float flower = Mathf.SmoothStep(0.55f, 0.62f, ValueNoise(u, v, 16, 42));
            Color leaf = Tint(new Color(0.2f, 0.42f, 0.18f), 0.7f + 0.5f * n);
            Color red = Tint(new Color(0.85f, 0.12f, 0.15f), 0.8f + 0.3f * n);
            return Color.Lerp(leaf, red, flower);
        }

        static Color PosterPixel(float u, float v)
        {
            int cx = Mathf.FloorToInt(u * 4f), cy = Mathf.FloorToInt(v * 2f);
            float fx = u * 4f % 1f, fy = v * 2f % 1f;
            if (fx < 0.04f || fy < 0.04f) return new Color(0.15f, 0.25f, 0.18f);
            Color[] colors =
            {
                new Color(0.85f, 0.3f, 0.25f), new Color(0.95f, 0.8f, 0.35f), new Color(0.25f, 0.4f, 0.65f),
                new Color(0.92f, 0.88f, 0.8f), new Color(0.5f, 0.25f, 0.45f), new Color(0.3f, 0.55f, 0.45f),
            };
            Color c = colors[Mathf.FloorToInt(Hash(cx, cy, 51) * colors.Length) % colors.Length];
            bool title = Mathf.Abs(fy - 0.78f) < 0.06f && fx > 0.15f && fx < 0.85f;
            return title ? Tint(c, 0.4f) : Tint(c, 0.9f + 0.1f * ValueNoise(u, v, 32, 52));
        }

        /// <summary>해 질 녘 강물: 짙은 청보라 물결 + 불빛이 길게 비친 줄.</summary>
        static Color WaterPixel(float u, float v)
        {
            float ripple = Fbm(u, v * 4f, 8, 3, 61);
            Color water = Color.Lerp(new Color(0.16f, 0.2f, 0.32f), new Color(0.32f, 0.33f, 0.48f), ripple);
            float column = ValueNoise(u, 0.5f, 24, 62);
            float streak = Mathf.SmoothStep(0.78f, 0.86f, column) * Mathf.SmoothStep(0.35f, 0.6f, ValueNoise(u, v, 32, 63));
            return Color.Lerp(water, new Color(1f, 0.78f, 0.45f), streak);
        }

        /// <summary>먼 도시 실루엣의 창문 불빛 점.</summary>
        static Color CityLightsPixel(float u, float v)
        {
            Color wall = Color.Lerp(new Color(0.42f, 0.4f, 0.52f), new Color(0.55f, 0.5f, 0.6f), v);
            float cell = Hash(Mathf.FloorToInt(u * 32f), Mathf.FloorToInt(v * 24f), 71);
            bool window = cell > 0.72f && (u * 32f % 1f) > 0.3f && (v * 24f % 1f) > 0.35f;
            return window ? new Color(1f, 0.82f, 0.5f) : wall;
        }
    }
}
