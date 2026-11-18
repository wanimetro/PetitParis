using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// Quest 실기기 진단 로그 (Android 실기기만, 3초마다 logcat "[PetitParis][Diag]").
    /// 2026-11-18 Quest 2: "소리가 안 남", "팔을 흔들어도 안 빨라짐" — 에디터에서는 정상이라 실기기 값을 본다.
    /// - 오디오: AudioListener 일시 정지·볼륨, 재생 중인 AudioSource 수, Unity 최종 출력 RMS(0이면 Unity가 무음을 내보냄)
    /// - 팔 흔들기: XR 장치(L/R/H) 인식, 손 속도(m/s), SwingFactor
    /// 확인: adb logcat -s Unity | findstr Diag
    /// </summary>
    public class RatQuestDiagnostics : MonoBehaviour
    {
        const float Interval = 3f;
        readonly float[] samples = new float[256];
        float next;
        RatArmSwing swing;

#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("PetitParis Diagnostics");
            DontDestroyOnLoad(go);
            go.AddComponent<RatQuestDiagnostics>();
        }
#endif

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;

            int playing = 0, total = 0;
            string names = "";
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            {
                total++;
                if (!a.isPlaying) continue;
                playing++;
                if (names.Length < 120) names += (a.clip != null ? a.clip.name : a.name) + "(" + a.volume.ToString("0.00") + ") ";
            }
            AudioListener.GetOutputData(samples, 0);
            float sum = 0f;
            foreach (var s in samples) sum += s * s;
            float rms = Mathf.Sqrt(sum / samples.Length);
            var cfg = AudioSettings.GetConfiguration();

            if (swing == null) swing = FindAnyObjectByType<RatArmSwing>();
            string swingText = swing == null ? "no RatArmSwing" :
                $"src={swing.Source} devices={swing.DevicesFound} missing={swing.MissingNode} L={swing.LeftHandSpeed:0.00} R={swing.RightHandSpeed:0.00} factor={swing.SwingFactor:0.00} enabled={swing.isActiveAndEnabled}";

            Debug.Log($"[PetitParis][Diag] audio pause={AudioListener.pause} vol={AudioListener.volume:0.00} listeners={FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length} " +
                      $"playing={playing}/{total} rms={rms:0.00000} rate={cfg.sampleRate} speaker={cfg.speakerMode} timeScale={Time.timeScale:0.0} | {names}| swing {swingText}");
        }
    }
}
