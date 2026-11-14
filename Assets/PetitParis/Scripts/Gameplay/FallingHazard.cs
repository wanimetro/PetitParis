using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 위에서 물건(바게트, 양파 등)이 떨어지는 이벤트.
    /// - 쥐가 zone(Trigger Collider) 안에 있으면 interval마다 하나씩, 한 번 들어올 때 dropsPerVisit개까지만 떨어뜨린다
    ///   (끝없이 쏟아지지 않게). 구역을 나갔다가 revisitCooldown초 뒤 다시 들어오면 다시 시작
    /// - 떨어질 자리는 쥐가 곧 있을 위치 (현재 위치 + 이동 방향 × 예측 시간)
    /// - dropFromEdge를 켜면 차양 앞 끝(edgeA~edgeB 선)에서만 굴러떨어진다: 쥐가 차양 끝 가까이 올 때만,
    ///   차양 끝 바로 앞 바닥에 떨어진다 → 차양 밑은 안전하고, 차양 끝 앞 줄을 건널 때가 위험
    /// - 떨어지기 전에 바닥 그림자가 점점 커지고 진해진다 (warningTime초) → 질주로 피한다
    /// - 맞았는지는 떨어지는 매 프레임 직접 검사한다 (빠르게 떨어져도 놓치지 않음) → 맞으면 "바게트에 맞았어요!" + 리스폰
    ///   간발의 차로 피하면 "아슬아슬!" 메시지. 바닥에 닿으면 한 번 튀었다가 눕고, 누운 바게트는 위험하지 않다
    /// - 쥐가 죽으면 떨어지던 바게트와 그림자를 모두 치운다 (리스폰한 쥐 위로 떨어지지 않게)
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
        [SerializeField, Range(0.3f, 3f)] float warningTime = 1.5f;
        [SerializeField, Range(0.5f, 10f)] float interval = 3.5f;
        [SerializeField, Range(0f, 1.5f)] float leadTime = 0.3f;
        [SerializeField, Range(0f, 0.3f)] float randomOffset = 0.15f;
        [SerializeField, Range(0.5f, 10f)] float lieTime = 3f;
        [SerializeField] Vector2 shadowSize = new Vector2(0.18f, 0.62f);
        [SerializeField] string firstMessage = PetitParisText.MessageBaguette;
        [Tooltip("구역에 한 번 들어올 때 떨어지는 최대 개수.")]
        [SerializeField, Range(1, 20)] int dropsPerVisit = 3;
        [Tooltip("구역을 나간 뒤 이 시간(초)이 지나야 다시 떨어지기 시작한다.")]
        [SerializeField, Range(0f, 60f)] float revisitCooldown = 8f;
        [Tooltip("맞음 판정 여유 (월드 m). +면 더 엄격.")]
        [SerializeField, Range(-0.02f, 0.03f)] float hitPadding = 0.005f;
        [Tooltip("이 거리(월드 m) 안에 떨어졌는데 안 맞으면 '아슬아슬' 메시지.")]
        [SerializeField, Range(0f, 0.5f)] float nearMissDistance = 0.16f;

        [Header("차양 끝에서 굴러떨어짐 (선택)")]
        [Tooltip("켜면 하늘이 아니라 차양 앞 끝(edgeA~edgeB, 월드 좌표·끝 높이)에서 떨어진다.")]
        [SerializeField] bool dropFromEdge;
        [SerializeField] Vector3 edgeA;
        [SerializeField] Vector3 edgeB;
        [Tooltip("차양 끝에서 바깥쪽(거리 쪽) 방향. 바게트는 끝보다 조금 바깥에 떨어진다.")]
        [SerializeField] Vector3 edgeOutward = Vector3.right;
        [SerializeField] Vector2 edgeLandingOffset = new Vector2(0.04f, 0.14f);
        [Tooltip("쥐가 차양 끝 선에서 이 거리(월드 m) 안에 있을 때만 떨어뜨린다.")]
        [SerializeField, Range(0.1f, 2f)] float edgeTriggerDistance = 0.8f;

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
        float[] heights;
        float nextDrop;
        Vector3 lastRatPos;
        Vector3 ratVelocity;
        int bonusSpawned;
        bool messageShown;
        int dropsThisVisit;
        float outsideSince = -999f;
        bool wasInZone;
        bool doneMessageShown;

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
            // 소리 위치를 낙하 지점으로 옮기는데, AudioSource가 이 오브젝트(구역·바게트의 부모)에 붙어 있으면
            // 구역째 움직여 쥐가 맞은 뒤 구역이 공중에 떠 다시는 떨어지지 않는다 → 전용 자식으로 옮긴다
            if (audioSource != null && (audioSource.transform == transform || (zone != null && audioSource.transform == zone.transform)))
            {
                var old = audioSource;
                var child = new GameObject("DropAudio").AddComponent<AudioSource>();
                child.transform.SetParent(transform, false);
                child.outputAudioMixerGroup = old.outputAudioMixerGroup;
                child.volume = old.volume;
                Destroy(old);
                audioSource = child;
            }
            PetitParisAudio.Configure3D(audioSource, 0.4f, 10f);

            int n = items != null ? items.Length : 0;
            states = new ItemState[n];
            timers = new float[n];
            targets = new Vector3[n];
            yaws = new float[n];
            speeds = new float[n];
            heights = new float[n];
            for (int i = 0; i < n; i++)
            {
                if (items[i] != null) items[i].gameObject.SetActive(false);
                SetShadow(i, Vector3.zero, 0f, 0f);
            }
            if (crumbTemplate != null) crumbTemplate.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (respawn != null) respawn.Died += OnRatDied;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.Died -= OnRatDied;
        }

        /// <summary>쥐가 죽으면 떨어지던 것을 모두 치우고, 다시 들어오면 처음부터.</summary>
        void OnRatDied(string reason)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] == ItemState.Lying) continue; // 이미 누운 빵은 그대로
                states[i] = ItemState.Idle;
                if (items[i] != null) items[i].gameObject.SetActive(false);
                SetShadow(i, Vector3.zero, 0f, 0f);
            }
            dropsThisVisit = 0;
            doneMessageShown = false;
            nextDrop = Time.time + interval;
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
                             && (!dropFromEdge || DistanceToEdge(rig.transform.position) <= edgeTriggerDistance)
                             && (gameFlow == null || gameFlow.CurrentPhase != PetitParisGameFlow.Phase.Completed);
            // 한 번 들어올 때 dropsPerVisit개까지만. 나갔다가 revisitCooldown 뒤 다시 들어오면 다시 시작
            if (!ratInZone && wasInZone) outsideSince = Time.time;
            if (ratInZone && !wasInZone && Time.time - outsideSince >= revisitCooldown)
            {
                dropsThisVisit = 0;
                doneMessageShown = false;
            }
            wasInZone = ratInZone;
            if (ratInZone && Time.time >= nextDrop && dropsThisVisit < dropsPerVisit) TryDrop();
            if (dropsThisVisit >= dropsPerVisit && !doneMessageShown && AllIdleOrLying() && gameFlow != null)
            {
                gameFlow.PostMessage(PetitParisText.MessageBaguetteDone);
                doneMessageShown = true;
            }

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
                heights[i] = dropHeight;
                yaws[i] = Random.Range(0f, 360f);
                if (dropFromEdge)
                {
                    // 차양 끝 선 위에서 쥐와 가장 가까운 지점 바로 바깥에 떨어진다. 바게트는 차양 끝과 나란히 눕는다
                    Vector3 onEdge = ClosestOnEdge(target);
                    Vector3 outward = new Vector3(edgeOutward.x, 0f, edgeOutward.z).normalized;
                    Vector3 landing = onEdge + outward * Random.Range(edgeLandingOffset.x, edgeLandingOffset.y);
                    heights[i] = Mathf.Max(0.3f, onEdge.y - target.y);
                    target = new Vector3(landing.x, target.y, landing.z);
                    Vector3 along = edgeB - edgeA;
                    yaws[i] = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg - 90f + Random.Range(-12f, 12f);
                }
                targets[i] = target;
                states[i] = ItemState.Warning;
                timers[i] = 0f;
                nextDrop = Time.time + interval;
                dropsThisVisit++;
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
            float fallTime = Mathf.Sqrt(2f * heights[i] / 9.81f);

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
                        item.SetPositionAndRotation(targets[i] + Vector3.up * heights[i], Quaternion.Euler(0f, yaws[i], 70f));
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
                    float k = Mathf.Clamp01(1f - (pos.y - restY) / heights[i]);
                    Quaternion rot = Quaternion.Euler(0f, yaws[i], Mathf.Lerp(70f, 90f, k));
                    bool landed = pos.y <= restY;
                    if (landed) pos.y = restY;
                    item.SetPositionAndRotation(pos, rot);

                    // 맞았는지 매 프레임 직접 검사 (빠르게 떨어져 한 프레임에 쥐를 지나쳐도 놓치지 않게)
                    float d = DistanceToRat(item);
                    if (d <= 0f && CanKillRat())
                    {
                        respawn.Kill("바게트");
                        if (gameFlow != null) gameFlow.PostMessage(PetitParisText.MessageBaguetteHit); // 기본 "밟힐 뻔했어요" 대신
                        break;
                    }

                    if (landed)
                    {
                        states[i] = ItemState.Lying;
                        timers[i] = 0f;
                        SetHazard(item, false); // 누운 바게트는 위험하지 않다 (판정은 위에서 직접 함)
                        if (audioSource != null)
                        {
                            audioSource.transform.position = targets[i];
                            audioSource.PlayOneShot(PetitParisAudio.Thud, 0.9f);
                        }
                        if (d < nearMissDistance && gameFlow != null && (respawn == null || !respawn.IsRespawning))
                            gameFlow.PostMessage(PetitParisText.MessageBaguetteNearMiss);
                        SpawnBonusCrumb(targets[i]);
                        SpawnBonusCrumb(targets[i]);
                    }
                    break;
                }

                case ItemState.Lying:
                {
                    // 바닥에 닿으면 한 번 통 튀었다가 눕는다 (맞았다는 느낌)
                    const float bounceTime = 0.3f;
                    if (timers[i] <= bounceTime + dt)
                    {
                        float bt = Mathf.Clamp01(timers[i] / bounceTime);
                        Vector3 lp = item.position;
                        lp.y = targets[i].y + 0.035f + Mathf.Sin(bt * Mathf.PI) * 0.04f;
                        item.position = lp;
                    }
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

        Vector3 ClosestOnEdge(Vector3 p)
        {
            Vector3 a = edgeA, b = edgeB;
            Vector3 ab = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float len2 = ab.sqrMagnitude;
            float t = len2 > 1e-6f ? Mathf.Clamp01(Vector3.Dot(new Vector3(p.x - a.x, 0f, p.z - a.z), ab) / len2) : 0f;
            return Vector3.Lerp(a, b, t);
        }

        float DistanceToEdge(Vector3 p)
        {
            Vector3 c = ClosestOnEdge(p);
            return new Vector2(p.x - c.x, p.z - c.z).magnitude;
        }

        bool AllIdleOrLying()
        {
            foreach (var st in states)
                if (st == ItemState.Warning || st == ItemState.Falling) return false;
            return true;
        }

        bool CanKillRat() =>
            respawn != null && !respawn.IsRespawning && !respawn.IsInvulnerable && (danger == null || !danger.IsSafe);

        /// <summary>바게트(FootstepHazard 박스)와 쥐 몸 캡슐 사이 거리 (월드 m). 0 이하 = 맞음.</summary>
        float DistanceToRat(Transform item)
        {
            var hazard = item.GetComponentInChildren<FootstepHazard>(true);
            var body = rig != null ? rig.Body : null;
            if (hazard == null || body == null) return float.MaxValue;
            Vector3 up = body.transform.up;
            Vector3 center = body.transform.TransformPoint(body.center);
            float half = Mathf.Max(0f, body.height * 0.5f - body.radius);
            return hazard.DistanceToCapsule(center + up * half, center - up * half, body.radius, out _) - hitPadding;
        }

        void OnDrawGizmosSelected()
        {
            if (!dropFromEdge) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawLine(edgeA, edgeB);
            Gizmos.DrawLine(new Vector3(edgeA.x, 0f, edgeA.z), new Vector3(edgeB.x, 0f, edgeB.z));
        }

        void SpawnBonusCrumb(Vector3 at)
        {
            if (crumbTemplate == null || bonusSpawned >= maxBonusCrumbs) return;
            bonusSpawned++;
            Vector2 off = Random.insideUnitCircle.normalized * Random.Range(0.08f, 0.16f);
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
