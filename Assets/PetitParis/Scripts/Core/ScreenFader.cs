using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace PetitParis
{
    /// <summary>
    /// 카메라 바로 앞의 사각형(Quad)으로 화면 전체를 검게 덮는 페이드.
    /// 캘리브레이션·리스폰처럼 시점이 순간 이동할 때 사용한다.
    /// 셰이더(PetitParis/ScreenFade)는 ZTest Always라 앞에 무엇이 있든 화면을 덮는다.
    /// 카메라 Transform은 건드리지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public class ScreenFader : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] Color fadeColor = Color.black;

        [Tooltip("시작할 때 검은 화면으로 시작한다. 첫 캘리브레이션 전의 사람 높이 시점을 보여주지 않기 위함.")]
        [SerializeField] bool startOpaque = true;

        [Tooltip("Quad를 near clip의 몇 배 거리에 둘지.")]
        [SerializeField, Range(1.5f, 20f)] float nearClipMultiplier = 4f;

        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;
        float alpha;
        int fadeVersion;

        public float Alpha => alpha;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            block = new MaterialPropertyBlock();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            if (meshRenderer.sharedMaterial == null)
                Debug.LogWarning("[PetitParis] ScreenFader: 머티리얼이 비어 있습니다. PP_ScreenFade 머티리얼을 연결하세요.", this);

            PlaceInFrontOfCamera();
            ApplyAlpha(startOpaque ? 1f : 0f);
        }

        void Start()
        {
            // RatScaleRig가 스케일을 적용한 뒤 위치를 다시 맞춘다.
            PlaceInFrontOfCamera();
        }

        /// <summary>
        /// 부모 카메라의 스케일을 고려해 near clip 바깥, 시야 전체를 덮는 크기로 Quad를 배치한다.
        /// (자기 자신의 로컬 Transform만 바꾼다)
        /// </summary>
        public void PlaceInFrontOfCamera()
        {
            var cam = GetComponentInParent<Camera>();
            if (cam == null || transform.parent == null)
            {
                Debug.LogWarning("[PetitParis] ScreenFader는 카메라의 자식이어야 합니다.", this);
                return;
            }

            float worldDistance = Mathf.Max(cam.nearClipPlane * nearClipMultiplier, 0.01f);
            float parentScale = Mathf.Max(1e-5f, transform.parent.lossyScale.z);
            float localZ = worldDistance / parentScale;

            transform.localPosition = new Vector3(0f, 0f, localZ);
            transform.localRotation = Quaternion.identity;
            // 거리의 8배 크기 → 좌우 ±4배, 시야각 약 150도까지 덮는다.
            transform.localScale = Vector3.one * (localZ * 8f);
        }

        /// <summary>진행 중인 페이드를 취소하고 즉시 알파를 설정한다.</summary>
        public void SetAlpha(float value)
        {
            fadeVersion++;
            ApplyAlpha(value);
        }

        /// <summary>호출한 쪽의 코루틴 안에서 yield return으로 기다릴 수 있는 페이드.</summary>
        public IEnumerator Fade(float target, float duration)
        {
            int version = ++fadeVersion;
            float start = alpha;
            if (duration <= 0f)
            {
                ApplyAlpha(target);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                if (version != fadeVersion) yield break; // 더 새로운 페이드가 시작됨
                t += Time.unscaledDeltaTime;
                ApplyAlpha(Mathf.Lerp(start, target, t / duration));
                yield return null;
            }

            if (version == fadeVersion) ApplyAlpha(target);
        }

        /// <summary>기다릴 필요 없이 페이드만 시작한다.</summary>
        public void FadeTo(float target, float duration)
        {
            if (isActiveAndEnabled) StartCoroutine(Fade(target, duration));
            else ApplyAlpha(target);
        }

        void ApplyAlpha(float value)
        {
            alpha = Mathf.Clamp01(value);
            if (meshRenderer == null) return;

            Color c = fadeColor;
            c.a = alpha;
            meshRenderer.GetPropertyBlock(block);
            block.SetColor(ColorId, c);
            meshRenderer.SetPropertyBlock(block);
            // 완전히 투명하면 렌더링 자체를 끈다 (Quest 성능)
            meshRenderer.enabled = alpha > 0.001f;
        }
    }
}
