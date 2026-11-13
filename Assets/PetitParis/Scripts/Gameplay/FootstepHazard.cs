using System.Collections.Generic;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 밟히면 실패하는 위험 물체 (신발, 자전거 바퀴 등). 아무 오브젝트에나 붙여 재사용할 수 있다.
    ///
    /// - BoxCollider(Trigger)로 판정 모양을 정한다. Trigger라서 쥐를 물리적으로 밀거나 날리지 않는다.
    /// - 실제 판정은 RatDangerDetector가 매 프레임 "쥐 몸 캡슐 ↔ 이 박스" 거리를 계산해서 한다
    ///   (움직이는 Trigger의 물리 이벤트 누락에 영향받지 않음).
    /// - warningRadius 안에 들어오면 위험도(0~1)가 올라간다 → 상태 표시·피드백.
    /// 수치는 모두 월드 미터.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class FootstepHazard : MonoBehaviour
    {
        static readonly List<FootstepHazard> active = new List<FootstepHazard>();

        /// <summary>현재 활성화된 모든 위험 물체.</summary>
        public static IReadOnlyList<FootstepHazard> Active => active;

        [SerializeField] string hazardName = "신발";

        [Tooltip("이 거리 안이면 위험 피드백 시작 (월드 m). 0.4 = 사람 기준 체감 4m.")]
        [SerializeField, Range(0.05f, 3f)] float warningRadius = 0.4f;

        [Tooltip("판정 여유. +면 더 엄격(더 멀리서 밟힘), -면 더 관대.")]
        [SerializeField, Range(-0.05f, 0.05f)] float hitPadding = 0f;

        [Tooltip("끄면 경고만 하고 실패시키지 않는다.")]
        [SerializeField] bool lethal = true;

        BoxCollider box;

        public string HazardName => hazardName;
        public float WarningRadius => warningRadius;
        public bool IsLethal => lethal && isActiveAndEnabled;

        void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        void OnDisable()
        {
            active.Remove(this);
        }

        /// <summary>박스 표면에서 점까지 가장 가까운 점 (월드).</summary>
        public Vector3 ClosestPoint(Vector3 worldPoint)
        {
            if (box == null) box = GetComponent<BoxCollider>();
            Vector3 local = transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 half = box.size * 0.5f;
            local.x = Mathf.Clamp(local.x, -half.x, half.x);
            local.y = Mathf.Clamp(local.y, -half.y, half.y);
            local.z = Mathf.Clamp(local.z, -half.z, half.z);
            return transform.TransformPoint(local + box.center);
        }

        /// <summary>
        /// 캡슐(a-b 선분, 반지름 r)과 박스 사이의 거리 (월드 m). 0 이하 = 닿음.
        /// 선분 위 몇 점을 샘플링하는 근사이며, 쥐 크기에서는 충분히 정확하다.
        /// </summary>
        public float DistanceToCapsule(Vector3 a, Vector3 b, float radius, out Vector3 closestOnHazard)
        {
            float best = float.MaxValue;
            closestOnHazard = transform.position;
            for (int i = 0; i <= 3; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / 3f);
                Vector3 c = ClosestPoint(p);
                float d = (p - c).magnitude;
                if (d < best)
                {
                    best = d;
                    closestOnHazard = c;
                }
            }
            return best - radius - hitPadding;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, warningRadius);
        }
    }
}
