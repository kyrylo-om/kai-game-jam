Shader "Custom/CinemachineFucker"
{
    Properties
    {
        [MainColor]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        //акуратно з цією фігнею бо без неї альфа просирається
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            //акуратно з цією фігнею бо без неї альфа просирається
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            //шейдер пашоль
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define DARK_COMMON_LIT_INCLUDED 1

            float4 _MainTex_TexelSize;
            #include "SDF.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TexturePointSmoothUV(IN.uv)) * _BaseColor;
                return color;
            }
            ENDHLSL
        }
    }
}
