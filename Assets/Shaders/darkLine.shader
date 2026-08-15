Shader "Custom/BlackGradientLineParticles"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (0.05, 0.05, 0.05, 1.0) // Майже чорний
        [HDR]_ParticleColor ("Particle Color", Color) = (0.4, 0.4, 0.4, 1.0) // Світліший для вкраплень

        _LineAngle ("Line Angle", Float) = 0.5 // Кут нахилу лінії
        _LineThickness ("Line Thickness", Float) = 0.02
        _LineSoftness ("Line Edge Softness", Float) = 0.05

        // Градієнт прозорості (початок і кінець затухання)
        _GradStart ("Gradient Solid Point", Float) = -0.3
        _GradEnd ("Gradient Fade Point", Float) = 0.4

        // Налаштування частинок (SDF Circles)
        _ParticleDensity ("Particle Grid Scale", Float) = 40.0
        _ParticleSize ("Base Particle Size", Float) = 0.2
        _ParticleAmount ("Particle Amount (0 to 1)", Float) = 0.3
        _ParticleSpeed ("Particle Drift Speed", Float) = 0.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"}

        // Класичний альфа-бленд (необхідний для чорного кольору)
        Blend SrcAlpha OneMinusSrcAlpha
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
            float4 _LineColor;
            float4 _ParticleColor;
            float _LineAngle;
            float _LineThickness;
            float _LineSoftness;
            float _GradStart;
            float _GradEnd;
            float _ParticleDensity;
            float _ParticleSize;
            float _ParticleAmount;
            float _ParticleSpeed;
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
                // 1. ПІДГОТОВКА КООРДИНАТ ТА ПОВОРОТ
                float2 centeredUV = input.uv - 0.5;

                // Матриця повороту для лінії
                float s = sin(_LineAngle);
                float c = cos(_LineAngle);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                float2 rotatedUV = mul(rotMatrix, centeredUV);

                // ==========================================
                // 2. БАЗОВА ЛІНІЯ (SDF BOX)
                // ==========================================
                // Використовуємо sdBox з твоєї бібліотеки. X робимо дуже великим, щоб лінія виходила за екран
                float lineSDF = sdBox(rotatedUV, float2(2.0, _LineThickness));

                // Робимо м'які краї (Anti-aliasing / Softness)
                float lineMask = smoothstep(_LineSoftness, 0.0, lineSDF);

                // ==========================================
                // 3. АЛЬФА ГРАДІЄНТ
                // ==========================================
                // Використовуємо локальну координату X повернутої лінії
                // smoothstep зробить плавний перехід від 1.0 (непрозорий) до 0.0 (прозорий)
                float alphaGradient = 1.0 - smoothstep(_GradStart, _GradEnd, rotatedUV.x);
                lineMask *= alphaGradient;

                // ==========================================
                // 4. ВКРАПЛЕННЯ (SDF CIRCLES)
                // ==========================================
                // Створюємо сітку для частинок, яка "їде" вздовж лінії
                float2 particleUV = rotatedUV;
                particleUV.x -= _Time.y * _ParticleSpeed; // Рух частинок
                particleUV *= _ParticleDensity; // Масштаб сітки

                float2 cellID = floor(particleUV);
                float2 localCellUV = frac(particleUV) - 0.5;

                // Рандомізація для кожної комірки
                float cellSeed = rand(cellID, 1.0);

                // Випадковий зсув частинки всередині комірки (-0.3 .. 0.3)
                float offsetX = (rand(cellID, 2.0) - 0.5) * 0.6;
                float offsetY = (rand(cellID, 3.0) - 0.5) * 0.6;
                float2 particleCenter = float2(offsetX, offsetY);

                // Визначаємо випадковий розмір
                float pSize = _ParticleSize * rand(cellID, 4.0);

                // Малюємо коло через твою бібліотеку
                float dCircle = sdCircle(localCellUV - particleCenter, pSize);
                float particleAlpha = smoothstep(0.05, 0.0, dCircle);

                // Відфільтровуємо частинки (щоб вони були не в кожній комірці)
                float spawnChance = step(1.0 - _ParticleAmount, cellSeed);
                particleAlpha *= spawnChance;

                // ВАЖЛИВО: Маскуємо частинки, щоб вони були ТІЛЬКИ всередині нашої чорної лінії
                // smoothstep тут трохи ширший, щоб частинки могли злегка виступати за краї лінії
                float boundsMask = smoothstep(_LineThickness + 0.02, 0.0, lineSDF);
                particleAlpha *= boundsMask * alphaGradient; // Затухають разом із лінією

                // ==========================================
                // 5. ЗМІШУВАННЯ КОЛЬОРІВ ТА АЛЬФИ
                // ==========================================
                float3 finalColor = lerp(_LineColor.rgb, _ParticleColor.rgb, particleAlpha);

                // Загальна прозорість = прозорість лінії + прозорість частинок
                float finalAlpha = saturate(lineMask + particleAlpha);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}