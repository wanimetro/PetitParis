using System.Collections;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 큰 낙하 완충 (AI 리뷰 ISSUE-02). 주방 널빤지·상자(0.3~0.9m)에서 떨어지면
    /// 바닥에 닿기 전에 화면을 어둡게 하고, 떨어지기 직전 서 있던 자리로 되돌린다.
    /// - 실패(리스폰)가 아니다: 체크포인트·부스러기·사망 횟수는 그대로
    /// - 카메라는 흔들거나 기울이지 않는다. 페이드 + XR Origin 순간이동만 쓴다 (RatRespawn과 같은 방식)
    /// - 리스폰 중이면 아무것도 하지 않고, 리스폰은 낙하 복귀 중에 시작되지 않는다 (RatRespawn이 IsRecovering 확인)
    /// 거리 단위는 월드 미터. maxSafeDrop 0.2 = 사람 체감 2m.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(120)]
    public class RatFallRecovery : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatLocomotionController locomotion;
        [SerializeField] RatRespawn respawn;
        [SerializeField] ScreenFader fader;
        [SerializeField] PetitParisGameFlow gameFlow;

        [Tooltip("땅을 떠난 높이보다 이만큼(월드 m) 아래로 떨어지면 되돌린다. 0.2 = 체감 2m.")]
        [SerializeField, Range(0.05f, 1f)] float maxSafeDrop = 0.2f;
        [Tooltip("되돌릴 자리: 땅을 떠나기 이만큼(초) 전에 서 있던 곳 (가장자리에서 조금 안쪽).")]
        [SerializeField, Range(0.1f, 1.5f)] float rewindSeconds = 0.4f;
        [SerializeField, Range(0.05f, 1f)] float fadeOutTime = 0.18f;
        [SerializeField, Range(0.05f, 1f)] float fadeInTime = 0.35f;

        const int HistorySize = 32;
        readonly Vector3[] positions = new Vector3[HistorySize];
        readonly float[] times = new float[HistorySize];
        int head;
        int count;
        float nextSample;
        bool airborne;
        float takeoffY;
        Vector3 recoverPosition;

        public bool IsRecovering { get; private set; }
        public int RecoverCount { get; private set; }
        public event System.Action Recovered;

        void Awake()
        {
            if (rig == null) rig = GetComponent<RatScaleRig>();
            if (respawn == null) respawn = GetComponent<RatRespawn>();
            if (locomotion == null) locomotion = GetComponentInChildren<RatLocomotionController>(true);
            if (fader == null) fader = GetComponentInChildren<ScreenFader>(true);
        }

        void Start()
        {
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (respawn != null) respawn.Respawned += ClearHistory;
        }

        void OnDestroy()
        {
            if (respawn != null) respawn.Respawned -= ClearHistory;
        }

        void ClearHistory()
        {
            count = 0;
            airborne = false;
        }

        void Update()
        {
            if (IsRecovering || rig == null || locomotion == null || locomotion.Body == null || !locomotion.Body.enabled) return;
            if (respawn != null && respawn.IsRespawning) { ClearHistory(); return; }

            Vector3 p = rig.transform.position;
            if (locomotion.IsGrounded)
            {
                airborne = false;
                if (Time.time >= nextSample)
                {
                    nextSample = Time.time + 0.05f;
                    positions[head] = p;
                    times[head] = Time.time;
                    head = (head + 1) % HistorySize;
                    count = Mathf.Min(count + 1, HistorySize);
                }
                return;
            }

            if (!airborne)
            {
                airborne = true;
                takeoffY = count > 0 ? positions[(head - 1 + HistorySize) % HistorySize].y : p.y;
                recoverPosition = PickRecoverPosition(p);
            }
            if (takeoffY - p.y > maxSafeDrop) StartCoroutine(Recover());
        }

        Vector3 PickRecoverPosition(Vector3 fallback)
        {
            if (count == 0) return fallback;
            float wanted = Time.time - rewindSeconds;
            Vector3 best = positions[(head - 1 + HistorySize) % HistorySize];
            for (int i = 1; i <= count; i++)
            {
                int idx = (head - i + HistorySize) % HistorySize;
                best = positions[idx];
                if (times[idx] <= wanted) break;
            }
            return best;
        }

        IEnumerator Recover()
        {
            IsRecovering = true;
            locomotion.SetInputLocked(true);
            if (fader != null) yield return fader.Fade(1f, fadeOutTime);
            locomotion.SetSuspended(true);
            yield return null;

            var body = rig.Body;
            bool wasEnabled = body != null && body.enabled;
            if (body != null) body.enabled = false;
            rig.transform.position = recoverPosition;
            if (body != null) body.enabled = wasEnabled;
            Physics.SyncTransforms();

            locomotion.SetSuspended(false);
            locomotion.SetInputLocked(false);
            ClearHistory();
            RecoverCount++;
            if (gameFlow != null) gameFlow.PostMessage(PetitParisText.MessageFallRecovered, 1);
            Recovered?.Invoke();
            if (fader != null) yield return fader.Fade(0f, fadeInTime);
            IsRecovering = false;
        }
    }
}
