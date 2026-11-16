using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 배경 소리 (Quest 2 테스트 Q2-04: 플레이 중 거의 아무 소리도 안 났다).
    /// - 거리 배경음 (먼 차·웅성임·컵 소리) + 아코디언 왈츠 배경 음악, 둘 다 2D 루프 (코드로 만든 소리, 라이선스 문제 없음)
    /// - 사람에게 들켜 쫓기거나 추격 이벤트 중에는 음악을 줄인다 (추격 음악·발소리가 잘 들리게)
    /// - 완료 화면에서는 음악을 조금 키운다
    /// - 일시 정지(AudioListener.pause) 때는 같이 멈춘다
    /// </summary>
    [DisallowMultipleComponent]
    public class RatAmbience : MonoBehaviour
    {
        [SerializeField] AudioSource cityLoop;
        [SerializeField] AudioSource musicLoop;
        [Tooltip("질주 바람 소리 등 짧은 효과음 (2D).")]
        [SerializeField] AudioSource sfx;
        [SerializeField] PetitParisGameFlow gameFlow;
        [SerializeField, Range(0f, 1f)] float cityVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] float duckedMusicVolume = 0.06f;
        [SerializeField, Range(0.1f, 5f)] float fadeSpeed = 0.5f;

        ChaseEvent[] chases = new ChaseEvent[0];
        RatDashController dash;

        void Start()
        {
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            chases = FindObjectsByType<ChaseEvent>();
            AudioListener.pause = false; // 이전 씬에서 일시 정지가 남아 있지 않게
            Play(cityLoop, PetitParisAudio.CityAmbience, cityVolume);
            Play(musicLoop, PetitParisAudio.MusetteLoop, 0f);
            if (sfx != null) PetitParisAudio.Configure2D(sfx);
            dash = FindAnyObjectByType<RatDashController>();
            if (dash != null) dash.DashStarted += OnDash; // 질주가 발동됐다는 소리 (Q2-01)
        }

        void OnDestroy()
        {
            if (dash != null) dash.DashStarted -= OnDash;
        }

        void OnDash()
        {
            if (sfx != null) sfx.PlayOneShot(PetitParisAudio.Whoosh, 0.35f);
        }

        static void Play(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null) return;
            PetitParisAudio.Configure2D(source);
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.Play();
        }

        void Update()
        {
            if (musicLoop == null) return;
            bool chased = PedestrianAwareness.AnyHunting;
            foreach (var c in chases) if (c != null && c.IsRunning) chased = true;
            float target = musicVolume;
            if (chased) target = duckedMusicVolume;
            if (gameFlow != null && gameFlow.CurrentPhase == PetitParisGameFlow.Phase.Completed) target = musicVolume * 1.5f;
            musicLoop.volume = Mathf.MoveTowards(musicLoop.volume, target, fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
