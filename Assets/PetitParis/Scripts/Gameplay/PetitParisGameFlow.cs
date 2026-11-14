using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PetitParis
{
    /// <summary>
    /// 게임 진행: 조작 안내 → 탐험 → 목적지 도착 → 완료 화면 → (A 버튼) 처음부터 다시.
    /// - 조작 안내 패널은 시작 지점 앞 월드 공간에 두고, 조금 움직이거나 시간이 지나면 사라진다 (머리 고정 HUD 아님)
    /// - 다음 목표(체크포인트 → 출구)에 작은 표시(RatObjectiveMarker)를 띄운다
    /// - 체크포인트·숨기·실패 때 짧은 메시지를 RatStatusPanel에 전달
    /// - 다시 시작은 씬을 다시 불러온다 (모든 NPC·체크포인트 초기화)
    /// </summary>
    [DisallowMultipleComponent]
    public class PetitParisGameFlow : MonoBehaviour
    {
        public enum Phase { Intro, Playing, Completed }

        [Header("참조")]
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatInputReader input;
        [SerializeField] RatLocomotionController locomotion;
        [SerializeField] RatRespawn respawn;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] ScreenFader fader;
        [SerializeField] RatObjectiveMarker objectiveMarker;
        [SerializeField] AudioSource sfx;
        [SerializeField] CrumbCollection crumbs;

        [Header("목적지")]
        [Tooltip("목적지 구역 (Trigger Collider). 쥐 몸 중심이 들어오면 완료.")]
        [SerializeField] Collider goalVolume;
        [SerializeField] Transform goalMarkerTarget;

        [Header("안내 패널")]
        [SerializeField] GameObject introPanel;
        [SerializeField] TMP_Text introText;
        [Tooltip("시작 지점에서 이만큼(월드 m) 움직이면 조작 안내를 숨긴다.")]
        [SerializeField, Range(0.05f, 2f)] float introHideDistance = 0.35f;
        [SerializeField, Range(3f, 120f)] float introMaxTime = 30f;

        [Header("완료 패널")]
        [SerializeField] Transform completePanel;
        [SerializeField] TMP_Text completeText;
        [Tooltip("완료 패널을 카메라 앞 이 거리(월드 m)에 띄운다. 0.3 = 체감 3m.")]
        [SerializeField, Range(0.05f, 1f)] float completePanelDistance = 0.3f;
        [SerializeField, Range(0f, 5f)] float restartInputDelay = 1.5f;

        [Header("메시지")]
        [SerializeField, Range(0.5f, 6f)] float messageDuration = 2.5f;

        Vector3 startPosition;
        float introTimer;
        float completedAt;
        bool restarting;
        bool hideMessageShown;

        public Phase CurrentPhase { get; private set; } = Phase.Intro;
        public float ElapsedTime { get; private set; }
        public string CurrentMessage { get; private set; }
        public float MessageUntil { get; private set; }

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null)
            {
                if (input == null) input = rig.GetComponent<RatInputReader>();
                if (respawn == null) respawn = rig.GetComponent<RatRespawn>();
                if (danger == null) danger = rig.GetComponent<RatDangerDetector>();
                if (locomotion == null) locomotion = rig.GetComponentInChildren<RatLocomotionController>(true);
                if (fader == null) fader = rig.GetComponentInChildren<ScreenFader>(true);
            }
            if (checkpoints == null) checkpoints = FindAnyObjectByType<CheckpointManager>();
            if (crumbs == null) crumbs = FindAnyObjectByType<CrumbCollection>();
            if (sfx != null) PetitParisAudio.Configure2D(sfx);
            if (goalVolume == null) Debug.LogError("[PetitParis] 목적지(goalVolume)가 연결되지 않았습니다.", this);
        }

        void OnEnable()
        {
            if (checkpoints != null) checkpoints.CheckpointReached += OnCheckpoint;
            if (danger != null) danger.HidFromDanger += OnHid;
            if (respawn != null) respawn.Died += OnDied;
        }

        void OnDisable()
        {
            if (checkpoints != null) checkpoints.CheckpointReached -= OnCheckpoint;
            if (danger != null) danger.HidFromDanger -= OnHid;
            if (respawn != null) respawn.Died -= OnDied;
        }

        void Start()
        {
            startPosition = rig != null ? rig.transform.position : Vector3.zero;
            if (introPanel != null) introPanel.SetActive(true);
            if (introText != null) introText.text = PetitParisText.IntroBody;
            if (completePanel != null) completePanel.gameObject.SetActive(false);
            CurrentPhase = Phase.Intro;
        }

        void Update()
        {
            if (rig == null) return;
            float dt = Time.deltaTime;

            if (CurrentPhase != Phase.Completed) ElapsedTime += dt;

            switch (CurrentPhase)
            {
                case Phase.Intro:
                    introTimer += dt;
                    Vector3 moved = rig.transform.position - startPosition;
                    moved.y = 0f;
                    if (moved.magnitude > introHideDistance || introTimer > introMaxTime) HideIntro();
                    UpdateObjective();
                    CheckGoal();
                    break;

                case Phase.Playing:
                    UpdateObjective();
                    CheckGoal();
                    break;

                case Phase.Completed:
                    if (!restarting && input != null && input.JumpPressedThisFrame && Time.time - completedAt > restartInputDelay)
                        StartCoroutine(RestartRoutine());
                    break;
            }
        }

        void HideIntro()
        {
            if (introPanel != null) introPanel.SetActive(false);
            CurrentPhase = Phase.Playing;
        }

        void UpdateObjective()
        {
            if (objectiveMarker == null) return;
            Transform next = checkpoints != null ? checkpoints.NextTarget : null;
            objectiveMarker.SetTarget(next != null ? next : goalMarkerTarget);
        }

        void CheckGoal()
        {
            if (goalVolume == null || danger == null || (respawn != null && respawn.IsRespawning)) return;
            if (RatSafeZone.ColliderContains(goalVolume, danger.BodyCenter)) Complete();
        }

        void Complete()
        {
            if (CurrentPhase == Phase.Completed) return;
            if (introPanel != null) introPanel.SetActive(false);
            CurrentPhase = Phase.Completed;
            completedAt = Time.time;
            if (locomotion != null) locomotion.SetInputLocked(true);
            if (objectiveMarker != null) objectiveMarker.SetTarget(null);
            if (sfx != null) sfx.PlayOneShot(PetitParisAudio.Success, 0.8f);
            ShowCompletePanel();
        }

        void ShowCompletePanel()
        {
            if (completePanel == null) return;
            Transform cam = rig.EyeCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f) forward = rig.transform.forward;
            forward.Normalize();
            completePanel.SetPositionAndRotation(cam.position + forward * completePanelDistance, Quaternion.LookRotation(forward, Vector3.up));
            completePanel.gameObject.SetActive(true);

            if (completeText != null)
            {
                int total = Mathf.FloorToInt(ElapsedTime);
                int deaths = respawn != null ? respawn.DeathCount : 0;
                string scoreLines = "";
                if (crumbs != null)
                {
                    int stars = crumbs.Stars(true);
                    scoreLines =
                        $"<size=170%><color=#ffd040>{CrumbCollection.StarString(stars)}</color></size>\n" +
                        $"{PetitParisText.CompleteScore}  {crumbs.Score} / {crumbs.TotalPoints}\n" +
                        $"{PetitParisText.CompleteGolden}  {crumbs.GoldenCount} / {crumbs.GoldenTotal}\n";
                }
                completeText.text =
                    $"<size=150%><b>{PetitParisText.CompleteTitle}</b></size>\n{PetitParisText.CompleteBody}\n\n" +
                    scoreLines +
                    $"{PetitParisText.CompleteTime}  {total / 60}:{total % 60:00}\n" +
                    $"{PetitParisText.CompleteDeaths}  {deaths}\n\n" +
                    (crumbs != null ? $"<size=70%>{PetitParisText.CompleteStarRule}</size>\n" : "") +
                    $"{PetitParisText.CompleteRestart}";
            }
        }

        IEnumerator RestartRoutine()
        {
            restarting = true;
            if (fader != null) yield return fader.Fade(1f, 0.4f);
            SceneManager.LoadScene(gameObject.scene.name);
        }

        void OnCheckpoint(int index)
        {
            if (index > 0) PostMessage(PetitParisText.MessageCheckpoint);
        }

        void OnHid(RatSafeZone zone)
        {
            PostMessage(PetitParisText.MessageHidden);
            hideMessageShown = true;
        }

        void OnDied(string reason)
        {
            PostMessage(reason == PetitParisText.MessageFell ? PetitParisText.MessageFell : PetitParisText.MessageDied);
        }

        public void PostMessage(string message)
        {
            CurrentMessage = message;
            MessageUntil = Time.time + messageDuration;
        }

        public bool HasShownHideMessage => hideMessageShown;
    }
}
