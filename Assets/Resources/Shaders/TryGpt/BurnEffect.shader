Shader "Custom/BurnEffect"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
        _BurnTex ("Burn Mask", 2D) = "white" {}
        _BurnColor ("Burn Color", Color) = (1, 0.4, 0, 1)
        _EdgeColor ("Edge Color", Color) = (1, 1, 0, 1)
        _BurnProgress ("Burn Progress", Range(0,1)) = 1.0
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BurnTex;
            float4 _MainTex_ST;

            float4 _BurnColor;
            float4 _EdgeColor;
            float _BurnProgress;
            float _EdgeWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float burnValue = tex2D(_BurnTex, i.uv).r;
                float4 baseColor = tex2D(_MainTex, i.uv);

                // === Чёрный полупрозрачный фон — по умолчанию ===
                float4 backgroundColor = float4(0, 0, 0, 0.5); // Полупрозрачный чёрный

                // === Граница огня ===
                float edge = smoothstep(_BurnProgress - _EdgeWidth, _BurnProgress, burnValue);

                // === Цвет края и пламени ===
                float4 mixColor = lerp(_EdgeColor, _BurnColor, edge);

                // === Проявляем MainTex (текст и т.п.) только при сгорании ===
                float showContent = smoothstep(_BurnProgress - _EdgeWidth, _BurnProgress + _EdgeWidth, burnValue);
                float4 burnedContent = lerp(baseColor, mixColor, edge);

                // === Собираем финальный цвет ===
                float4 finalColor = lerp(backgroundColor, burnedContent, showContent);

                // === Маска для исчезновения — убираем после сгорания ===
                float alphaMask = 1.0 - smoothstep(_BurnProgress, _BurnProgress + _EdgeWidth, burnValue);
                finalColor.a *= alphaMask;

                return finalColor;
            }

            ENDCG
        }
    }
}
