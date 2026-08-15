Shader "UI/ProceduralBoxVignette"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}

        _CenterColor ("Center Color", Color) = (0.2, 0.2, 0.2, 0.8)
        _EdgeColor ("Edge Color", Color) = (0.0, 0.0, 0.0, 0.95)
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.3

        // Required UI Stencil properties (keeps it from breaking if you use UI Masks)
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _CenterColor;
            fixed4 _EdgeColor;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Box Vignette Math
                // smoothstep creates a smooth fade from the edges toward the center
                float fadeX = smoothstep(0.0, _Softness, IN.texcoord.x) * smoothstep(1.0, 1.0 - _Softness, IN.texcoord.x);
                float fadeY = smoothstep(0.0, _Softness, IN.texcoord.y) * smoothstep(1.0, 1.0 - _Softness, IN.texcoord.y);

                // Combine X and Y to create a rounded box mask
                float boxMask = fadeX * fadeY;

                // Lerp between the dark edge color and the lighter center color
                half4 gradientColor = lerp(_EdgeColor, _CenterColor, boxMask);

                // Multiply by the UI Canvas vertex color so CanvasGroup Alpha fading still works
                half4 finalColor = gradientColor * IN.color;

                // Apply standard UI masking logic
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return finalColor;
            }
            ENDCG
        }
    }
}