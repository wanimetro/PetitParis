using TMPro;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 길 위에 놓인 짧은 안내문 (월드 공간). 쥐가 가까이 오면 서서히 나타나고 멀어지면 사라진다.
    /// 머리에 붙는 HUD가 아니라 장소에 놓인 표지판이라 멀미·가림이 적다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatHintSign : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] RatScaleRig rig;

        [Tooltip("이 거리(월드 m) 안에서 완전히 보인다.")]
        [SerializeField, Range(0.05f, 3f)] float fullDistance = 0.6f;

        [Tooltip("이 거리(월드 m) 밖에서는 보이지 않는다.")]
        [SerializeField, Range(0.1f, 5f)] float fadeDistance = 1.2f;

        [Tooltip("쥐 쪽으로 Yaw만 돌려 항상 읽을 수 있게 한다.")]
        [SerializeField] bool faceRat = true;

        void Awake()
        {
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
        }

        void LateUpdate()
        {
            if (text == null || rig == null) return;
            Vector3 eye = rig.EyeCamera != null ? rig.EyeCamera.transform.position : rig.transform.position;
            Vector3 d = transform.position - eye;
            d.y = 0f;
            float distance = d.magnitude;
            float alpha = 1f - Mathf.InverseLerp(fullDistance, Mathf.Max(fullDistance + 0.01f, fadeDistance), distance);
            text.alpha = alpha;
            text.enabled = alpha > 0.01f;
            if (faceRat && distance > 1e-3f && text.enabled)
                transform.rotation = Quaternion.LookRotation(d / distance, Vector3.up);
        }
    }
}
