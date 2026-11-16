using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 추격 이벤트: 쥐가 trigger 구역에 들어오면 지정한 사람이 바로 추격을 시작하고 긴장 음악이 나온다.
    /// - 쥐가 숨거나(안전 구역) 시간이 지나 사람이 놓치면 끝 → "휴, 따돌렸어요!" (이벤트 완료, 다시 안 함)
    /// - 추격 중에 밟히면 이벤트를 다시 준비한다 (체크포인트에서 다시 도전)
    /// - 카메라는 흔들지 않는다 (멀미 방지). 긴장감은 소리·말풍선·발소리로만 만든다
    ///
    /// 상태 관리 (AI 리뷰 ISSUE-08): 쥐가 죽으면 RatRespawn.Died와 PedestrianAwareness.ChaseEnded가 같은 프레임에 온다.
    /// 어느 쪽이 먼저 와도 결과가 같도록, 추격이 끝났을 때 "쥐가 리스폰 중인지"를 직접 보고 다시 준비할지 정한다
    /// (RatRespawn은 Died를 보내기 전에 IsRespawning을 켠다).
    /// </summary>
    [DisallowMultipleComponent]
    public class ChaseEvent : MonoBehaviour
    {
        public enum EventState { Armed, Running, Done }

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

        float targetVolume;

        public EventState State { get; private set; } = EventState.Armed;
        public bool IsRunning => State == EventState.Running;
        public int StartCount { get; private set; }

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
                music.volume = Mathf.MoveTowards(music.volume, targetVolume, musicVolume / musicFade * Time.unscaledDeltaTime);
                if (music.volume <= 0f && music.isPlaying && targetVolume <= 0f) music.Stop();
            }

            // 안전장치: 실행 중인데 추격자가 더 이상 쫓지 않으면 (이벤트를 놓친 경우) 상태를 정리한다
            if (State == EventState.Running && chaser != null && !chaser.IsHunting) Finish(RatIsRespawning());

            if (State != EventState.Armed || trigger == null || danger == null || chaser == null) return;
            if (respawn != null && (respawn.IsRespawning || respawn.IsInvulnerable)) return;
            if (!RatSafeZone.ColliderContains(trigger, danger.BodyCenter)) return;
            Begin();
        }

        void Begin()
        {
            State = EventState.Running;
            StartCount++;
            chaser.ForceChase(chaseSeconds);
            if (broom != null) broom.Frantic = true;
            if (gameFlow != null) gameFlow.PostMessage(startMessage, 1);
            if (music != null)
            {
                if (!music.isPlaying) music.Play();
                targetVolume = musicVolume;
            }
        }

        bool RatIsRespawning() => respawn != null && respawn.IsRespawning;

        void OnChaseEnded()
        {
            if (State != EventState.Running) return;
            Finish(RatIsRespawning()); // 밟혀서 끝났으면 다시 준비, 따돌렸으면 완료
        }

        void OnDied(string reason)
        {
            // Died가 ChaseEnded보다 먼저 오든 나중에 오든: 실행 중이었거나 같은 프레임에 끝났으면 다시 준비
            if (State == EventState.Running || (State == EventState.Done && finishedFrame == Time.frameCount))
                Finish(true);
        }

        int finishedFrame = -1;

        void Finish(bool rearm)
        {
            State = rearm ? EventState.Armed : EventState.Done;
            finishedFrame = Time.frameCount;
            targetVolume = 0f;
            if (broom != null) broom.Frantic = false;
        }
    }
}
