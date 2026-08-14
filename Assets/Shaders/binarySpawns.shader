Shader "Custom/BinarySpawnsGridless"
{
    Properties
    {
        // Вбудована змінна для спрайтів (Unity заповнює автоматично)
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [HDR] _Color0 ("Color Zero", Color) = (0.2, 0.8, 0.2, 1.0)
        [HDR] _Color1 ("Color One", Color) = (0.5, 0.9, 0.5, 1.0)
        
        _Scale ("Canvas Scale", Float) = 15.0 
        _Thickness ("Font Thickness", Float) = 0.04
        
        // НОВЕ: Параметр для розмиття країв екрану/спрайта
        _EdgeFade ("Edge Fade Spread", Range(0.0, 0.5)) = 0.15
        
        _DigitCount ("Max Digits on Screen", Integer) = 40
        _CycleDuration ("Total Cycle (Sec)", Float) = 9.0 
        _DriftSpeed ("Global Float Speed", Float) = 1.5 
        
        _FlipSpeed ("Base Morph Flip Speed", Float) = 4.0 
        
        _WarpAmp ("Warp Amplitude", Float) = 0.015
        _WarpFreq ("Warp Frequency", Float) = 15.0
        _WarpSpeed ("Warp Speed", Float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend One One 
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #include "SDF.hlsl" 

            // Оголошуємо змінні текстури поза CBUFFER (вимога URP/SRP)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // x=1/w, y=1/h, z=w, w=h

            CBUFFER_START(UnityPerMaterial)
                float4 _Color0;
                float4 _Color1;
                float _Scale;
                float _Thickness;
                float _EdgeFade; // Наша нова змінна
                int _DigitCount;
                float _CycleDuration;
                float _DriftSpeed;
                float _FlipSpeed;
                float _WarpAmp;
                float _WarpFreq;
                float _WarpSpeed;
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
                // ==========================================
                // ASPECT RATIO ТА EDGE FADE
                // ==========================================
                
                // 1. Вираховуємо пропорції спрайта/екрана (ширина / висота)
                float aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                
                // Переводимо UV з (0...1) в (-0.5...0.5)
                float2 centeredUV = input.uv - 0.5;
                
                // Коригуємо вісь X, щоб цифри не розтягувалися на широких спрайтах
                centeredUV.x *= aspect;
                
                float2 baseUV = centeredUV * _Scale;
                float3 finalColor = float3(0.0, 0.0, 0.0);

                // 2. Створюємо маску затухання по краях (через базові UV 0...1)
                // smoothstep(0, _EdgeFade) робить плавний перехід від 0 до 1 на початку
                // smoothstep(1, 1 - _EdgeFade) робить плавний перехід від 1 до 0 в кінці
                float fadeX = smoothstep(0.0, _EdgeFade, input.uv.x) * smoothstep(1.0, 1.0 - _EdgeFade, input.uv.x);
                float fadeY = smoothstep(0.0, _EdgeFade, input.uv.y) * smoothstep(1.0, 1.0 - _EdgeFade, input.uv.y);
                float edgeFadeMask = fadeX * fadeY;

                // ==========================================
                // GRIDLESS ПІДХІД
                // ==========================================
                [unroll]
                for (int i = 0; i < 50; i++)
                {
                    float id = float(i) + 1.234;
                    
                    float timeOffset = rand(float2(id, 12.34)) * _CycleDuration;
                    float rawTime = _Time.y + timeOffset;
                    
                    float cycle = floor(rawTime / _CycleDuration * 1.0);
                    float localT = fmod(rawTime / 2, _CycleDuration);
                    
                    float alphaFade = smoothstep(0.0, 1.0, localT) - smoothstep(6.0, 7.0, localT);
                    
                    if (alphaFade <= 0.001) continue;

                    // Збільшуємо зону спавну по осі X відповідно до aspect ratio, 
                    // щоб цифри не зникали різко по боках широкого екрану
                    float spawnX = (rand(float2(id, cycle + 1.0)) - 0.5) * _Scale * aspect * 1.2;
                    float spawnY = (rand(float2(id, cycle + 2.0)) - 0.5) * _Scale * 1.2;
                    float2 spawnPos = float2(spawnX, spawnY);
                    
                    float driftAngle = rand(float2(id, cycle + 3.0)) * 6.28318;
                    float2 driftDir = float2(cos(driftAngle), sin(driftAngle));
                    
                    float speed = _DriftSpeed * lerp(0.3, 1.5, rand(float2(id, cycle + 4.0)));
                    
                    float2 currentPos = spawnPos + (driftDir * localT * speed);
                    float2 localUV = baseUV - currentPos;

                    float waveX = sin(localUV.y * _WarpFreq + _Time.y * _WarpSpeed + id * 50.0);
                    float waveY = cos(localUV.x * _WarpFreq + _Time.y * _WarpSpeed * 0.8 + id * 30.0);
                    localUV.x += waveX * _WarpAmp;
                    localUV.y += waveY * _WarpAmp * 0.8;
                    
                    float localFlipSpeed = _FlipSpeed * lerp(0.3, 2.0, rand(float2(id, cycle + 5.0)));
                    float flipWave = sin(_Time.y * localFlipSpeed + id * 100.0);
                    float morph = smoothstep(-0.2, 0.2, flipWave);
                    
                    float d = lerp(sdfZero(localUV, _Thickness), sdfOne(localUV, _Thickness), morph);
                    
                    float3 col = lerp(_Color0.rgb, _Color1.rgb, morph);
                    float shapeAlpha = smoothstep(0.02, 0.0, d);
                    float glow = saturate(0.005 / max(d, 0.001));
                    
                    shapeAlpha = (shapeAlpha + glow * 0.5) * alphaFade;
                    finalColor += col * shapeAlpha;
                }
                
                // 3. Застосовуємо маску затухання до фінального кольору
                finalColor *= edgeFadeMask;
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}