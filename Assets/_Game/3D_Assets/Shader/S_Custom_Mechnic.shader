Shader "PLA/Custom_Cube_Mechanic_Lite"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _HColor ("Highlight Color", Color) = (1,1,1,1)
        _SColor ("Shadow Color", Color) = (0.65,0.7,0.85,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _RampThreshold ("Light Threshold", Range(0,1)) = 0.35
        _RampSmoothing ("Light Smoothing", Range(0.001,1)) = 0.2
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularToonSize ("Specular Size", Range(0,1)) = 0
        _SpecularToonSmoothness ("Specular Smoothness", Range(0.001,0.5)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _HColor;
            fixed4 _SColor;
            half _RampThreshold;
            half _RampSmoothing;
            fixed4 _SpecularColor;
            half _SpecularToonSize;
            half _SpecularToonSmoothness;

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
                half3 worldNormal : TEXCOORD1;
                half3 viewDir : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = UnityWorldSpaceViewDir(worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 normal = normalize(i.worldNormal);
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half ndl = saturate(dot(normal, lightDir));

                half smoothWidth = max(_RampSmoothing, 0.001h);
                half ramp = smoothstep(_RampThreshold - smoothWidth * 0.5h, _RampThreshold + smoothWidth * 0.5h, ndl);
                fixed3 lightTint = lerp(_SColor.rgb, _HColor.rgb, ramp);

                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed3 color = tex.rgb * _Color.rgb * lightTint;

                half3 viewDir = normalize(i.viewDir);
                half3 halfDir = normalize(lightDir + viewDir);
                half specBase = saturate(dot(normal, halfDir));
                half spec = smoothstep(
                    saturate(1.0h - _SpecularToonSize - _SpecularToonSmoothness),
                    saturate(1.0h - _SpecularToonSize),
                    specBase);
                color += spec * _SpecularColor.rgb * saturate(_SpecularToonSize);

                return fixed4(color, tex.a * _Color.a);
            }
            ENDCG
        }
    }

    Fallback "Mobile/Diffuse"
}
