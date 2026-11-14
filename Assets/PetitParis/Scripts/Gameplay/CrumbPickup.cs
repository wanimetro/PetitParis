using System.Collections.Generic;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 주울 수 있는 빵 부스러기·크루아상·치즈 조각.
    /// - 판정은 CrumbCollection이 쥐 몸 중심과의 거리로 한다 (물리 Trigger 없음)
    /// - 제자리에서 천천히 돌고 살짝 떠오른다 → 멀리서도 눈에 띈다
    /// - 주우면 쥐 쪽으로 빨려 들어가며 작아지고 사라진다
    /// - Restore()로 되살린다 (체크포인트 전에 죽으면 그동안 모은 것이 원래 자리로 돌아감)
    /// 거리 단위는 월드 미터.
    /// </summary>
    [DisallowMultipleComponent]
    public class CrumbPickup : MonoBehaviour
    {
        public enum Kind { Crumb, Croissant, Cheese, GoldenCroissant }

        static readonly List<CrumbPickup> active = new List<CrumbPickup>();
        public static IReadOnlyList<CrumbPickup> Active => active;

        [SerializeField] Kind kind = Kind.Crumb;
        [SerializeField, Range(1, 20)] int points = 1;

        [Tooltip("쥐 몸 중심이 이 거리(월드 m) 안에 들어오면 줍는다.")]
        [SerializeField, Range(0.02f, 0.3f)] float pickupRadius = 0.075f;

        [Tooltip("돌고 떠오르는 모양 오브젝트 (자식).")]
        [SerializeField] Transform visual;
        [SerializeField, Range(0f, 360f)] float spinSpeed = 70f;
        [SerializeField, Range(0f, 0.03f)] float bobHeight = 0.006f;
        [SerializeField, Range(0.05f, 1f)] float collectDuration = 0.22f;

        enum State { Idle, Collecting, Collected }

        State state = State.Idle;
        Vector3 visualHome;
        Vector3 visualScale = Vector3.one;
        float collectTimer;
        Transform collectTarget;
        float phase;

        public Kind PickupKind => kind;
        public int Points => points;
        public float PickupRadius => pickupRadius;
        public bool IsGolden => kind == Kind.GoldenCroissant;
        public bool IsAvailable => state == State.Idle && isActiveAndEnabled;
        /// <summary>게임 중에 생긴 조각 (떨어진 바게트에서 나온 부스러기 등). 별점 기준 총점에는 넣지 않는다.</summary>
        public bool IsBonus { get; set; }

        void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null)
            {
                visualHome = visual.localPosition;
                visualScale = visual.localScale;
            }
            phase = (transform.position.x * 3.1f + transform.position.z * 1.7f) % 6.28f;
        }

        void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        void OnDisable()
        {
            active.Remove(this);
        }

        void Update()
        {
            if (visual == null) return;
            switch (state)
            {
                case State.Idle:
                    visual.localPosition = visualHome + Vector3.up * (Mathf.Sin(Time.time * 2.2f + phase) * 0.5f + 0.5f) * bobHeight;
                    visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
                    break;

                case State.Collecting:
                    collectTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(collectTimer / collectDuration);
                    if (collectTarget != null)
                        visual.position = Vector3.Lerp(visual.position, collectTarget.position, 1f - Mathf.Exp(-18f * Time.deltaTime));
                    visual.localScale = visualScale * (1f - t * t);
                    if (t >= 1f)
                    {
                        state = State.Collected;
                        visual.gameObject.SetActive(false);
                    }
                    break;
            }
        }

        /// <summary>줍기 시작 (toward: 빨려 들어갈 목표, 보통 쥐 몸).</summary>
        public void Collect(Transform toward)
        {
            if (state != State.Idle) return;
            state = State.Collecting;
            collectTimer = 0f;
            collectTarget = toward;
        }

        /// <summary>다시 주울 수 있게 원래 자리에 되살린다.</summary>
        public void Restore()
        {
            state = State.Idle;
            if (visual == null) return;
            visual.gameObject.SetActive(true);
            visual.localPosition = visualHome;
            visual.localScale = visualScale;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGolden ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.7f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
