Shader "VibeCode/StylizedBow_Final_Rim_TCP2_Outline"
{
    Properties
    {
        [Header(Shader Toggle)]
        [Toggle] _UseStylizedFX ("Enable Stylized Shader FX", Float) = 1

        [Header(Base Settings)]
        _MainTex ("Base Texture RGB", 2D) = "white" {}
        _Color ("Base Color Tint", Color) = (1, 1, 1, 1)
        _ShadowColor ("Core Shadow Color", Color) = (0.4, 0.2, 0.8, 1)

        [Header(Gradient AO Mapping)]
        [NoScaleOffset] _GradientAO ("Gradient AO Grayscale", 2D) = "white" {}
        _FakeAOIntensity ("Gradient AO Intensity", Range(0.0, 1.0)) = 1.0

        [Header(Stylized Specular)]
        [HDR] _SpecularColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _Glossiness ("Highlight Position N.H", Range(0.01, 1.0)) = 0.85
        _GlossSoftness ("Highlight Softness", Range(0.001, 0.5)) = 0.05

        [Header(Environment Reflection)]
        [NoScaleOffset] _Cubemap ("Reflection Cubemap", Cube) = "" {}
        _ReflectColor ("Reflection Tint", Color) = (1, 1, 1, 1)
        _ReflectIntensity ("Reflection Intensity", Range(0.0, 1.0)) = 0.3
        _ReflectRoughness ("Reflection Roughness Blur", Range(0.0, 1.0)) = 0.5 

        [Header(Stylized Rim Light)]
        [HDR] _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimWidth ("Rim Width", Range(0.0, 1.0)) = 0.7
        _RimSoftness ("Rim Softness", Range(0.001, 0.5)) = 0.05

        [Header(TCP2 Style Outline)]
        [Toggle] _UseOutline ("Enable Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)

        // Giống TCP2: width không phải world unit.
        // Giá trị đẹp thường là 0.5 - 4.
        _OutlineWidth ("Outline Width", Range(0.0, 10.0)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="AlphaTest+25"
        }

        LOD 100

        // =====================================================
        // OUTLINE PASS - TCP2 CLIP SPACE STYLE
        // Vẽ trước main mesh, main mesh sẽ che phần outline bên trong.
        // =====================================================
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="ForwardBase" }

            Cull Off
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert_outline
            #pragma fragment frag_outline
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata_outline
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f_outline
            {
                float4 pos : SV_POSITION;
            };

            float _UseOutline;
            float _OutlineWidth;
            float4 _OutlineColor;

            v2f_outline vert_outline(appdata_outline v)
            {
                v2f_outline o;

                float4 clipPos = UnityObjectToClipPos(v.vertex.xyz);

                // TCP2 style:
                // normal object -> world -> clip direction
                float3 worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;

                float2 clipNormal = mul(UNITY_MATRIX_VP, float4(worldNormal, 0.0)).xy;
                clipNormal = normalize(clipNormal + 0.000001);

                // Fix aspect ratio giống TCP2
                float2 screenRatio = float2(1.0, _ScreenParams.x / _ScreenParams.y);

                // TCP2 dùng width / 100
                float2 outlineOffset = (_OutlineWidth / 100.0) * screenRatio;

                clipPos.xy += clipNormal.xy * outlineOffset;

                o.pos = clipPos;
                return o;
            }

            fixed4 frag_outline(v2f_outline i) : SV_Target
            {
                clip(_UseOutline - 0.5);
                return _OutlineColor;
            }

            ENDCG
        }

        // =====================================================
        // MAIN PASS
        // =====================================================
        Pass
        {
            Name "FORWARD_BASE"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0 
            
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

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
                float3 worldViewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _GradientAO; 
            samplerCUBE _Cubemap; 
            
            float _UseStylizedFX;

            float4 _Color;
            float4 _ShadowColor;

            float4 _SpecularColor;
            float _Glossiness;
            float _GlossSoftness;
            float _FakeAOIntensity;
            
            float4 _ReflectColor;
            float _ReflectIntensity;
            float _ReflectRoughness;

            float4 _RimColor;
            float _RimWidth;
            float _RimSoftness;

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); 
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldViewDir = WorldSpaceViewDir(v.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

                if (_UseStylizedFX < 0.5)
                {
                    return fixed4(texColor.rgb, 1.0);
                }

                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(i.worldViewDir);
                float3 H = normalize(L + V);

                float NdotL = dot(N, L);
                float halfLambert = NdotL * 0.5 + 0.5;

                float3 diffuseTerm = lerp(_ShadowColor.rgb, texColor.rgb, halfLambert);

                float gradientSample = tex2D(_GradientAO, i.uv).r;
                float ao = lerp(1.0, gradientSample, _FakeAOIntensity);
                diffuseTerm *= ao;

                float NdotH = max(0, dot(N, H));

                float specIntensity = smoothstep(
                    _Glossiness - _GlossSoftness, 
                    _Glossiness + _GlossSoftness, 
                    NdotH
                );

                float3 specularTerm = specIntensity * _SpecularColor.rgb * _LightColor0.rgb;
                specularTerm *= saturate(halfLambert * 2.0) * ao;

                float NdotV = max(0, dot(N, V));
                float rimBase = 1.0 - NdotV;

                float rimIntensity = smoothstep(
                    _RimWidth - _RimSoftness, 
                    _RimWidth + _RimSoftness, 
                    rimBase
                );

                float3 rimTerm = rimIntensity * _RimColor.rgb;
                rimTerm *= saturate(halfLambert) * ao;

                float3 reflDir = reflect(-V, N);
                float mipLOD = _ReflectRoughness * 6.0;

                float3 reflectionTerm = texCUBElod(_Cubemap, float4(reflDir, mipLOD)).rgb;
                reflectionTerm *= _ReflectColor.rgb * _ReflectIntensity;
                reflectionTerm *= ao * saturate(halfLambert);

                float3 finalRGB = diffuseTerm + specularTerm + rimTerm + reflectionTerm;
                
                return fixed4(finalRGB, 1.0);
            }

            ENDCG
        }

        // =====================================================
        // OUTLINE DEPTH PASS
        // Chỉ ghi depth cho outline, không ghi màu.
        // Giúp outline có cảm giác đứng sau mesh/prop khác.
        // =====================================================
        Pass
        {
            Name "OUTLINE_DEPTH"
            Tags { "LightMode"="ForwardBase" }

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert_outline
            #pragma fragment frag_outline_depth
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata_outline
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f_outline
            {
                float4 pos : SV_POSITION;
            };

            float _UseOutline;
            float _OutlineWidth;

            v2f_outline vert_outline(appdata_outline v)
            {
                v2f_outline o;

                float4 clipPos = UnityObjectToClipPos(v.vertex.xyz);

                float3 worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;

                float2 clipNormal = mul(UNITY_MATRIX_VP, float4(worldNormal, 0.0)).xy;
                clipNormal = normalize(clipNormal + 0.000001);

                float2 screenRatio = float2(1.0, _ScreenParams.x / _ScreenParams.y);
                float2 outlineOffset = (_OutlineWidth / 100.0) * screenRatio;

                clipPos.xy += clipNormal.xy * outlineOffset;

                o.pos = clipPos;
                return o;
            }

            fixed4 frag_outline_depth(v2f_outline i) : SV_Target
            {
                clip(_UseOutline - 0.5);
                return 0;
            }

            ENDCG
        }
    }

    Fallback "VertexLit"
}