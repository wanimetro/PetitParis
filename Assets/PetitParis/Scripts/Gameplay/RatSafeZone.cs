using System.Collections.Generic;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 안전 구역 (의자 아래, 벤치 아래, 벽 틈 등). 아무 오브젝트에나 붙여 재사용한다.
    ///
    /// 사용법: 빈 GameObject에 BoxCollider(또는 Sphere/Capsule)를 넣고 이 컴포넌트를 추가 →
    ///        Collider는 자동으로 Trigger가 된다. 크기는 Collider로 조절.
    /// - 쥐 몸 중심이 구역 안이면 RatDangerDetector가 "안전"으로 판정 → 위험 물체가 닿아도 실패하지 않음.
    /// - requireCrouch를 켜면 웅크렸을 때만 숨은 것으로 인정.
    /// - indicator(바닥 표시)는 쥐가 들어오면 색이 바뀐다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class RatSafeZone : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly List<RatSafeZone> active = new List<RatSafeZone>();

        public static IReadOnlyList<RatSafeZone> Active => active;

        [SerializeField] string zoneName = "의자 아래";

        [Tooltip("켜면 웅크린(B) 상태에서만 안전.")]
        [SerializeField] bool requireCrouch = false;

        [Tooltip("바닥 표시 (선택). PetitParis/BlobShadow 셰이더 머티리얼 권장.")]
        [SerializeField] Renderer indicator;
        [SerializeField] Color idleColor = new Color(0.55f, 0.95f, 0.6f, 0.18f);
        [SerializeField] Color occupiedColor = new Color(0.55f, 0.95f, 0.6f, 0.45f);

        Collider volume;
        MaterialPropertyBlock block;
        bool occupied;

        public string ZoneName => zoneName;
        public bool RequireCrouch => requireCrouch;
        public Collider Volume => volume != null ? volume : volume = GetComponent<Collider>();

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void Awake()
        {
            volume = GetComponent<Collider>();
            volume.isTrigger = true;
            block = new MaterialPropertyBlock();
            ApplyIndicator();
        }

        void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        void OnDisable()
        {
            active.Remove(this);
        }

        public bool Contains(Vector3 worldPoint) => ColliderContains(Volume, worldPoint);

        public bool Accepts(bool crouched) => !requireCrouch || crouched;

        /// <summary>RatDangerDetector가 쥐의 입장/퇴장을 알려준다.</summary>
        public void SetOccupied(bool value)
        {
            if (occupied == value) return;
            occupied = value;
            ApplyIndicator();
        }

        void ApplyIndicator()
        {
            if (indicator == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block);
            block.SetColor(ColorId, occupied ? occupiedColor : idleColor);
            indicator.SetPropertyBlock(block);
        }

        /// <summary>점이 Collider 부피 안에 있는지 (Trigger 여부와 무관, 물리 이벤트 불필요).</summary>
        public static bool ColliderContains(Collider collider, Vector3 worldPoint)
        {
            if (collider == null) return false;
            switch (collider)
            {
                case BoxCollider b:
                {
                    Vector3 local = b.transform.InverseTransformPoint(worldPoint) - b.center;
                    Vector3 half = b.size * 0.5f;
                    return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
                }
                case SphereCollider s:
                {
                    Vector3 local = s.transform.InverseTransformPoint(worldPoint) - s.center;
                    return local.sqrMagnitude <= s.radius * s.radius;
                }
                default:
                    return (collider.ClosestPoint(worldPoint) - worldPoint).sqrMagnitude < 1e-8f;
            }
        }

        void OnDrawGizmos()
        {
            var c = Volume as BoxCollider;
            if (c == null) return;
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(c.center, c.size);
        }
    }
}
