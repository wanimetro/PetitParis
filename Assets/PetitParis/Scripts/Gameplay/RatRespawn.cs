using System;
using System.Collections;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 실패 → 짧은 페이드 → 가까운 체크포인트에서 재시작.
    /// - 쥐를 물리적으로 날리거나 카메라를 밀지 않는다. 화면이 어두워진 동안 XR Origin만 순간이동한다.
    /// - 순간이동 중에는 이동 큐를 멈추고 CharacterController를 잠시 끈다 (XR Origin 위치가 충돌로 되돌려지지 않게).
    /// - 이동 후 캘리브레이션을 다시 해서 눈높이와 정면 방향을 체크포인트에 맞춘다.
    /// - 재시작 직후 잠깐 무적 (같은 자리에서 연속 실패 방지).
    /// </summary>
    [DisallowMultipleComponent]
    public class RatRespawn : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatLocomotionController locomotion;
        [SerializeField] RatDashController dash;
        [SerializeField] RatJumpController jump;
        [SerializeField] RatCrouchController crouch;
        [SerializeField] RatEyeCalibrator calibrator;
        [SerializeField] ScreenFader fader;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] AudioSource sfx;

        [Header("전환 시간(초)")]
        [SerializeField, Range(0.05f, 1f)] float fadeOutTime = 0.25f;
        [SerializeField, Range(0f, 1f)] float holdTime = 0.25f;
        [SerializeField, Range(0.05f, 1f)] float fadeInTime = 0.4f;

        [Tooltip("재시작 후 무적 시간(초).")]
        [SerializeField, Range(0f, 5f)] float invulnerableTime = 1.5f;

        [Tooltip("이 높이(월드 Y)보다 떨어지면 실패 처리.")]
        [SerializeField] float killHeight = -1f;

        float invulnerableUntil;

        public bool IsRespawning { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public int DeathCount { get; private set; }
        public string LastReason { get; private set; }

        public event Action<string> Died;
        public event Action Respawned;

        void Awake()
        {
            if (rig == null) rig = GetComponent<RatScaleRig>();
            if (locomotion == null) locomotion = GetComponentInChildren<RatLocomotionController>(true);
            if (dash == null) dash = GetComponentInChildren<RatDashController>(true);
            if (jump == null) jump = GetComponentInChildren<RatJumpController>(true);
            if (crouch == null) crouch = GetComponentInChildren<RatCrouchController>(true);
            if (calibrator == null) calibrator = GetComponent<RatEyeCalibrator>();
            if (fader == null) fader = GetComponentInChildren<ScreenFader>(true);
            if (danger == null) danger = GetComponent<RatDangerDetector>();
            if (checkpoints == null) checkpoints = FindAnyObjectByType<CheckpointManager>();
            if (sfx != null) PetitParisAudio.Configure2D(sfx);
        }

        void Update()
        {
            if (!IsRespawning && rig != null && rig.transform.position.y < killHeight)
                Kill(PetitParisText.MessageFell);
        }

        /// <summary>실패 처리. 이미 처리 중이거나 무적이면 무시.</summary>
        public void Kill(string reason)
        {
            if (IsRespawning || IsInvulnerable || !isActiveAndEnabled) return;
            StartCoroutine(RespawnRoutine(reason));
        }

        IEnumerator RespawnRoutine(string reason)
        {
            IsRespawning = true;
            DeathCount++;
            LastReason = reason;
            Died?.Invoke(reason);

            if (locomotion != null) locomotion.SetInputLocked(true);
            if (sfx != null) sfx.PlayOneShot(PetitParisAudio.Fail, 0.8f);
            if (fader != null) yield return fader.Fade(1f, fadeOutTime);

            Vector3 deathPosition = rig != null ? rig.transform.position : Vector3.zero;
            if (locomotion != null) locomotion.SetSuspended(true);
            yield return null; // 이미 큐에 들어간 이동이 처리되도록 한 프레임 대기

            Transform target = checkpoints != null ? checkpoints.GetRespawnPoint(deathPosition) : null;
            if (target != null) TeleportTo(target);
            else Debug.LogWarning("[PetitParis] 체크포인트가 없어 제자리에서 재시작합니다.", this);

            if (crouch != null) crouch.ForceStand();
            if (dash != null) dash.ResetState();
            if (jump != null) jump.ResetState();
            if (danger != null) danger.ResetState();
            if (calibrator != null) calibrator.CalibrateImmediate(); // 화면이 검은 동안 눈높이·정면 재정렬

            yield return new WaitForSecondsRealtime(holdTime);

            if (locomotion != null)
            {
                locomotion.SetSuspended(false);
                locomotion.SetInputLocked(false);
            }
            invulnerableUntil = Time.time + invulnerableTime;
            if (fader != null) yield return fader.Fade(0f, fadeInTime);

            IsRespawning = false;
            Respawned?.Invoke();
        }

        /// <summary>XR Origin을 목표 위치로 옮긴다 (Yaw만 사용). 카메라는 건드리지 않는다.</summary>
        public void TeleportTo(Transform target)
        {
            if (rig == null || target == null) return;
            var body = rig.Body;
            bool wasEnabled = body != null && body.enabled;
            if (body != null) body.enabled = false;
            rig.transform.SetPositionAndRotation(target.position, Quaternion.Euler(0f, target.eulerAngles.y, 0f));
            if (body != null) body.enabled = wasEnabled;
            Physics.SyncTransforms();
        }
    }
}
