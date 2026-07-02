Shader "Custom/Rope_3D_TwoColor_Ramp_Replace_OutlineBlend_Specular_CustomDirection"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Base Texture", 2D) = "white" {}

        _LeftColor ("Left Color", Color) = (1, 0.35, 0.05, 1)
        _RightColor ("Right Color", Color) = (0.1, 0.45, 1, 1)

        [Header(Shadow Replace Colors)]
        _LeftShadowColor ("Left Shadow Color", Color) = (0.55, 0.18, 0.03, 1)
        _RightShadowColor ("Right Shadow Color", Color) = (0.03, 0.18, 0.55, 1)

        [Header(Color Blend By UV X)]
        _BlendOffset ("Blend Offset", Range(0,1)) = 0.5
        _BlendSoftness ("Blend Softness", Range(0.001,1)) = 0.25

        [Header(Custom Light Direction)]
        [Toggle] _UseCustomLightDirection ("Use Custom Light Direction", Float) = 0
        _CustomLightYaw ("Light Direction Horizontal", Range(-180,180)) = 0
        _CustomLightPitch ("Light Direction Vertical", Range(-180,180)) = 45

        [Header(Ramp Shading Replace)]
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.45
        _RampSoftness ("Ramp Softness", Range(0.001,0.5)) = 0.06
        _RampStrength ("Ramp Strength", Range(0,1)) = 1

        [Header(Light Area)]
        _LightTint ("Light Tint", Color) = (1, 1, 1, 1)

        [Header(Stylized Specular)]
        [Toggle] _UseSpecular ("Use Specular", Float) = 1
        [HDR] _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularIntensity ("Specular Intensity", Range(0,5)) = 1
        _SpecularThreshold ("Specular Threshold", Range(0,1)) = 0.75
        _SpecularSoftness ("Specular Softness", Range(0.001,0.5)) = 0.06
        _SpecularSharpness ("Specular Sharpness", Range(0.2,8)) = 2
        [Toggle] _SpecularOnlyLightArea ("Specular Only Light Area", Float) = 1
        [Toggle] _SpecularUseLightColor ("Specular Use Unity Light Color", Float) = 0

        [Header(Outline)]
        [Toggle] _UseOutline ("Use Outline", Float) = 1
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.015
        _LeftOutlineColor ("Left Outline Color", Color) = (0.55, 0.12, 0.02, 1)
        _RightOutlineColor ("Right Outline Color", Color) = (0.02, 0.12, 0.55, 1)
        _OutlineBrightness ("Outline Brightness", Range(0,2)) = 1

        [Header(Outline Blend)]
        [Toggle] _OutlineUseSameBlend ("Outline Use Same Blend", Float) = 1
        _OutlineBlendOffset ("Outline Blend Offset", Range(0,1)) = 0.5
        _OutlineBlendSoftness ("Outline Blend Softness", Range(0.001,1)) = 0.25

        [Header(Final)]
        _Brightness ("Brightness", Range(0,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // =========================
        // OUTLINE PASS
        // =========================
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            float _UseOutline;
            float _OutlineWidth;

            fixed4 _LeftOutlineColor;
            fixed4 _RightOutlineColor;
            float _OutlineBrightness;

            float _BlendOffset;
            float _BlendSoftness;

            float _OutlineUseSameBlend;
            float _OutlineBlendOffset;
            float _OutlineBlendSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 rawUV : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 normalDir = normalize(v.normal);
                float outlineWidth = _OutlineWidth * _UseOutline;

                float4 expandedPos = v.vertex;
                expandedPos.xyz += normalDir * outlineWidth;

                o.pos = UnityObjectToClipPos(expandedPos);
                o.rawUV = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(_UseOutline - 0.5);

                float blendOffset = lerp(_OutlineBlendOffset, _BlendOffset, _OutlineUseSameBlend);
                float blendSoftness = lerp(_OutlineBlendSoftness, _BlendSoftness, _OutlineUseSameBlend);

                float outlineBlendMask = smoothstep(
                    blendOffset - blendSoftness,
                    blendOffset + blendSoftness,
                    i.rawUV.x
                );

                fixed4 outlineCol = lerp(_LeftOutlineColor, _RightOutlineColor, outlineBlendMask);
                outlineCol.rgb *= _OutlineBrightness;

                return outlineCol;
            }
            ENDCG
        }

        // =========================
        // MAIN PASS
        // =========================
        Pass
        {
            Name "Main"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _LeftColor;
            fixed4 _RightColor;

            fixed4 _LeftShadowColor;
            fixed4 _RightShadowColor;

            float _BlendOffset;
            float _BlendSoftness;

            float _UseCustomLightDirection;
            float _CustomLightYaw;
            float _CustomLightPitch;

            float _RampThreshold;
            float _RampSoftness;
            float _RampStrength;

            fixed4 _LightTint;
            float _Brightness;

            float _UseSpecular;
            fixed4 _SpecularColor;
            float _SpecularIntensity;
            float _SpecularThreshold;
            float _SpecularSoftness;
            float _SpecularSharpness;
            float _SpecularOnlyLightArea;
            float _SpecularUseLightColor;

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
                float2 rawUV : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            float3 GetCustomLightDirection(float yawDeg, float pitchDeg)
            {
                float yaw = radians(yawDeg);
                float pitch = radians(pitchDeg);

                float cosPitch = cos(pitch);

                float3 dir;
                dir.x = sin(yaw) * cosPitch;
                dir.y = sin(pitch);
                dir.z = cos(yaw) * cosPitch;

                return normalize(dir);
            }

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rawUV = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texCol = tex2D(_MainTex, i.uv);

                // =========================
                // BASE COLOR BLEND
                // =========================
                float colorBlendMask = smoothstep(
                    _BlendOffset - _BlendSoftness,
                    _BlendOffset + _BlendSoftness,
                    i.rawUV.x
                );

                fixed4 baseColor = lerp(_LeftColor, _RightColor, colorBlendMask);
                fixed4 shadowColor = lerp(_LeftShadowColor, _RightShadowColor, colorBlendMask);

                baseColor *= texCol;

                // =========================
                // LIGHT DIRECTION
                // =========================
                float3 unityLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float3 customLightDir = GetCustomLightDirection(_CustomLightYaw, _CustomLightPitch);

                float3 L = normalize(lerp(
                    unityLightDir,
                    customLightDir,
                    _UseCustomLightDirection
                ));

                // =========================
                // RAMP SHADING REPLACE
                // =========================
                float3 N = normalize(i.worldNormal);

                float ndotl = saturate(dot(N, L));

                float rampMask = smoothstep(
                    _RampThreshold - _RampSoftness,
                    _RampThreshold + _RampSoftness,
                    ndotl
                );

                fixed3 lightArea = baseColor.rgb * _LightTint.rgb * _LightColor0.rgb;
                fixed3 shadowArea = shadowColor.rgb;

                fixed3 rampReplaceColor = lerp(shadowArea, lightArea, rampMask);
                fixed3 finalColor = lerp(baseColor.rgb, rampReplaceColor, _RampStrength);

                // =========================
                // STYLIZED SPECULAR
                // =========================
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 H = normalize(L + V);

                float ndoth = saturate(dot(N, H));

                float specMask = smoothstep(
                    _SpecularThreshold - _SpecularSoftness,
                    _SpecularThreshold + _SpecularSoftness,
                    ndoth
                );

                specMask = pow(specMask, _SpecularSharpness);

                float specLightAreaMask = smoothstep(
                    _RampThreshold - _RampSoftness,
                    _RampThreshold + _RampSoftness,
                    ndotl
                );

                specMask *= lerp(1.0, specLightAreaMask, _SpecularOnlyLightArea);

                fixed3 specLightColor = lerp(
                    fixed3(1, 1, 1),
                    _LightColor0.rgb,
                    _SpecularUseLightColor
                );

                fixed3 specularFinal =
                    _SpecularColor.rgb *
                    specLightColor *
                    _SpecularIntensity *
                    specMask *
                    _UseSpecular;

                finalColor += specularFinal;

                // =========================
                // FINAL
                // =========================
                finalColor *= _Brightness;

                return fixed4(finalColor, baseColor.a);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}