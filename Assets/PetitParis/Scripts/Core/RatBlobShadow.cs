using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 바닥에 눕힌 Quad로 그리는 부드러운 원형 그림자 (PetitParis/BlobShadow 셰이더).
    /// 실시간 그림자 대신 사용해 Quest 성능을 아끼고, 발이 내려올 위치를 플레이어에게 알려준다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public class RatBlobShadow : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly Quaternion LayFlat = Quaternion.Euler(90f, 0f, 0f);

        [SerializeField] Color color = new Color(0f, 0f, 0f, 0.5f);

        MeshRenderer meshRenderer;
        MaterialPropertyBlock block;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            block = new MaterialPropertyBlock();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        /// <summary>그림자 위치(바닥), 방향(Yaw), 크기(폭, 길이), 불투명도를 설정한다.</summary>
        public void Set(Vector3 groundPosition, Quaternion yaw, Vector2 size, float alpha)
        {
            if (meshRenderer == null) Awake();
            transform.SetPositionAndRotation(groundPosition, yaw * LayFlat);
            transform.localScale = new Vector3(size.x, size.y, 1f);

            Color c = color;
            c.a = Mathf.Clamp01(alpha);
            meshRenderer.GetPropertyBlock(block);
            block.SetColor(ColorId, c);
            meshRenderer.SetPropertyBlock(block);
            meshRenderer.enabled = c.a > 0.005f;
        }
    }
}
