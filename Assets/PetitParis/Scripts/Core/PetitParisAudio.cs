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
