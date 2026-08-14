Shader "Custom/EnergyLaser"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (1, 0, 0, 1)
        _Thickness ("Laser Thickness", Range(0.01, 0.5)) = 0.05
        _PulseSpeed ("Pulse Speed", Float) = 15.0
        
        [Header(Distortion Settings)]
        _WobbleAmp ("Wobble Amplitude", Float) = 0.03
        _WobbleFreq ("Wobble Frequency", Float) = 15.0
        _WobbleSpeed ("Wobble Speed", Float) = 30.0
        
        _NoiseSpeed ("Plasma Noise Speed", Float) = 15.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // ТВОЯ БІБЛІОТЕКА
            #include "SDF.hlsl" 

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _GlowColor;
                float _Thickness;
                float _PulseSpeed;
                float _WobbleAmp;
                float _WobbleFreq;
                float _WobbleSpeed;
                float _NoiseSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float2 uv : TEXCOORD0; float4 positionCS : SV_POSITION; };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centeredUV = float2(uv.x, uv.y - 0.5); 
                
                // ==========================================
                // DOMAIN WARPING (ВИКРИВЛЕННЯ КООРДИНАТ)
                // ==========================================
                
                // 1. Макро-викривлення (велика синусоїдна хвиля вздовж променя)
                float wave = sin(uv.x * _WobbleFreq - _Time.y * _WobbleSpeed) * _WobbleAmp;
                
                // 2. Мікро-викривлення (рваний хаос через шум)
                // Використовуємо твій noise(), щоб зробити вигини непередбачуваними
                float nOffset = noise(float2(uv.x * 10.0, _Time.y * _NoiseSpeed));
                float microChaos = (nOffset - 0.5) * (_WobbleAmp * 0.8);
                
                // ЗМІШУЄМО локальну координату Y!
                centeredUV.y += wave + microChaos;
                // ==========================================
                
                // Пульсація товщини
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float currentThick = _Thickness + (pulse * 0.02);

                // Тепер передаємо ВИКРИВЛЕНІ координати у звичайний sdBox.
                // Box думає, що малює рівну лінію, але простір навколо нього кривий.
                float d = sdBox(centeredUV, float2(10.0, currentThick));
                
                // Додаємо високочастотний нойз до САМОЇ ДИСТАНЦІЇ (робить краї лазера "рваними" і плазмовими)
                float surfaceNoise = noise(uv * float2(30.0, 5.0) - float2(_Time.y * _NoiseSpeed, 0.0));
                d += (surfaceNoise - 0.5) * 0.02;

                // Малювання: Ядро (Core)
                float core = smoothstep(0.01, 0.0, d + currentThick * 0.5);
                
                // Малювання: Світіння (Glow)
                float glow = saturate(0.01 / max(d, 0.001));

                float3 finalColor = (_CoreColor.rgb * core) + (_GlowColor.rgb * glow);
                
                return half4(finalColor, core + glow);
            }
            ENDHLSL
        }
    }
}