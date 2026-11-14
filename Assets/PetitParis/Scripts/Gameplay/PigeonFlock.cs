using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 바닥에서 모이를 쪼는 비둘기 무리. 쥐가 가까이 오면 한꺼번에 날아오른다 (위험은 없음, 분위기용).
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
                for (int i = 0; i < birds.Length; i++)
                {
                    var b = birds[i];
                    if (b == null || b.root == null) continue;
                    Vector3 d = b.root.position - rat;
                    d.y = 0f;
                    if (d.sqrMagnitude < scareRadius * scareRadius) { TakeOff(rat); break; }
                    Peck(i, dt);
                }
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
            if (hopTimer[i] <= 0f)
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
