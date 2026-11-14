using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 위에서 물건(바게트, 양파 등)이 떨어지는 이벤트.
    /// - 쥐가 zone(Trigger Collider) 안에 있으면 interval마다 하나씩 떨어뜨린다
    /// - 떨어질 자리는 쥐가 곧 있을 위치 (현재 위치 + 이동 방향 × 예측 시간)
    /// - 떨어지기 전에 바닥 그림자가 점점 커지고 진해진다 (warningTime초) → 질주로 피한다
    /// - 떨어지는 동안에만 FootstepHazard가 켜진다. 바닥에 닿으면 꺼지고 잠시 누워 있다가 사라진다
    /// - 떨어진 자리에 보너스 빵 부스러기가 생긴다 (crumbTemplate, 최대 maxBonusCrumbs개) → 위험과 보상
    /// 물건은 미리 만들어 둔 items(비활성 자식, 각각 FootstepHazard 포함)를 돌려 쓴다.
    /// 거리 단위는 월드 미터.
    /// </summary>
    [DisallowMultipleComponent]
    public class FallingHazard : MonoBehaviour
    {
        [SerializeField] RatScaleRig rig;
        [SerializeField] RatDangerDetector danger;
        [SerializeField] RatRespawn respawn;
        [SerializeField] PetitParisGameFlow gameFlow;

        [Tooltip("이 구역(Trigger Collider) 안에 쥐가 있을 때만 떨어진다.")]
        [SerializeField] Collider zone;
        [Tooltip("떨어뜨릴 물건들 (비활성 상태로 둔다). 각 물건에 FootstepHazard.")]
        [SerializeField] Transform[] items = new Transform[0];
        [SerializeField] RatBlobShadow[] shadows = new RatBlobShadow[0];

        [SerializeField, Range(0.5f, 5f)] float dropHeight = 2.4f;
        [SerializeField, Range(0.3f, 3f)] float warningTime = 1.1f;
        [SerializeField, Range(0.5f, 10f)] float interval = 2.6f;
        [SerializeField, Range(0f, 1.5f)] float leadTime = 0.6f;
        [SerializeField, Range(0f, 0.3f)] float randomOffset = 0.08f;
        [SerializeField, Range(0.5f, 10f)] float lieTime = 3f;
        [SerializeField] Vector2 shadowSize = new Vector2(0.18f, 0.62f);
        [SerializeField] string firstMessage = PetitParisText.MessageBaguette;

        [Header("보너스 부스러기")]
        [SerializeField] CrumbPickup crumbTemplate;
        [SerializeField, Range(0, 20)] int maxBonusCrumbs = 6;

        [Header("소리 (3D)")]
        [SerializeField] AudioSource audioSource;

        enum ItemState { Idle, Warning, Falling, Lying }

        ItemState[] states;
        float[] timers;
        Vector3[] targets;
        float[] yaws;
        float[] speeds;
        float nextDrop;
        Vector3 lastRatPos;
        Vector3 ratVelocity;
        int bonusSpawned;
        bool messageShown;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null)
            {
                if (danger == null) danger = rig.GetComponent<RatDangerDetector>();
                if (respawn == null) respawn = rig.GetComponent<RatRespawn>();
            }
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (zone == null) zone = GetComponent<Collider>();
            if (zone != null) zone.isTrigger = true;
            PetitParisAudio.Configure3D(audioSource, 0.4f, 10f);

            int n = items != null ? items.Length : 0;
            states = new ItemState[n];
            timers = new float[n];
            targets = new Vector3[n];
            yaws = new float[n];
            speeds = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (items[i] != null) items[i].gameObject.SetActive(false);
                SetShadow(i, Vector3.zero, 0f, 0f);
            }
            if (crumbTemplate != null) crumbTemplate.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (rig != null && dt > 0f)
            {
                Vector3 p = rig.transform.position;
                Vector3 v = (p - lastRatPos) / dt;
                v.y = 0f;
                ratVelocity = Vector3.Lerp(ratVelocity, Vector3.ClampMagnitude(v, 1f), 1f - Mathf.Exp(-6f * dt));
                lastRatPos = p;
            }

            bool ratInZone = rig != null && danger != null && zone != null
                             && RatSafeZone.ColliderContains(zone, danger.BodyCenter)
                             && (respawn == null || (!respawn.IsRespawning && !respawn.IsInvulnerable))
                             && !danger.IsSafe
                             && (gameFlow == null || gameFlow.CurrentPhase != PetitParisGameFlow.Phase.Completed);
            if (ratInZone && Time.time >= nextDrop) TryDrop();

            for (int i = 0; i < states.Length; i++) Tick(i, dt);
        }

        void TryDrop()
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] != ItemState.Idle || items[i] == null) continue;
                Vector3 target = rig.transform.position + ratVelocity * (warningTime * leadTime + 0.1f);
                target += new Vector3(Random.Range(-randomOffset, randomOffset), 0f, Random.Range(-randomOffset, randomOffset));
                if (zone != null)
                {
                    // 구역 밖(벽 안 등)으로 나가지 않게
                    Bounds b = zone.bounds;
                    target.x = Mathf.Clamp(target.x, b.min.x, b.max.x);
                    target.z = Mathf.Clamp(target.z, b.min.z, b.max.z);
                }
                target.y = rig.transform.position.y;
                targets[i] = target;
                yaws[i] = Random.Range(0f, 360f);
                states[i] = ItemState.Warning;
                timers[i] = 0f;
                nextDrop = Time.time + interval;
                if (!messageShown && gameFlow != null)
                {
                    gameFlow.PostMessage(firstMessage);
                    messageShown = true;
                }
                return;
            }
        }

        void Tick(int i, float dt)
        {
            var item = items[i];
            if (item == null) return;
            timers[i] += dt;
            float fallTime = Mathf.Sqrt(2f * dropHeight / 9.81f);

            switch (states[i])
            {
                case ItemState.Warning:
                {
                    float k = Mathf.Clamp01(timers[i] / warningTime);
                    SetShadow(i, targets[i], Mathf.Lerp(0.4f, 1f, k), Mathf.Lerp(0.15f, 0.65f, k));
                    float fallStartsAt = warningTime - fallTime;
                    if (timers[i] >= Mathf.Max(0f, fallStartsAt))
                    {
                        states[i] = ItemState.Falling;
                        speeds[i] = 0f;
                        item.gameObject.SetActive(true);
                        item.SetPositionAndRotation(targets[i] + Vector3.up * dropHeight, Quaternion.Euler(0f, yaws[i], 70f));
                        SetHazard(item, true);
                        if (audioSource != null)
                        {
                            audioSource.transform.position = targets[i] + Vector3.up * 0.5f;
                            audioSource.PlayOneShot(PetitParisAudio.Whoosh, 0.7f);
                        }
                    }
                    break;
                }

                case ItemState.Falling:
                {
                    timers[i] = Mathf.Min(timers[i], warningTime); // 그림자 최대 유지
                    SetShadow(i, targets[i], 1f, 0.7f);
                    speeds[i] += 9.81f * dt;
                    Vector3 pos = item.position + Vector3.down * (speeds[i] * dt);
                    float restY = targets[i].y + 0.035f;
                    // 떨어지며 눕는다
                    float k = Mathf.Clamp01(1f - (pos.y - restY) / dropHeight);
                    Quaternion rot = Quaternion.Euler(0f, yaws[i], Mathf.Lerp(70f, 90f, k));
                    if (pos.y <= restY)
                    {
                        pos.y = restY;
                        states[i] = ItemState.Lying;
                        timers[i] = 0f;
                        if (audioSource != null)
                        {
                            audioSource.transform.position = targets[i];
                            audioSource.PlayOneShot(PetitParisAudio.Thud, 0.9f);
                        }
                        SpawnBonusCrumb(targets[i]);
                    }
                    item.SetPositionAndRotation(pos, rot);
                    break;
                }

                case ItemState.Lying:
                {
                    // 닿은 직후 0.12초만 위험 (그 뒤는 그냥 누워 있는 빵)
                    if (timers[i] > 0.12f) SetHazard(item, false);
                    SetShadow(i, targets[i], 1f, Mathf.Lerp(0.5f, 0f, timers[i] / 0.4f));
                    if (timers[i] > lieTime)
                    {
                        // 바닥으로 가라앉으며 사라짐
                        item.position += Vector3.down * (0.08f * dt);
                        if (timers[i] > lieTime + 1f)
                        {
                            item.gameObject.SetActive(false);
                            states[i] = ItemState.Idle;
                        }
                    }
                    break;
                }
            }
        }

        void SpawnBonusCrumb(Vector3 at)
        {
            if (crumbTemplate == null || bonusSpawned >= maxBonusCrumbs) return;
            bonusSpawned++;
            Vector2 off = Random.insideUnitCircle.normalized * 0.12f;
            var crumb = Instantiate(crumbTemplate, at + new Vector3(off.x, 0.02f, off.y), Quaternion.identity, transform);
            crumb.IsBonus = true;
            crumb.gameObject.SetActive(true);
        }

        static void SetHazard(Transform item, bool on)
        {
            var hazard = item.GetComponentInChildren<FootstepHazard>(true);
            if (hazard != null) hazard.enabled = on;
        }

        void SetShadow(int i, Vector3 at, float scale, float alpha)
        {
            if (shadows == null || i >= shadows.Length || shadows[i] == null) return;
            shadows[i].Set(at + Vector3.up * 0.007f, Quaternion.Euler(0f, yaws != null && i < yaws.Length ? yaws[i] + 90f : 0f, 0f), shadowSize * scale, alpha);
        }
    }
}
