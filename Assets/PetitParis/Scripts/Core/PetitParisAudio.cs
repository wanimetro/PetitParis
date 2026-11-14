using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 외부 음원 없이 코드로 만드는 효과음 (라이선스 문제 없음).
    /// 처음 요청될 때 한 번만 생성해 재사용한다. 22kHz 모노라 메모리가 작다.
    /// </summary>
    public static class PetitParisAudio
    {
        const int Rate = 22050;

        static AudioClip footstep, bikeRoll, bikeBell, chime, fail, success;
        static AudioClip pickup, goldPickup, alert, whoosh, thud, flutter, chaseLoop, broom;

        /// <summary>빵 부스러기 줍기 (짧고 밝은 소리).</summary>
        public static AudioClip Pickup => pickup != null ? pickup : pickup = MakePickup();

        /// <summary>황금 크루아상 (반짝이는 아르페지오).</summary>
        public static AudioClip GoldPickup => goldPickup != null ? goldPickup : goldPickup = MakeGoldPickup();

        /// <summary>사람이 쥐를 발견했을 때 (짧은 경고음 두 번).</summary>
        public static AudioClip Alert => alert != null ? alert : alert = MakeAlert();

        /// <summary>물건이 떨어지는 바람 소리.</summary>
        public static AudioClip Whoosh => whoosh != null ? whoosh : whoosh = MakeWhoosh();

        /// <summary>떨어진 물건이 바닥에 닿는 소리.</summary>
        public static AudioClip Thud => thud != null ? thud : thud = MakeThud();

        /// <summary>비둘기 날갯짓.</summary>
        public static AudioClip Flutter => flutter != null ? flutter : flutter = MakeFlutter();

        /// <summary>추격 음악 (2초 루프, 낮은 북소리 + 하이햇).</summary>
        public static AudioClip ChaseLoop => chaseLoop != null ? chaseLoop : chaseLoop = MakeChaseLoop();

        /// <summary>빗자루로 바닥 쓰는 소리 (루프).</summary>
        public static AudioClip Broom => broom != null ? broom : broom = MakeBroom();

        /// <summary>거대한 신발이 땅을 디디는 둔탁한 소리.</summary>
        public static AudioClip Footstep => footstep != null ? footstep : footstep = MakeFootstep();

        /// <summary>자전거 바퀴 굴러가는 소리 (루프).</summary>
        public static AudioClip BikeRoll => bikeRoll != null ? bikeRoll : bikeRoll = MakeBikeRoll();

        /// <summary>자전거 벨 (따르릉).</summary>
        public static AudioClip BikeBell => bikeBell != null ? bikeBell : bikeBell = MakeBell();

        /// <summary>체크포인트 도착.</summary>
        public static AudioClip Chime => chime != null ? chime : chime = MakeChime();

        /// <summary>실패(밟힘).</summary>
        public static AudioClip Fail => fail != null ? fail : fail = MakeFail();

        /// <summary>목적지 도착.</summary>
        public static AudioClip Success => success != null ? success : success = MakeSuccess();

        /// <summary>3D 공간 음향 기본 설정. 거리는 월드 미터 (쥐 기준 1m = 사람 체감 10m).</summary>
        public static void Configure3D(AudioSource source, float minDistance, float maxDistance)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.spatialBlend = 1f;          // 완전 3D: 방향과 거리에 따라 좌우·크기가 바뀐다
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;          // 빠른 자전거의 피치 왜곡 방지
            source.spread = 0f;
        }

        /// <summary>2D(머리 고정) 효과음 설정. UI·실패·완료 소리용.</summary>
        public static void Configure2D(AudioSource source)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        static AudioClip Create(string name, float[] data)
        {
            Normalize(data, 0.9f);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeFootstep()
        {
            var rng = new System.Random(11);
            int n = (int)(0.32f * Rate);
            var d = new float[n];
            float phase = 0f, low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float freq = Mathf.Lerp(70f, 38f, t / 0.32f);
                phase += 2f * Mathf.PI * freq / Rate;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t * 14f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.08f;                   // 저역 통과 → 둔탁한 질감
                float scuff = low * Mathf.Exp(-t * 30f) * 2.5f;
                d[i] = body + scuff;
            }
            return Create("pp_footstep", d);
        }

        static AudioClip MakeBikeRoll()
        {
            var rng = new System.Random(23);
            int n = Rate; // 1초 루프
            var d = new float[n];
            float brown = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                brown = Mathf.Clamp(brown * 0.995f + white * 0.05f, -1f, 1f);
                float tick = Mathf.Exp(-((t * 8f) % 1f) * 40f) * 0.25f * Mathf.Sin(2f * Mathf.PI * 900f * t);
                d[i] = brown + tick;
            }
            // 루프 이음매를 부드럽게
            int fade = Rate / 50;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[i] *= k;
                d[n - 1 - i] *= k;
            }
            return Create("pp_bike_roll", d);
        }

        static AudioClip MakeBell()
        {
            int n = (int)(1.3f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                foreach (float start in new[] { 0f, 0.2f })
                {
                    float tt = t - start;
                    if (tt < 0f) continue;
                    float env = Mathf.Exp(-tt * 5f);
                    v += env * (Mathf.Sin(2f * Mathf.PI * 2093f * tt)
                                + 0.6f * Mathf.Sin(2f * Mathf.PI * 2637f * tt)
                                + 0.3f * Mathf.Sin(2f * Mathf.PI * 4186f * tt));
                }
                d[i] = v;
            }
            return Create("pp_bike_bell", d);
        }

        static AudioClip MakeChime()
        {
            int n = (int)(0.7f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * 6f) * Mathf.Clamp01(t * 200f);
                d[i] = env * (Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.7f * Mathf.Sin(2f * Mathf.PI * 1320f * t));
            }
            return Create("pp_chime", d);
        }

        static AudioClip MakeFail()
        {
            int n = (int)(0.6f * Rate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float freq = Mathf.Lerp(420f, 110f, t / 0.6f);
                phase += 2f * Mathf.PI * freq / Rate;
                d[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 4f) * Mathf.Clamp01(t * 100f);
            }
            return Create("pp_fail", d);
        }

        static AudioClip MakeSuccess()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float noteLen = 0.16f;
            int n = (int)((noteLen * notes.Length + 0.8f) * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float tt = t - k * noteLen;
                    if (tt < 0f) continue;
                    v += Mathf.Sin(2f * Mathf.PI * notes[k] * tt) * Mathf.Exp(-tt * 3.5f) * Mathf.Clamp01(tt * 200f);
                }
                d[i] = v;
            }
            return Create("pp_success", d);
        }

        static AudioClip MakePickup()
        {
            int n = (int)(0.18f * Rate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float freq = Mathf.Lerp(900f, 1600f, t / 0.18f);
                phase += 2f * Mathf.PI * freq / Rate;
                d[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 18f) * Mathf.Clamp01(t * 400f);
            }
            return Create("pp_pickup", d);
        }

        static AudioClip MakeGoldPickup()
        {
            float[] notes = { 1046.5f, 1318.5f, 1568f, 2093f, 2637f };
            float noteLen = 0.06f;
            int n = (int)((noteLen * notes.Length + 0.6f) * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float tt = t - k * noteLen;
                    if (tt < 0f) continue;
                    v += Mathf.Sin(2f * Mathf.PI * notes[k] * tt) * Mathf.Exp(-tt * 6f) * Mathf.Clamp01(tt * 300f);
                }
                d[i] = v;
            }
            return Create("pp_gold_pickup", d);
        }

        static AudioClip MakeAlert()
        {
            int n = (int)(0.45f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                foreach (var (start, freq) in new[] { (0f, 740f), (0.16f, 988f) })
                {
                    float tt = t - start;
                    if (tt < 0f || tt > 0.25f) continue;
                    // 사각파 비슷한 소리 (만화 같은 "!")
                    float s = Mathf.Sin(2f * Mathf.PI * freq * tt);
                    v += Mathf.Sign(s) * 0.4f * Mathf.Exp(-tt * 10f) * Mathf.Clamp01(tt * 300f);
                }
                d[i] = v;
            }
            return Create("pp_alert", d);
        }

        static AudioClip MakeWhoosh()
        {
            var rng = new System.Random(31);
            int n = (int)(0.8f * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = t / 0.8f;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * Mathf.Lerp(0.03f, 0.25f, k);     // 점점 밝아짐 (가까워지는 느낌)
                d[i] = lp * Mathf.Sin(k * Mathf.PI * 0.5f);
            }
            return Create("pp_whoosh", d);
        }

        static AudioClip MakeThud()
        {
            var rng = new System.Random(37);
            int n = (int)(0.25f * Rate);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(160f, 70f, t / 0.25f) / Rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * 0.2f;
                d[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 22f) + lp * Mathf.Exp(-t * 40f) * 1.5f;
            }
            return Create("pp_thud", d);
        }

        static AudioClip MakeFlutter()
        {
            var rng = new System.Random(41);
            int n = (int)(1.0f * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * 0.35f;
                float flap = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI * 14f)), 6f); // 초당 14번 날갯짓
                d[i] = lp * flap * Mathf.Exp(-t * 1.5f);
            }
            return Create("pp_flutter", d);
        }

        static AudioClip MakeChaseLoop()
        {
            var rng = new System.Random(43);
            const float bpm = 150f;
            float beat = 60f / bpm;
            int n = Mathf.RoundToInt(beat * 4f * Rate); // 4박 루프 (1.6초)
            var d = new float[n];
            float[] bass = { 110f, 110f, 130.81f, 103.83f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int b = Mathf.Min(3, (int)(t / beat));
                float tb = t - b * beat;
                // 낮은 북 (매 박)
                float kick = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 45f, Mathf.Clamp01(tb / 0.12f)) * tb) * Mathf.Exp(-tb * 18f);
                // 베이스 (8분음표 스타카토)
                float te = tb % (beat * 0.5f);
                float bassV = Mathf.Sin(2f * Mathf.PI * bass[b] * t) * Mathf.Exp(-te * 9f) * 0.45f;
                // 하이햇 (16분음표)
                float ts = tb % (beat * 0.25f);
                float hat = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-ts * 60f) * 0.18f;
                d[i] = kick + bassV + hat;
            }
            return Create("pp_chase_loop", d);
        }

        static AudioClip MakeBroom()
        {
            var rng = new System.Random(47);
            int n = Rate; // 1초 루프 (두 번 쓸기)
            var d = new float[n];
            float lp = 0f, hp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * 0.5f;
                hp = lp - hp * 0.6f;
                float stroke = Mathf.Pow(Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f, 3f);
                d[i] = hp * stroke;
            }
            int fade = Rate / 50;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[i] *= k;
                d[n - 1 - i] *= k;
            }
            return Create("pp_broom", d);
        }

        static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            foreach (var v in data) max = Mathf.Max(max, Mathf.Abs(v));
            if (max < 1e-6f) return;
            float k = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= k;
        }
    }
}
