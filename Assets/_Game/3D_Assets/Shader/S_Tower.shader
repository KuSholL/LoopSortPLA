Shader "Custom/Complete_Ramp_CustomLight_ToonSpec_TCP2Outline_Copy"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.35,0.35,0.35,1)

        [Header(TCP2 Style Outline)]
        [Toggle] _UseOutline ("Enable Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0,10.0)) = 1.5

        [Header(Custom Light Slider 360)]
        [Toggle] _UseCustomLight ("Use Custom Light", Float) = 1
        _LightYaw ("Light Horizontal Angle", Range(-180,180)) = 35
        _LightPitch ("Light Vertical Angle", Range(-180,180)) = 45
        _CustomLightIntensity ("Custom Light Intensity", Range(0,2)) = 1

        [Header(Ramp Shading)]
        [Enum(Replace,0,Multiply,1)] _RampBlendMode ("Ramp Blend Mode", Float) = 0
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.5
        _RampSoftness ("Ramp Softness", Range(0.001,1)) = 0.08
        _Brightness ("Brightness", Range(0,2)) = 1

        [Header(Stylized Specular)]
        [Toggle] _UseSpecular ("Enable Specular", Float) = 1
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularCoverage ("Specular Coverage", Range(0,1)) = 0.25
        _SpecularSoftness ("Specular Softness", Range(0.001,0.5)) = 0.08
        _SpecularIntensity ("Specular Intensity", Range(0,5)) = 1
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
        // OUTLINE PASS - COPY THEO TCP2 STYLE CỦA FILE THAM KHẢO
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
        // Không nhận ánh sáng scene, dùng custom light riêng
        // =====================================================
        Pass
        {
            Name "MAIN_PASS"
            Tags { "LightMode"="Always" }

            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _BaseColor;
            float4 _ShadowColor;

            float _UseCustomLight;
            float _LightYaw;
            float _LightPitch;
            float _CustomLightIntensity;

            float _RampBlendMode;
            float _RampThreshold;
            float _RampSoftness;
            float _Brightness;

            float _UseSpecular;
            float4 _SpecularColor;
            float _SpecularCoverage;
            float _SpecularSoftness;
            float _SpecularIntensity;

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

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float3 GetCustomLightDirection()
            {
                float DEG2RAD = 0.01745329252;

                float yaw = _LightYaw * DEG2RAD;
                float pitch = _LightPitch * DEG2RAD;

                float3 dir;

                dir.x = 0;
                dir.y = sin(pitch);
                dir.z = cos(pitch);

                float x = dir.x;
                float z = dir.z;

                dir.x = x * cos(yaw) + z * sin(yaw);
                dir.z = -x * sin(yaw) + z * cos(yaw);

                return normalize(dir);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float3 baseCol = tex.rgb * _BaseColor.rgb;

                float useCustomLight = step(0.5, _UseCustomLight);
                float useSpecular = step(0.5, _UseSpecular);
                float useMultiplyRamp = step(0.5, _RampBlendMode);

                float3 normalWS = normalize(i.worldNormal);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 lightDirWS = GetCustomLightDirection();

                // =========================
                // RAMP SHADING
                // =========================
                float ndotl = saturate(dot(normalWS, lightDirWS));

                float ramp = smoothstep(
                    _RampThreshold - _RampSoftness,
                    _RampThreshold + _RampSoftness,
                    ndotl
                );

                // Replace mode:
                // Shadow = Shadow Color
                float3 shadowReplaceCol = _ShadowColor.rgb;

                // Multiply mode:
                // Shadow = Base Color * Shadow Color
                float3 shadowMultiplyCol = baseCol * _ShadowColor.rgb;

                // Switch Replace / Multiply
                float3 shadowCol = lerp(
                    shadowReplaceCol,
                    shadowMultiplyCol,
                    useMultiplyRamp
                );

                float3 rampCol = lerp(shadowCol, baseCol, ramp);

                // Tắt custom light thì trả về màu flat
                float3 finalCol = lerp(
                    baseCol,
                    rampCol * _CustomLightIntensity,
                    useCustomLight
                );

                // =========================
                // STYLIZED SPECULAR
                // =========================
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float specRaw = saturate(dot(normalWS, halfDir));

                // Coverage cao = vùng specular rộng
                float specThreshold = lerp(0.98, 0.15, _SpecularCoverage);

                float spec = smoothstep(
                    specThreshold - _SpecularSoftness,
                    specThreshold + _SpecularSoftness,
                    specRaw
                );

                spec *= ramp;
                spec *= useCustomLight;
                spec *= useSpecular;

                finalCol += _SpecularColor.rgb * spec * _SpecularIntensity;

                finalCol *= _Brightness;

                return fixed4(finalCol, tex.a * _BaseColor.a);
            }

            ENDCG
        }

        // =====================================================
        // OUTLINE DEPTH PASS - COPY THEO FILE THAM KHẢO
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

    FallBack "VertexLit"
}