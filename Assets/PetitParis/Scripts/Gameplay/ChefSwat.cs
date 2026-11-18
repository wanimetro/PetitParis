using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 요리사가 높은 곳(널빤지·상자·식탁)의 쥐를 발견하면 주걱으로 내려친다 (최종 테스트 요청: 발견해도 "앗, 쥐다!"만 하고 반응이 없었다).
    /// - 요리사가 쥐를 발견·추격 중(PedestrianAwareness)이고, 쥐가 바닥보다 elevation 이상 높고, reach 안에 있으면
    /// - 쥐가 있는 자리에 붉은 그림자 예고 → warnTime 뒤 주걱이 내려온다 → 그때 그 자리에 있으면 실패(체크포인트)
    /// - 옆으로 피하거나 숨으면(안전 구역) 괜찮다. interval마다 반복
    /// </summary>
    [DisallowMultipleComponent]
    public class ChefSwat : MonoBehaviour
    {
        [SerializeField] PedestrianAwareness chef;
        [SerializeField] Transform tool;
        [SerializeField] FootstepHazard toolHazard;
        [SerializeField] RatBlobShadow marker;
        [SerializeField] AudioSource sfx;
        [SerializeField, Range(0.5f, 4f)] float reach = 2.6f;
        [SerializeField, Range(0.05f, 1f)] float elevation = 0.2f;
        [SerializeField, Range(0.3f, 3f)] float warnTime = 1.1f;
        [SerializeField, Range(0.5f, 6f)] float interval = 1.8f;

        RatScaleRig rig;
        RatDangerDetector danger;
        RatRespawn respawn;
        PetitParisGameFlow flow;
        PedestrianPath path;
        Vector3 target;
        float timer = -1f;
        float nextTime;
        bool messageShown;

        void Awake()
        {
            rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null) { danger = rig.GetComponent<RatDangerDetector>(); respawn = rig.GetComponent<RatRespawn>(); }
            flow = FindAnyObjectByType<PetitParisGameFlow>();
            if (chef != null) path = chef.GetComponent<PedestrianPath>();
            PetitParisAudio.Configure3D(sfx, 0.4f, 10f);
            if (toolHazard != null) toolHazard.enabled = false; // 판정은 여기서 직접 (리스폰 이중 처리 방지)
            Hide();
        }

        void Hide()
        {
            if (tool != null) tool.gameObject.SetActive(false);
            if (marker != null) marker.Set(Vector3.zero, Quaternion.identity, Vector2.one * 0.01f, 0f);
        }

        bool Eligible()
        {
            if (chef == null || path == null || rig == null || danger == null || !chef.IsHunting || danger.IsSafe) return false;
            if (respawn != null && (respawn.IsRespawning || respawn.IsInvulnerable)) return false;
            Vector3 r = rig.transform.position, c = path.GroundPosition;
            return r.y - c.y > elevation && new Vector2(r.x - c.x, r.z - c.z).magnitude < reach;
        }

        void Update()
        {
            if (timer < 0f)
            {
                if (!chef || !chef.IsHunting) messageShown = false;
                if (Time.time < nextTime || !Eligible()) return;
                target = rig.transform.position;
                timer = 0f;
                if (!messageShown && flow != null) { flow.PostMessage(PetitParisText.MessageChefSwat, 1); messageShown = true; }
                if (tool != null) tool.gameObject.SetActive(true);
            }

            timer += Time.deltaTime;
            float k = Mathf.Clamp01(timer / warnTime);
            if (marker != null) marker.Set(target + Vector3.up * 0.008f, Quaternion.Euler(0f, 30f, 0f), new Vector2(0.16f, 0.5f) * Mathf.Lerp(0.6f, 1f, k), Mathf.Lerp(0.2f, 0.75f, k));
            if (tool != null)
            {
                // 위에서 들고 있다가 마지막 0.15초에 내려친다
                float drop = Mathf.Clamp01((timer - (warnTime - 0.15f)) / 0.15f);
                tool.SetPositionAndRotation(target + Vector3.up * Mathf.Lerp(0.55f, 0.02f, drop), Quaternion.Euler(0f, 30f, 0f));
            }

            if (timer >= warnTime)
            {
                if (sfx != null) { sfx.transform.position = target; sfx.PlayOneShot(PetitParisAudio.Thud, 1f); }
                if (toolHazard != null && respawn != null && danger != null && !danger.IsSafe && DistanceToRat() <= 0f)
                    respawn.Kill(PetitParisText.HazardSpatula);
                timer = -1f;
                nextTime = Time.time + interval;
                Invoke(nameof(Hide), 0.35f);
            }
        }

        float DistanceToRat()
        {
            var body = rig.Body;
            if (body == null) return float.MaxValue;
            Vector3 up = body.transform.up, c = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0f, body.height * 0.5f - body.radius);
            return toolHazard.DistanceToCapsule(c + up * half, c - up * half, body.radius, out _);
        }
    }
}
