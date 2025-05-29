Shader "Unlit/HypnoSpiral_Shader"
{
    Properties
    {
        _Speed("Speed", Float) = 1.0
        _Frequency("Frequency", Float) = 30.0
        _ColorA("Color A", Color) = (1, 1, 1, 1)
        _ColorB("Color B", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            float _Speed;
            float _Frequency;
            fixed4 _ColorA;
            fixed4 _ColorB;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ÷ентрируем UV
                float2 uv = i.uv * 2.0 - 1.0;

                // ѕеревод в пол€рные координаты
                float angle = atan2(uv.y, uv.x);
                float radius = length(uv);

                // јнимаци€ по времени
                float time = _Time.y * _Speed;
                float wave = sin(angle * _Frequency + time);

                // ћаска Ч узор спирали
                float mask = smoothstep(0.0, 0.05, wave);

                // ѕеремешиваем два цвета
                fixed4 col = lerp(_ColorA, _ColorB, mask);

                return col;
            }
            ENDCG
        }
    }
}
