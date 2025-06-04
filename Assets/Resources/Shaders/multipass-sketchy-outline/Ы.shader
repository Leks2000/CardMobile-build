Shader "Custom/OutlineNoiseFresnel"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Float) = 0.1
        _NoiseTex ("Outline Noise", 2D) = "white" {}
        _FalloffTex ("Falloff Curve", 2D) = "white" {}
        _Scissor ("Alpha Scissor", Float) = 0.5
        _UVScale ("UV Scale", Vector) = (1,1,0,0)
        _OffsetFresnel ("Offset Fresnel", Float) = 0.3
        _FPS ("FPS", Float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            sampler2D _FalloffTex;
            float4 _OutlineColor;
            float _OutlineSize;
            float _Scissor;
            float4 _UVScale;
            float _OffsetFresnel;
            float _FPS;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float noise = tex2Dlod(_NoiseTex, float4(v.uv, 0, 0)).r;

                float3 offset = v.normal * _OutlineSize * noise;

                float4 displacedVertex = v.vertex;
                displacedVertex.xyz += offset;

                o.pos = UnityObjectToClipPos(displacedVertex);
                o.uv = v.uv;

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, displacedVertex).xyz;

                return o;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                float fres = abs(dot(-viewDir, normal));
                fres = saturate(fres);

                float angle = atan2(normal.y, normal.x) / 3.14;
                float t = floor(_Time.y * _FPS) / _FPS;

                float2 uv = float2(angle * _UVScale.x + i.uv.x, normal.z * _UVScale.y + t);
                float noise = tex2D(_NoiseTex, uv).r;

                float fresCurve = tex2D(_FalloffTex, float2(1.0 - fres, 0)).r + _OffsetFresnel;
                float alpha = noise * fresCurve;

                if (alpha < _Scissor)
                    discard;

                return float4(_OutlineColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
