// PETIT PARIS 바닥 원형 그림자 (URP, Quest 멀티뷰/싱글패스 인스턴싱 대응)
// UV 중심에서 멀어질수록 투명해지는 부드러운 원. 실시간 그림자보다 훨씬 저렴하다.
Shader "PetitParis/BlobShadow"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 0.5)
        _Softness ("Softness", Range(0.01, 0.5)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BlobShadow"
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float d = distance(input.uv, float2(0.5, 0.5)) * 2.0; // 0 중심 ~ 1 가장자리
                half a = _Color.a * (1.0 - smoothstep(1.0 - _Softness * 2.0, 1.0, d));
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
