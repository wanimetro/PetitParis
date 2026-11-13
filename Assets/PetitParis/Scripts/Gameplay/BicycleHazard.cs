using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 골목을 가로지르는 자전거. 두 Waypoint 사이를 왕복하고, 양 끝(벽 뒤)에서 잠시 멈춘다.
    /// - 출발할 때 벨 소리, 달리는 동안 바퀴 소리 (3D 공간 음향) → 플레이어가 방향과 타이밍을 예측
    /// - 바닥 그림자로 지나가는 위치 표시
    /// - 판정은 자식의 FootstepHazard (Trigger 박스)
    /// 속도는 월드 m/s.
    /// </summary>
    [DisallowMultipleComponent]
    public class BicycleHazard : MonoBehaviour
    {
        [Header("경로 (왕복)")]
        [SerializeField] Transform pointA;
        [SerializeField] Transform pointB;

        [Tooltip("최고 속도 (월드 m/s).")]
        [SerializeField, Range(0.2f, 6f)] float speed = 2.2f;

        [SerializeField, Range(0.1f, 10f)] float acceleration = 3f;

        [Tooltip("양 끝에서 멈추는 시간(초). 이 동안 골목을 건널 수 있다.")]
        [SerializeField, Range(0f, 10f)] float pauseAtEnds = 2.5f;

        [SerializeField, Range(0f, 10f)] float startDelay = 1f;

        [Header("모양")]
        [SerializeField] Transform bike;
        [SerializeField] Transform[] wheels = new Transform[0];
        [SerializeField, Range(0.1f, 0.5f)] float wheelRadius = 0.34f;
        [SerializeField] RatBlobShadow shadow;

        [Header("소리 (3D)")]
        [SerializeField] AudioSource rollingAudio;
        [SerializeField] AudioSource bellAudio;
        [SerializeField, Range(0f, 1f)] float rollingVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] float bellVolume = 0.8f;
        [SerializeField, Range(0.05f, 2f)] float audioMinDistance = 0.4f;
        [SerializeField, Range(1f, 30f)] float audioMaxDistance = 12f;

        bool towardB = true;
        float pauseTimer;
        float currentSpeed;
        float t; // 0 = A, 1 = B
        bool valid;

        public bool IsMoving => currentSpeed > 0.05f;

        void Start()
        {
            valid = pointA != null && pointB != null && bike != null;
            if (!valid)
            {
                Debug.LogError($"[PetitParis] {name}: pointA, pointB, bike 참조가 필요합니다.", this);
                enabled = false;
                return;
            }

            PetitParisAudio.Configure3D(rollingAudio, audioMinDistance, audioMaxDistance);
            PetitParisAudio.Configure3D(bellAudio, audioMinDistance, audioMaxDistance);
            if (rollingAudio != null)
            {
                rollingAudio.clip = PetitParisAudio.BikeRoll;
                rollingAudio.loop = true;
                rollingAudio.volume = 0f;
                rollingAudio.Play();
            }
            pauseTimer = startDelay;
            Place();
        }

        void Update()
        {
            if (!valid) return;
            float dt = Time.deltaTime;
            float length = Mathf.Max(0.01f, Vector3.Distance(pointA.position, pointB.position));

            if (pauseTimer > 0f)
            {
                pauseTimer -= dt;
                currentSpeed = 0f;
                if (pauseTimer <= 0f && bellAudio != null) bellAudio.PlayOneShot(PetitParisAudio.BikeBell, bellVolume);
            }
            else
            {
                float remaining = (towardB ? 1f - t : t) * length;
                float brakeDistance = currentSpeed * currentSpeed / (2f * acceleration);
                float targetSpeed = remaining <= brakeDistance ? 0.15f : speed;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * dt);
                t += (towardB ? 1f : -1f) * currentSpeed * dt / length;
                if (t >= 1f || t <= 0f)
                {
                    t = Mathf.Clamp01(t);
                    towardB = !towardB;
                    pauseTimer = pauseAtEnds;
                }
            }

            Place();

            float spin = currentSpeed / wheelRadius * Mathf.Rad2Deg * dt;
            foreach (var w in wheels)
                if (w != null) w.Rotate(Vector3.up, spin, Space.Self); // 자전거 기준 항상 앞으로 굴러감

            if (rollingAudio != null) rollingAudio.volume = rollingVolume * Mathf.Clamp01(currentSpeed / speed);
        }

        void Place()
        {
            Vector3 a = pointA.position, b = pointB.position;
            Vector3 p = Vector3.Lerp(a, b, t);
            Vector3 dir = (towardB ? b - a : a - b);
            dir.y = 0f;
            Quaternion yaw = dir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(dir.normalized, Vector3.up) : bike.rotation;
            bike.SetPositionAndRotation(p, yaw);
            if (shadow != null)
                shadow.Set(new Vector3(p.x, p.y + 0.006f, p.z), yaw, new Vector2(0.35f, 1.8f), 0.45f);
        }
    }
}
