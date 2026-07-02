Shader "Custom/Alpha_Hard_Edge_Control"
{
    Properties
    {
        [Header(Base Texture)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        [Header(Alpha Control)]
        _AlphaStrength ("Alpha Strength", Range(0,1)) = 1
        _AlphaPower ("Alpha Density", Range(0.2,3)) = 1

        [Header(Alpha Edge Hardening)]
        [Toggle] _UseHardAlphaEdge ("Use Hard Alpha Edge", Float) = 1
        _HardAlphaThreshold ("Hard Alpha Threshold", Range(0,1)) = 0.1
        _HardAlphaFeather ("Hard Alpha Feather", Range(0.001,0.5)) = 0.03
        _HardAlphaAmount ("Hard Alpha Amount", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;

            float _AlphaStrength;
            float _AlphaPower;

            float _UseHardAlphaEdge;
            float _HardAlphaThreshold;
            float _HardAlphaFeather;
            float _HardAlphaAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float ApplyHardAlpha(float a)
            {
                if (_UseHardAlphaEdge < 0.5)
                    return a;

                float feather = max(_HardAlphaFeather, 0.0001);

                float hardA = smoothstep(
                    _HardAlphaThreshold - feather,
                    _HardAlphaThreshold + feather,
                    a
                );

                return lerp(a, hardA, _HardAlphaAmount);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                float alpha = tex.a;

                alpha = ApplyHardAlpha(alpha);

                alpha *= _Color.a * _AlphaStrength;
                alpha = pow(saturate(alpha), _AlphaPower);

                fixed4 col = tex * _Color;
                col.a = alpha;

                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}