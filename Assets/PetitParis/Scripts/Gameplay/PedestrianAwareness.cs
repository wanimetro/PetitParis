using System;
using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 사람이 쥐를 "알아채는" 반응. 보행자(PedestrianPath)에 함께 붙인다.
    ///
    /// 1. 발견: 쥐가 앞쪽 시야(noticeAngle) 안, noticeRadius 거리 안에 있고 안전 구역 밖이면
    ///    걸음을 멈추고 쥐 쪽을 본다. 머리 위에 "!" + 경고음
    /// 2. 추격: reactTime 뒤 "앗, 쥐다!"를 외치고 쥐를 향해 쿵쿵 밟으며 다가온다 (chaseDuration초)
    ///    몸이 천천히 돌기 때문에 옆으로 질주하면 따돌릴 수 있다
    /// 3. 놓침: 쥐가 안전 구역에 숨거나 시간이 지나면 "어디 갔지?" 후 원래 경로로 돌아간다 (cooldown 동안 다시 발견하지 않음)
    /// - 웅크리면(crouch) 발견 거리가 줄어든다 → 몰래 지나가는 플레이
    /// - ForceChase()로 이벤트(ChaseEvent)가 강제로 추격을 시작할 수 있다
    /// 거리 단위는 월드 미터. 카메라는 건드리지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PedestrianPath))]
    public class PedestrianAwareness : MonoBehaviour
    {
        public enum State { Idle, Noticed, Chasing, Lost }

        [SerializeField] RatScaleRig rig;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] RatRespawn respawn;
        [SerializeField] RatCrouchController crouch;
        [SerializeField] PetitParisGameFlow gameFlow;

        [Header("발견")]
        [SerializeField] bool autoNotice = true;
        [Tooltip("발견 거리 (월드 m). 1.1 = 사람 기준 약 11m 앞이 아니라, 쥐 기준 체감 11m.")]
        [SerializeField, Range(0.2f, 3f)] float noticeRadius = 1.0f;
        [Tooltip("정면 기준 좌우 시야 반각(도).")]
        [SerializeField, Range(10f, 180f)] float noticeAngle = 65f;
        [Tooltip("웅크리면 발견 거리에 곱하는 값.")]
        [SerializeField, Range(0.1f, 1f)] float crouchMultiplier = 0.45f;
        [Tooltip("발견 후 추격까지 멈춰 있는 시간(초). 이 사이에 도망칠 수 있다.")]
        [SerializeField, Range(0.2f, 3f)] float reactTime = 0.9f;

        [Header("추격")]
        [SerializeField, Range(0.5f, 20f)] float chaseDuration = 3.5f;
        [SerializeField, Range(0.1f, 2f)] float chaseSpeed = 0.55f;
        [SerializeField, Range(20f, 360f)] float chaseTurnSpeed = 110f;
        [Tooltip("경로 영역 바깥으로 이만큼(월드 m)까지만 쫓아온다.")]
        [SerializeField, Range(0f, 3f)] float chaseAreaPadding = 0.6f;
        [Tooltip("켜면 경로 영역 대신 아래 영역 안에서만 쫓아온다 (월드 좌표).")]
        [SerializeField] bool useCustomArea;
        [SerializeField] Bounds customArea = new Bounds(Vector3.zero, Vector3.one);
        [Tooltip("쥐가 이 시간(초) 동안 안전 구역에 있으면 놓친다.")]
        [SerializeField, Range(0f, 5f)] float loseAfterHidden = 1.2f;
        [SerializeField, Range(0f, 30f)] float cooldown = 7f;

        [Header("말풍선·소리")]
        [SerializeField] TMP_Text bubble;
        [SerializeField] string shout = PetitParisText.ShoutRat;
        [SerializeField] AudioSource voice;

        PedestrianPath path;
        Transform ratTarget;
        float timer;
        float hiddenTimer;
        float cooldownUntil;
        float bubbleUntil;
        float forcedDuration = -1f;

        public State CurrentState { get; private set; } = State.Idle;
        public bool IsHunting => CurrentState == State.Noticed || CurrentState == State.Chasing;
        public bool AutoNotice { get => autoNotice; set => autoNotice = value; }

        /// <summary>어떤 보행자든 쥐를 발견했을 때.</summary>
        public static event Action<PedestrianAwareness> AnyNoticed;
        /// <summary>추격이 끝났을 때 (쥐를 놓침).</summary>
        public event Action ChaseEnded;

        void Awake()
        {
            path = GetComponent<PedestrianPath>();
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null)
            {
                if (danger == null) danger = rig.GetComponent<RatDangerDetector>();
                if (respawn == null) respawn = rig.GetComponent<RatRespawn>();
                if (crouch == null) crouch = rig.GetComponentInChildren<RatCrouchController>(true);
                ratTarget = rig.transform;
            }
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            PetitParisAudio.Configure3D(voice, 0.4f, 12f);
            if (bubble != null) bubble.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (respawn != null) respawn.Died += OnRatDied;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.Died -= OnRatDied;
        }

        void OnRatDied(string reason)
        {
            // 밟았으면 만족하고 돌아간다
            if (CurrentState == State.Idle) return;
            StopChase(false);
        }

        void Update()
        {
            if (path == null || ratTarget == null || danger == null) return;
            float dt = Time.deltaTime;
            bool ratBusy = respawn != null && respawn.IsRespawning;
            if (gameFlow != null && gameFlow.CurrentPhase == PetitParisGameFlow.Phase.Completed) ratBusy = true;

            switch (CurrentState)
            {
                case State.Idle:
                    if (!ratBusy && autoNotice && Time.time >= cooldownUntil && CanSeeRat()) Notice(-1f);
                    break;

                case State.Noticed:
                    timer -= dt;
                    if (ratBusy) { StopChase(false); break; }
                    if (timer <= 0f) StartChase();
                    break;

                case State.Chasing:
                    timer -= dt;
                    hiddenTimer = danger.IsSafe ? hiddenTimer + dt : 0f;
                    if (ratBusy) { StopChase(false); break; }
                    if (hiddenTimer >= loseAfterHidden || timer <= 0f) StopChase(true);
                    break;

                case State.Lost:
                    timer -= dt;
                    if (timer <= 0f) CurrentState = State.Idle;
                    break;
            }

            if (bubble != null && bubble.gameObject.activeSelf && Time.time > bubbleUntil && CurrentState != State.Noticed)
                bubble.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // 말풍선은 항상 쥐(카메라) 쪽을 본다
            if (bubble == null || !bubble.gameObject.activeSelf || rig == null || rig.EyeCamera == null) return;
            Vector3 away = bubble.transform.position - rig.EyeCamera.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 1e-6f) bubble.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        bool CanSeeRat()
        {
            if (danger.IsSafe) return false;
            Vector3 to = ratTarget.position - path.GroundPosition;
            to.y = 0f;
            float radius = noticeRadius * (crouch != null && crouch.IsCrouched ? crouchMultiplier : 1f);
            if (to.sqrMagnitude > radius * radius) return false;
            if (to.sqrMagnitude < 0.04f) return true; // 바로 발밑
            return Vector3.Angle(path.Forward, to) <= noticeAngle;
        }

        /// <summary>이벤트용: 바로 발견 → 추격 (duration초). 진행 중이면 무시.</summary>
        public void ForceChase(float duration)
        {
            if (IsHunting) return;
            Notice(duration);
        }

        void Notice(float forced)
        {
            forcedDuration = forced;
            CurrentState = State.Noticed;
            timer = reactTime;
            path.PauseAndLook(reactTime + 0.1f, ratTarget);
            Say(PetitParisText.ShoutNotice, reactTime + 0.2f);
            if (voice != null) voice.PlayOneShot(PetitParisAudio.Alert, 0.9f);
            if (gameFlow != null) gameFlow.PostMessage(PetitParisText.MessageSpotted);
            AnyNoticed?.Invoke(this);
        }

        void StartChase()
        {
            CurrentState = State.Chasing;
            timer = forcedDuration > 0f ? forcedDuration : chaseDuration;
            hiddenTimer = 0f;
            Bounds area = path.PathBounds;
            area.Expand(new Vector3(chaseAreaPadding * 2f, 10f, chaseAreaPadding * 2f));
            if (useCustomArea) area = customArea;
            path.BeginChase(ratTarget, chaseSpeed, chaseTurnSpeed, area);
            Say(shout, 1.6f);
        }

        void StopChase(bool lostRat)
        {
            path.EndChase();
            CurrentState = lostRat ? State.Lost : State.Idle;
            timer = 1.5f;
            cooldownUntil = Time.time + cooldown;
            if (lostRat)
            {
                Say(PetitParisText.ShoutLost, 1.5f);
                if (gameFlow != null && danger.IsSafe) gameFlow.PostMessage(PetitParisText.MessageEscaped);
            }
            else if (bubble != null) bubble.gameObject.SetActive(false);
            ChaseEnded?.Invoke();
        }

        void Say(string text, float seconds)
        {
            if (bubble == null) return;
            bubble.gameObject.SetActive(true);
            bubble.text = text;
            bubbleUntil = Time.time + seconds;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
            var p = GetComponent<PedestrianPath>();
            Vector3 c = p != null && Application.isPlaying ? p.GroundPosition : transform.position;
            Gizmos.DrawWireSphere(c, noticeRadius);
        }
    }
}
