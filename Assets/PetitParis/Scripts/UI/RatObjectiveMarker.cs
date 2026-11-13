using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 다음 목표 위치에 떠 있는 작은 표시 (과하지 않게: 작고 천천히 회전, 가까워지면 사라짐).
    /// 움직이는 것은 표시 자체이며 카메라와는 무관하다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatObjectiveMarker : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] RatScaleRig rig;

        [Tooltip("목표 위 높이 (월드 m). 0.2 = 쥐 눈높이보다 조금 위.")]
        [SerializeField, Range(0f, 1f)] float height = 0.2f;
        [SerializeField, Range(0f, 0.05f)] float bobAmplitude = 0.01f;
        [SerializeField, Range(0f, 180f)] float spinSpeed = 45f;

        [Tooltip("쥐가 이 거리(월드 m) 안이면 숨긴다.")]
        [SerializeField, Range(0f, 1f)] float hideDistance = 0.25f;

        Transform target;

        void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        void LateUpdate()
        {
            bool show = target != null;
            if (show && rig != null)
            {
                Vector3 d = target.position - rig.transform.position;
                d.y = 0f;
                show = d.magnitude > hideDistance;
            }
            if (visual != null && visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            if (!show) return;

            float bob = Mathf.Sin(Time.time * 2f) * bobAmplitude;
            transform.position = target.position + Vector3.up * (height + bob);
            if (visual != null) visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
