using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PetitParis
{
    /// <summary>
    /// 게임 진행: 조작 안내 → 탐험 → 목적지(주방 치즈) 도착 → 완료 화면 → (A 버튼) 처음부터 다시.
    /// - 조작 안내 패널은 시작 지점 앞 월드 공간에 두고, A(PC: Space)를 눌러야 닫힌다. 그동안 이동 입력을 막는다 (ISSUE-06)
    /// - X(PC: Tab) 메뉴: 게임을 멈추고 조작법·규칙을 다시 보여 준다. 메뉴에서 A(Space)로 편안함 모드 전환 (ISSUE-03, 06, 18)
    /// - Quest 시스템 메뉴 등으로 앱이 포커스를 잃으면 자동으로 메뉴(일시 정지)를 연다 (ISSUE-13, 실기기 빌드에서만)
    /// - 메시지는 우선순위 큐로 관리한다: 실패 메시지(2) > 들킴·이벤트(1) > 일반(0). 높은 메시지가 낮은 메시지를 덮지 않게
    ///   낮은 메시지는 뒤로 미룬다 (ISSUE-05)
    /// - 실패 메시지는 원인(신발·자전거·빗자루·바게트·낙하)별 문장 + 짧은 재도전 힌트, 리스폰 뒤에도 읽을 수 있게 4.5초
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
        [Tooltip("켜면 A(Space)를 누를 때까지 이동을 막는다.")]
        [SerializeField] bool lockMovementDuringIntro = true;
        [Tooltip("0보다 크면 이 시간(초)이 지나면 안내를 자동으로 닫는다. 0 = 직접 닫을 때까지.")]
        [SerializeField, Range(0f, 300f)] float introAutoCloseTime = 0f;

        [Header("메뉴 (X / Tab)")]
        [SerializeField] Transform menuPanel;
        [SerializeField] TMP_Text menuText;
        [SerializeField, Range(0.05f, 1f)] float menuPanelDistance = 0.3f;
        [Tooltip("앱이 포커스를 잃으면(Quest 시스템 메뉴 등) 일시 정지 메뉴를 연다. 에디터에서는 동작하지 않는다.")]
        [SerializeField] bool pauseOnFocusLoss = true;

        [Header("완료 패널")]
        [SerializeField] Transform completePanel;
        [SerializeField] TMP_Text completeText;
        [Tooltip("완료 패널을 카메라 앞 이 거리(월드 m)에 띄운다. 0.3 = 체감 3m.")]
        [SerializeField, Range(0.05f, 1f)] float completePanelDistance = 0.3f;
        [SerializeField, Range(0f, 5f)] float restartInputDelay = 1.5f;

        [Header("메시지")]
        [SerializeField, Range(0.5f, 6f)] float messageDuration = 2.5f;
        [Tooltip("실패 메시지 표시 시간(초). 리스폰 페이드(약 0.9초) 뒤에도 읽을 수 있게.")]
        [SerializeField, Range(1f, 8f)] float failMessageDuration = 4.5f;

        struct Message
        {
            public string text;
            public int priority;
            public float duration;
        }

        readonly List<Message> queue = new List<Message>();
        int currentPriority = -1;
        float introTimer;
        float completedAt;
        bool restarting;
        bool hideMessageShown;
        bool unlockNextFrame;
        float savedTimeScale = 1f;

        public Phase CurrentPhase { get; private set; } = Phase.Intro;
        public float ElapsedTime { get; private set; }
        public string CurrentMessage { get; private set; }
        public float MessageUntil { get; private set; }
        public bool IsMenuOpen { get; private set; }

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
            if (IsMenuOpen) SetPaused(false); // 씬을 떠날 때 시간 정지가 남지 않게
        }

        void Start()
        {
            if (introPanel != null) introPanel.SetActive(true);
            if (introText != null) introText.text = "<b>" + PetitParisText.Title + "</b>\n" + PetitParisText.IntroBody;
            if (completePanel != null) completePanel.gameObject.SetActive(false);
            if (menuPanel != null) menuPanel.gameObject.SetActive(false);
            CurrentPhase = Phase.Intro;
            if (lockMovementDuringIntro && locomotion != null) locomotion.SetInputLocked(true);
        }

        void Update()
        {
            if (rig == null) return;
            float dt = Time.deltaTime;

            if (unlockNextFrame)
            {
                unlockNextFrame = false;
                if (locomotion != null && CurrentPhase != Phase.Completed && !IsMenuOpen) locomotion.SetInputLocked(false);
            }
            UpdateMessageQueue();

            // 메뉴 (완료 화면에서는 쓰지 않음)
            if (input != null && CurrentPhase != Phase.Completed && input.MenuPressedThisFrame)
            {
                SetMenuOpen(!IsMenuOpen);
                return;
            }
            if (IsMenuOpen)
            {
                if (input != null && input.JumpPressedThisFrame)
                {
                    RatComfortRuntime.ComfortMode = !RatComfortRuntime.ComfortMode;
                    PostMessage(RatComfortRuntime.ComfortMode ? PetitParisText.MessageComfortOn : PetitParisText.MessageComfortOff);
                    RefreshMenuText();
                }
                return;
            }

            if (CurrentPhase != Phase.Completed) ElapsedTime += dt;

            switch (CurrentPhase)
            {
                case Phase.Intro:
                    introTimer += dt;
                    bool close = input != null && input.JumpPressedThisFrame;
                    if (introAutoCloseTime > 0f && introTimer > introAutoCloseTime) close = true;
                    // 자동 테스트(순간이동) 등으로 이미 멀리 갔으면 안내를 닫는다
                    if (respawn != null && respawn.DeathCount > 0) close = true;
                    if (close) HideIntro();
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

        /// <summary>조작 안내를 닫는다 (자동 테스트용으로도 공개).</summary>
        public void CloseIntro()
        {
            if (CurrentPhase == Phase.Intro) HideIntro();
        }

        void HideIntro()
        {
            if (introPanel != null) introPanel.SetActive(false);
            CurrentPhase = Phase.Playing;
            unlockNextFrame = true; // 같은 프레임의 A 입력이 점프로 이어지지 않게 다음 프레임에 이동을 푼다
        }

        // ───────── 메뉴·일시 정지 ─────────

        void SetMenuOpen(bool open)
        {
            if (open == IsMenuOpen) return;
            IsMenuOpen = open;
            if (menuPanel != null)
            {
                if (open) PlaceInFront(menuPanel, menuPanelDistance);
                menuPanel.gameObject.SetActive(open);
            }
            if (open) RefreshMenuText();
            SetPaused(open);
            if (locomotion != null)
            {
                if (open) locomotion.SetInputLocked(true);
                else if (CurrentPhase == Phase.Playing) unlockNextFrame = true;
            }
        }

        void SetPaused(bool paused)
        {
            if (paused)
            {
                savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else
            {
                Time.timeScale = savedTimeScale;
                AudioListener.pause = false;
            }
        }

        void RefreshMenuText()
        {
            if (menuText == null) return;
            menuText.text =
                $"<size=130%><b>{PetitParisText.MenuTitle}</b></size>\n\n" +
                PetitParisText.ControlsBody + "\n\n" +
                $"<size=85%>{PetitParisText.MenuRules}</size>\n\n" +
                (RatComfortRuntime.ComfortMode ? PetitParisText.MenuComfortOn : PetitParisText.MenuComfortOff) + "\n\n" +
                PetitParisText.MenuFooter;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) PauseForSystem();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) PauseForSystem();
        }

        void PauseForSystem()
        {
            if (Application.isEditor || !pauseOnFocusLoss || CurrentPhase == Phase.Completed || restarting) return;
            SetMenuOpen(true);
        }

        void PlaceInFront(Transform panel, float distance)
        {
            Transform cam = rig.EyeCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f) forward = rig.transform.forward;
            forward.Normalize();
            panel.SetPositionAndRotation(cam.position + forward * distance, Quaternion.LookRotation(forward, Vector3.up));
        }

        // ───────── 진행 ─────────

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
            // 패널은 깊이 검사 없이 그린다 (빌더의 MakeOverlay). 치즈 바퀴에 코를 대고 도착해 패널 자리가 바퀴 속이어도 보인다.
            PlaceInFront(completePanel, completePanelDistance);
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
                    // 작은 글씨 줄 다음에 빈 줄: 바로 다음 줄이 작은 줄 간격으로 붙어 겹치는 TMP 동작을 피한다
                    (crumbs != null ? $"<size=70%>{PetitParisText.CompleteStarRule}</size>\n\n" : "") +
                    $"{PetitParisText.CompleteRestart}";
            }
        }

        IEnumerator RestartRoutine()
        {
            restarting = true;
            if (fader != null) yield return fader.Fade(1f, 0.4f);
            Time.timeScale = 1f;
            AudioListener.pause = false;
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
            PostMessage(FailMessage(reason), 2, failMessageDuration);
        }

        /// <summary>실패 원인(FootstepHazard.hazardName 또는 낙하) → 메시지 + 재도전 힌트.</summary>
        public static string FailMessage(string reason)
        {
            switch (reason)
            {
                case PetitParisText.HazardShoe: return PetitParisText.FailShoe;
                case PetitParisText.HazardBike: return PetitParisText.FailBike;
                case PetitParisText.HazardBroom: return PetitParisText.FailBroom;
                case PetitParisText.HazardBaguette: return PetitParisText.FailBaguette;
                case PetitParisText.MessageFell: return PetitParisText.FailFell;
                default: return PetitParisText.MessageDied;
            }
        }

        // ───────── 메시지 큐 ─────────

        /// <summary>
        /// 메시지를 보여 준다. priority: 0 일반, 1 들킴·이벤트, 2 실패.
        /// 지금 보이는 메시지보다 우선순위가 낮으면 뒤로 미루고, 높으면 지금 메시지를 뒤로 미룬 뒤 바로 보여 준다.
        /// </summary>
        public void PostMessage(string message, int priority = 0, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message)) return;
            var m = new Message { text = message, priority = priority, duration = duration > 0f ? duration : messageDuration };
            bool showing = !string.IsNullOrEmpty(CurrentMessage) && Time.time < MessageUntil;
            if (showing && CurrentMessage == message) { MessageUntil = Mathf.Max(MessageUntil, Time.time + m.duration); return; }
            if (showing && currentPriority > priority)
            {
                Enqueue(m, false);
                return;
            }
            if (showing && currentPriority >= 0 && currentPriority < priority)
                Enqueue(new Message { text = CurrentMessage, priority = currentPriority, duration = messageDuration }, true);
            Show(m);
        }

        void Enqueue(Message m, bool front)
        {
            foreach (var q in queue) if (q.text == m.text) return;
            if (front) queue.Insert(0, m); else queue.Add(m);
            while (queue.Count > 3) queue.RemoveAt(queue.Count - 1);
        }

        void Show(Message m)
        {
            CurrentMessage = m.text;
            currentPriority = m.priority;
            MessageUntil = Time.time + m.duration;
        }

        void UpdateMessageQueue()
        {
            if (Time.time < MessageUntil || queue.Count == 0) return;
            // 가장 높은 우선순위부터
            int best = 0;
            for (int i = 1; i < queue.Count; i++) if (queue[i].priority > queue[best].priority) best = i;
            var m = queue[best];
            queue.RemoveAt(best);
            Show(m);
        }

        public bool HasShownHideMessage => hideMessageShown;
    }
}
