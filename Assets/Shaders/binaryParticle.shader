Shader "Custom/BinaryShroudParticle"
{
    Properties
    {
        [HDR]_Color0 ("Color Zero", Color) = (0.2, 0.8, 0.2, 1.0)
        [HDR]_Color1 ("Color One", Color) = (0.5, 0.9, 0.5, 1.0)

        _Thickness ("Font Thickness", Float) = 0.04

        _FlipSpeed ("Base Morph Flip Speed", Float) = 4.0

        _WarpAmp ("Gas Distortion Amp", Float) = 0.02
        _WarpFreq ("Gas Distortion Freq", Float) = 15.0
        _WarpSpeed ("Gas Distortion Speed", Float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "SDF.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color0;
            float4 _Color1;
            float _Thickness;
            float _FlipSpeed;
            float _WarpAmp;
            float _WarpFreq;
            float _WarpSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                // xy = UV, zw = Random xy
                float4 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv; // Passes all 4 components (xyzw) automatically
                output.color = input.color;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 1. Center the UVs (-0.5 to 0.5) from the .xy channels
                float2 uv = input.uv.xy - 0.5;

                // 2. Extract our random seeds from the .zw channels
                float id = input.uv.z;
                float id2 = input.uv.w;

                // 3. Gas Distortion (Warping)
                float waveX = sin(uv.y * _WarpFreq + _Time.y * _WarpSpeed + id * 50.0);
                float waveY = cos(uv.x * _WarpFreq + _Time.y * _WarpSpeed * 0.8 + id * 30.0);
                uv.x += waveX * _WarpAmp;
                uv.y += waveY * _WarpAmp * 0.8;

                // 4. Morphing Logic (0 to 1)
                float localFlipSpeed = _FlipSpeed * lerp(0.7, 1.3, id2);
                float flipWave = sin(_Time.y * localFlipSpeed + id * 100.0);
                float morph = smoothstep( -0.2, 0.2, flipWave);

                // 5. Evaluate SDF
                float d = lerp(sdfZero(uv, _Thickness), sdfOne(uv, _Thickness), morph);

                // 6. Coloring & Glow
                float3 baseCol = lerp(_Color0.rgb, _Color1.rgb, morph);
                float shapeAlpha = smoothstep(0.02, 0.0, d);
                float glow = saturate(0.005 / max(d, 0.001));

                // Combine shape, glow, and the Particle System's alpha
                float finalAlpha = (shapeAlpha + glow * 0.5) * input.color.a;
                float3 finalColor = baseCol * input.color.rgb;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}