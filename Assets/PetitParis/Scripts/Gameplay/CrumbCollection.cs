using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 빵 부스러기 점수와 별점.
    /// - 쥐 몸 중심이 CrumbPickup 반경에 들어오면 줍는다 (거리 판정, 매 프레임)
    /// - 체크포인트에 닿기 전에 밟히면, 그동안 모은 조각을 떨어뜨린다 (원래 자리로 돌아감) → 위험한 곳의 큰 조각에 긴장감
    /// - 체크포인트에 닿으면 들고 있던 조각이 확정된다
    /// - 주운 자리에 "+1" 글자가 떠오른다 (월드 공간, 카메라를 향함)
    /// - 별점: ★ 도착 / ★ 부스러기 총점의 60% 이상 / ★ 황금 크루아상 전부
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(110)] // RatDangerDetector(100) 뒤: 몸 중심이 갱신된 다음 판정
    public class CrumbCollection : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] RatRespawn respawn;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] PetitParisGameFlow gameFlow;
        [SerializeField] AudioSource sfx;

        [Header("+1 글자")]
        [SerializeField] TMP_Text popup;
        [SerializeField, Range(0.2f, 2f)] float popupDuration = 0.8f;
        [SerializeField, Range(0.01f, 0.2f)] float popupRise = 0.06f;

        [Header("별점")]
        [SerializeField, Range(0.1f, 1f)] float scoreStarRatio = 0.6f;

        readonly List<CrumbPickup> carried = new List<CrumbPickup>();
        Transform bodyAnchor;
        float popupTimer = -1f;
        Vector3 popupStart;
        Color popupColor = Color.white;

        public int Score { get; private set; }
        public int CollectedCount { get; private set; }
        public int GoldenCount { get; private set; }
        public int GoldenTotal { get; private set; }
        /// <summary>처음부터 레벨에 놓인 조각의 총점 (게임 중에 생긴 보너스 조각 제외).</summary>
        public int TotalPoints { get; private set; }
        public int CarriedCount => carried.Count;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null)
            {
                if (danger == null) danger = rig.GetComponent<RatDangerDetector>();
                if (respawn == null) respawn = rig.GetComponent<RatRespawn>();
            }
            if (checkpoints == null) checkpoints = FindAnyObjectByType<CheckpointManager>();
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (sfx != null) PetitParisAudio.Configure2D(sfx);
            if (popup != null) popup.gameObject.SetActive(false);

            // 조각이 빨려 들어갈 지점 (쥐 몸 중심을 따라다님)
            bodyAnchor = new GameObject("CrumbTarget").transform;
            bodyAnchor.SetParent(transform, false);
        }

        void Start()
        {
            foreach (var c in FindObjectsByType<CrumbPickup>(FindObjectsInactive.Exclude))
            {
                if (c.IsBonus) continue;
                TotalPoints += c.Points;
                if (c.IsGolden) GoldenTotal++;
            }
        }

        void OnEnable()
        {
            if (checkpoints != null) checkpoints.CheckpointReached += OnCheckpoint;
            if (respawn != null) respawn.Died += OnDied;
        }

        void OnDisable()
        {
            if (checkpoints != null) checkpoints.CheckpointReached -= OnCheckpoint;
            if (respawn != null) respawn.Died -= OnDied;
        }

        void Update()
        {
            UpdatePopup();
            if (danger == null || (respawn != null && respawn.IsRespawning)) return;
            if (gameFlow != null && gameFlow.CurrentPhase == PetitParisGameFlow.Phase.Completed) return;

            Vector3 center = danger.BodyCenter;
            bodyAnchor.position = center;
            var list = CrumbPickup.Active;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var c = list[i];
                if (c == null || !c.IsAvailable) continue;
                float r = c.PickupRadius;
                if ((c.transform.position - center).sqrMagnitude > r * r) continue;
                Pick(c);
            }
        }

        void Pick(CrumbPickup c)
        {
            c.Collect(bodyAnchor);
            carried.Add(c);
            Score += c.Points;
            CollectedCount++;
            if (c.IsGolden)
            {
                GoldenCount++;
                if (gameFlow != null) gameFlow.PostMessage(PetitParisText.MessageGolden);
            }
            if (sfx != null)
            {
                sfx.pitch = c.IsGolden ? 1f : Random.Range(0.95f, 1.15f);
                sfx.PlayOneShot(c.IsGolden ? PetitParisAudio.GoldPickup : PetitParisAudio.Pickup, c.IsGolden ? 0.9f : 0.6f);
            }
            ShowPopup(c.transform.position, c.IsGolden ? "★ +" + c.Points : "+" + c.Points,
                c.IsGolden ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.95f, 0.8f));
        }

        void OnCheckpoint(int index)
        {
            if (carried.Count == 0) return;
            carried.Clear(); // 확정
            if (index > 0 && gameFlow != null) gameFlow.PostMessage(PetitParisText.MessageCrumbsKept);
        }

        void OnDied(string reason)
        {
            if (carried.Count == 0) return;
            int lost = carried.Count;
            foreach (var c in carried)
            {
                if (c == null) continue;
                Score -= c.Points;
                CollectedCount--;
                if (c.IsGolden) GoldenCount--;
                c.Restore();
            }
            carried.Clear();
            if (gameFlow != null) gameFlow.PostMessage(string.Format(PetitParisText.MessageCrumbsLostFormat, lost));
        }

        /// <summary>별 개수 (0~3). arrived = 목적지에 도착했는지.</summary>
        public int Stars(bool arrived)
        {
            if (!arrived) return 0;
            int stars = 1;
            if (TotalPoints > 0 && Score >= Mathf.CeilToInt(TotalPoints * scoreStarRatio)) stars++;
            if (GoldenTotal > 0 && GoldenCount >= GoldenTotal) stars++;
            return stars;
        }

        public static string StarString(int stars)
        {
            var s = "";
            for (int i = 0; i < 3; i++) s += i < stars ? PetitParisText.StarFull : PetitParisText.StarEmpty;
            return s;
        }

        // ───────── +1 글자 ─────────

        void ShowPopup(Vector3 at, string text, Color color)
        {
            if (popup == null) return;
            popup.gameObject.SetActive(true);
            popup.text = text;
            popupColor = color;
            popupStart = at + Vector3.up * 0.04f;
            popupTimer = 0f;
            UpdatePopup();
        }

        void UpdatePopup()
        {
            if (popup == null || popupTimer < 0f) return;
            popupTimer += Time.deltaTime;
            float t = popupTimer / popupDuration;
            if (t >= 1f)
            {
                popupTimer = -1f;
                popup.gameObject.SetActive(false);
                return;
            }
            Vector3 pos = popupStart + Vector3.up * (popupRise * Mathf.Sqrt(t));
            Quaternion rot = popup.transform.rotation;
            if (rig != null && rig.EyeCamera != null)
            {
                Vector3 away = pos - rig.EyeCamera.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 1e-6f) rot = Quaternion.LookRotation(away.normalized, Vector3.up);
            }
            popup.transform.SetPositionAndRotation(pos, rot);
            var c = popupColor;
            c.a = 1f - t * t;
            popup.color = c;
        }
    }
}
