using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 추격 이벤트: 쥐가 trigger 구역에 들어오면 지정한 사람이 바로 추격을 시작하고 긴장 음악이 나온다.
    /// - 쥐가 숨거나(안전 구역) 시간이 지나 사람이 놓치면 끝 → "휴, 따돌렸어요!"
    /// - 추격 중에 밟히면 이벤트를 다시 준비한다 (체크포인트에서 다시 도전)
    /// - 카메라는 흔들지 않는다 (멀미 방지). 긴장감은 소리·말풍선·발소리로만 만든다
    /// </summary>
    [DisallowMultipleComponent]
    public class ChaseEvent : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] RatRespawn respawn;
        [SerializeField] PetitParisGameFlow gameFlow;

        [Tooltip("시작 구역 (Trigger Collider). 쥐 몸 중심이 들어오면 시작.")]
        [SerializeField] Collider trigger;
        [SerializeField] PedestrianAwareness chaser;
        [SerializeField, Range(2f, 30f)] float chaseSeconds = 9f;
        [SerializeField] string startMessage = PetitParisText.MessageChase;

        [Header("음악")]
        [SerializeField] AudioSource music;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;
        [SerializeField, Range(0.1f, 3f)] float musicFade = 0.8f;

        [Tooltip("추격 중 빗자루 (선택). 추격 중에만 빠르게 쓴다.")]
        [SerializeField] BroomSweep broom;

        bool armed = true;
        bool running;
        float targetVolume;

        public bool IsRunning => running;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null)
            {
                if (danger == null) danger = rig.GetComponent<RatDangerDetector>();
                if (respawn == null) respawn = rig.GetComponent<RatRespawn>();
            }
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (trigger == null) trigger = GetComponent<Collider>();
            if (trigger != null) trigger.isTrigger = true;
            if (music != null)
            {
                PetitParisAudio.Configure2D(music);
                music.clip = PetitParisAudio.ChaseLoop;
                music.loop = true;
                music.volume = 0f;
            }
        }

        void OnEnable()
        {
            if (chaser != null) chaser.ChaseEnded += OnChaseEnded;
            if (respawn != null) respawn.Died += OnDied;
        }

        void OnDisable()
        {
            if (chaser != null) chaser.ChaseEnded -= OnChaseEnded;
            if (respawn != null) respawn.Died -= OnDied;
        }

        void Update()
        {
            if (music != null)
            {
                music.volume = Mathf.MoveTowards(music.volume, targetVolume, musicVolume / musicFade * Time.deltaTime);
                if (music.volume <= 0f && music.isPlaying && targetVolume <= 0f) music.Stop();
            }

            if (!armed || running || trigger == null || danger == null || chaser == null) return;
            if (respawn != null && (respawn.IsRespawning || respawn.IsInvulnerable)) return;
            if (!RatSafeZone.ColliderContains(trigger, danger.BodyCenter)) return;
            Begin();
        }

        void Begin()
        {
            running = true;
            armed = false;
            chaser.ForceChase(chaseSeconds);
            if (broom != null) broom.Frantic = true;
            if (gameFlow != null) gameFlow.PostMessage(startMessage);
            if (music != null)
            {
                if (!music.isPlaying) music.Play();
                targetVolume = musicVolume;
            }
        }

        void OnChaseEnded()
        {
            if (!running) return;
            running = false;
            targetVolume = 0f;
            if (broom != null) broom.Frantic = false;
        }

        void OnDied(string reason)
        {
            if (!running) return;
            running = false;
            armed = true; // 다시 도전
            targetVolume = 0f;
            if (broom != null) broom.Frantic = false;
        }
    }
}
