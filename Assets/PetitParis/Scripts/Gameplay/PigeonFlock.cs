using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 바닥에서 모이를 쪼는 비둘기 무리.
    /// - 쥐가 followRadius 안에 오면 종종걸음으로 쥐를 따라온다. 쥐에게 닿으면 체크포인트 전에 모은 빵 부스러기를 하나 쪼아 간다
    /// - 쥐가 숨으면(안전 구역) 관심을 잃고, 둥지에서 leash보다 멀어지면 돌아간다
    /// - 쥐가 바로 옆에서 질주하면 한꺼번에 날아오른다
    /// - 평소: 고개를 까딱이며 쪼고, 가끔 몇 걸음 종종 움직인다
    /// - 쥐가 scareRadius 안에 들어오면: 날갯짓 소리와 함께 쥐 반대쪽 위로 날아간다
    /// - 쥐가 멀리 떨어지고 returnDelay초가 지나면 원래 자리로 돌아와 있다
    /// 비둘기 하나 = birds[i] (자식으로 head, wingL, wingR). 거리 단위는 월드 미터.
    /// </summary>
    [DisallowMultipleComponent]
    public class PigeonFlock : MonoBehaviour
    {
        [System.Serializable]
        public class Bird
        {
            public Transform root;
            public Transform head;
            public Transform wingL;
            public Transform wingR;
        }

        [SerializeField] RatScaleRig rig;
        [SerializeField] Bird[] birds = new Bird[0];
        [SerializeField, Range(0.1f, 2f)] float scareRadius = 0.55f;
        [SerializeField, Range(0.5f, 6f)] float flySpeed = 2.2f;
        [SerializeField, Range(2f, 60f)] float returnDelay = 18f;
        [SerializeField] AudioSource flutterAudio;
        [SerializeField, Range(0.3f, 4f)] float followRadius = 1.6f;
        [SerializeField, Range(0.05f, 1f)] float followSpeed = 0.3f;
        [SerializeField, Range(1f, 8f)] float leash = 4f;
        [SerializeField, Range(0.5f, 10f)] float peckCooldown = 2.5f;

        RatDangerDetector danger;
        RatDashController dash;
        CrumbCollection crumbs;
        PetitParisGameFlow flow;
        bool following;
        float nextPeck;

        Vector3[] homePos;
        Quaternion[] homeRot;
        Vector3[] headHome;
        Vector3[] flyDir;
        float[] seeds;
        float[] hopTimer;
        bool flying;
        float flyTimer;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (rig != null) { danger = rig.GetComponent<RatDangerDetector>(); dash = rig.GetComponentInChildren<RatDashController>(true); }
            crumbs = FindAnyObjectByType<CrumbCollection>();
            flow = FindAnyObjectByType<PetitParisGameFlow>();
            PetitParisAudio.Configure3D(flutterAudio, 0.4f, 10f);
            int n = birds.Length;
            homePos = new Vector3[n];
            homeRot = new Quaternion[n];
            headHome = new Vector3[n];
            flyDir = new Vector3[n];
            seeds = new float[n];
            hopTimer = new float[n];
            for (int i = 0; i < n; i++)
            {
                var b = birds[i];
                if (b == null || b.root == null) continue;
                homePos[i] = b.root.position;
                homeRot[i] = b.root.rotation;
                if (b.head != null) headHome[i] = b.head.localPosition;
                seeds[i] = i * 1.37f + 0.3f;
                hopTimer[i] = 1f + i * 0.7f;
            }
        }

        void Update()
        {
            if (rig == null) return;
            float dt = Time.deltaTime;
            Vector3 rat = rig.transform.position;

            if (!flying)
            {
                bool hidden = danger != null && danger.IsSafe;
                bool any = false;
                for (int i = 0; i < birds.Length; i++)
                {
                    var b = birds[i];
                    if (b == null || b.root == null) continue;
                    Vector3 d = rat - b.root.position;
                    d.y = 0f;
                    // 바로 옆에서 질주하면 날아오른다
                    if (dash != null && dash.IsDashing && d.sqrMagnitude < scareRadius * scareRadius) { TakeOff(rat); return; }
                    Vector3 fromHome = rat - homePos[i];
                    fromHome.y = 0f;
                    bool chase = !hidden && d.magnitude < followRadius && fromHome.magnitude < leash;
                    if (chase)
                    {
                        any = true;
                        // 쥐 주변 자기 자리(새마다 다른 각도)로 종종걸음
                        float a = seeds[i] * 2.4f;
                        Vector3 goal = rat + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.14f;
                        Vector3 step = goal - b.root.position;
                        step.y = 0f;
                        if (step.sqrMagnitude > 0.0004f)
                        {
                            b.root.position += Vector3.ClampMagnitude(step, followSpeed * dt);
                            b.root.rotation = Quaternion.Slerp(b.root.rotation, Quaternion.LookRotation(d.sqrMagnitude > 1e-6f ? d.normalized : b.root.forward, Vector3.up), 1f - Mathf.Exp(-6f * dt));
                        }
                        if (d.magnitude < 0.12f && Time.time >= nextPeck) Peck();
                        Peck(i, dt);
                    }
                    else
                    {
                        // 둥지로 돌아가 모이 쪼기
                        Vector3 back = homePos[i] - b.root.position;
                        back.y = 0f;
                        if (back.sqrMagnitude > 0.01f) b.root.position += Vector3.ClampMagnitude(back, followSpeed * 0.6f * dt);
                        Peck(i, dt);
                    }
                }
                if (any && !following && flow != null) flow.PostMessage(PetitParisText.MessagePigeons);
                following = any;
            }
            else
            {
                flyTimer += dt;
                for (int i = 0; i < birds.Length; i++)
                {
                    var b = birds[i];
                    if (b == null || b.root == null || !b.root.gameObject.activeSelf) continue;
                    b.root.position += flyDir[i] * (flySpeed * dt);
                    b.root.rotation = Quaternion.Slerp(b.root.rotation, Quaternion.LookRotation(flyDir[i], Vector3.up), 1f - Mathf.Exp(-8f * dt));
                    float flap = Mathf.Sin(Time.time * 45f + seeds[i]) * 55f;
                    if (b.wingL != null) b.wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                    if (b.wingR != null) b.wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
                    if (flyTimer > 3.5f) b.root.gameObject.SetActive(false);
                }

                // 쥐가 멀리 있으면 몰래 원래 자리로
                if (flyTimer > returnDelay && FarFromRat(rat)) Land();
            }
        }

        /// <summary>쥐를 쪼아 체크포인트 전에 모은 부스러기 하나를 가져간다.</summary>
        void Peck()
        {
            nextPeck = Time.time + peckCooldown;
            if (flutterAudio != null) flutterAudio.PlayOneShot(PetitParisAudio.Pickup, 0.5f);
            if (crumbs != null && crumbs.DropOne() && flow != null) flow.PostMessage(PetitParisText.MessagePigeonPeck, 1);
        }

        void Peck(int i, float dt)
        {
            var b = birds[i];
            float t = Time.time * 2.3f + seeds[i];
            if (b.head != null)
            {
                float peck = Mathf.Max(0f, Mathf.Sin(t * 2f)) * Mathf.Max(0f, Mathf.Sin(t * 0.5f));
                b.head.localPosition = headHome[i] + new Vector3(0f, -0.035f, 0.03f) * peck;
            }
            hopTimer[i] -= dt;
            if (!following && hopTimer[i] <= 0f)
            {
                hopTimer[i] = 1.5f + Mathf.Repeat(seeds[i] * 7.1f, 2.5f);
                float yaw = Mathf.Repeat(seeds[i] * 97f + Time.time * 40f, 360f);
                b.root.rotation = Quaternion.Euler(0f, yaw, 0f);
                // 원래 자리 근처(반경 15cm)에서만 종종
                Vector3 step = b.root.forward * 0.05f;
                Vector3 next = b.root.position + step;
                if ((next - homePos[i]).sqrMagnitude < 0.15f * 0.15f) b.root.position = next;
            }
        }

        void TakeOff(Vector3 rat)
        {
            flying = true;
            flyTimer = 0f;
            for (int i = 0; i < birds.Length; i++)
            {
                var b = birds[i];
                if (b == null || b.root == null) continue;
                Vector3 away = b.root.position - rat;
                away.y = 0f;
                if (away.sqrMagnitude < 1e-4f) away = Vector3.forward;
                float spread = (Mathf.Repeat(seeds[i] * 13f, 1f) - 0.5f) * 70f;
                Vector3 dir = Quaternion.Euler(0f, spread, 0f) * away.normalized;
                flyDir[i] = (dir + Vector3.up * (1.1f + Mathf.Repeat(seeds[i] * 3f, 0.6f))).normalized;
            }
            if (flutterAudio != null) flutterAudio.PlayOneShot(PetitParisAudio.Flutter, 1f);
        }

        bool FarFromRat(Vector3 rat)
        {
            for (int i = 0; i < homePos.Length; i++)
            {
                Vector3 d = homePos[i] - rat;
                d.y = 0f;
                if (d.sqrMagnitude < 2.5f * 2.5f) return false;
            }
            return true;
        }

        void Land()
        {
            flying = false;
            for (int i = 0; i < birds.Length; i++)
            {
                var b = birds[i];
                if (b == null || b.root == null) continue;
                b.root.gameObject.SetActive(true);
                b.root.SetPositionAndRotation(homePos[i], homeRot[i]);
                if (b.wingL != null) b.wingL.localRotation = Quaternion.identity;
                if (b.wingR != null) b.wingR.localRotation = Quaternion.identity;
            }
        }
    }
}
