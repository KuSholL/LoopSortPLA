Shader "Custom/SplineFollowerRoad"
{
    Properties
    {
        _LineShapeTex ("Line Shape", 2D) = "white" {}
        _FollowerColor ("Follower Color", Color) = (0.25, 0.85, 1, 1)
        _FollowerAlpha ("Follower Alpha", Range(0, 1)) = 1
        _FollowerEmission ("Follower Emission", Range(0, 5)) = 1.5
        _FollowerSpacing ("Follower Spacing", Range(0.05, 10)) = 1
        _FollowerLength ("Follower Length", Range(0.05, 1)) = 0.8
        _FollowerSoftness ("Follower Softness", Range(0.001, 0.3)) = 0.08
        _LineWidth ("Line Width", Range(0.05, 1)) = 0.42
        _LineSoftness ("Line Softness", Range(0.001, 0.3)) = 0.08
        _RoadColor ("Road Color", Color) = (0.43, 0.43, 0.62, 1)
        _RoadHighlightColor ("Road Highlight Color", Color) = (0.68, 0.69, 0.88, 1)
        _RoadEdgeColor ("Road Edge Color", Color) = (0.90, 0.91, 1, 1)
        _RoadInnerLineColor ("Road Inner Line Color", Color) = (0.88, 0.90, 1, 1)
        _RoadEdgeWidth ("Road Edge Width", Range(0.01, 0.25)) = 0.085
        _RoadInnerLineOffset ("Road Inner Line Offset", Range(0.02, 0.45)) = 0.18
        _RoadInnerLineWidth ("Road Inner Line Width", Range(0.005, 0.08)) = 0.025
        _RoadAlpha ("Road Alpha", Range(0, 1)) = 0
        _RoadLength ("Road Length", Float) = 1
        _ClosedLoop ("Closed Loop", Float) = 0
        _FollowerOffset ("Follower Offset", Float) = 0
        _RevealProgress ("Reveal Progress", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        LOD 100
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

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

            fixed4 _FollowerColor;
            sampler2D _LineShapeTex;
            float4 _LineShapeTex_ST;
            float _FollowerAlpha;
            float _FollowerEmission;
            float _FollowerSpacing;
            float _FollowerLength;
            float _FollowerSoftness;
            float _LineWidth;
            float _LineSoftness;
            fixed4 _RoadColor;
            fixed4 _RoadHighlightColor;
            fixed4 _RoadEdgeColor;
            fixed4 _RoadInnerLineColor;
            float _RoadEdgeWidth;
            float _RoadInnerLineOffset;
            float _RoadInnerLineWidth;
            float _RoadAlpha;
            float _RoadLength;
            float _ClosedLoop;
            float _FollowerOffset;
            float _RevealProgress;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float GetAxisMask(float value, float softness)
            {
                float lower = smoothstep(0, softness, value);
                float upper = 1 - smoothstep(1 - softness, 1, value);
                return saturate(lower * upper);
            }

            float GetBandMask(float value, float center, float halfWidth, float softness)
            {
                float distanceFromCenter = abs(value - center);
                return 1 - smoothstep(halfWidth, halfWidth + softness, distanceFromCenter);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float progress = i.uv.y / max(_RoadLength, 0.0001);
                float revealMask = step(progress, _RevealProgress);
                float roadX = saturate(i.uv.x);
                float roadBodyMask = GetAxisMask(roadX, 0.01) * revealMask;
                float edgeMask = saturate(GetBandMask(roadX, 0, _RoadEdgeWidth, 0.025) + GetBandMask(roadX, 1, _RoadEdgeWidth, 0.025));
                float innerLeft = GetBandMask(roadX, _RoadInnerLineOffset, _RoadInnerLineWidth, 0.012);
                float innerRight = GetBandMask(roadX, 1 - _RoadInnerLineOffset, _RoadInnerLineWidth, 0.012);
                float innerLineMask = saturate(innerLeft + innerRight);
                fixed3 roadColor = lerp(_RoadColor.rgb, _RoadHighlightColor.rgb, saturate(1 - abs(roadX - 0.5) * 2) * 0.18);
                roadColor = lerp(roadColor, _RoadInnerLineColor.rgb, innerLineMask * 0.85);
                roadColor = lerp(roadColor, _RoadEdgeColor.rgb, edgeMask);

                float shapeX = (i.uv.x - 0.5) / max(_LineWidth, 0.0001) + 0.5;
                float spacing = max(_FollowerSpacing, 0.01);
                float repeatCount = max(1, round(max(_RoadLength, spacing) / spacing));
                float actualSpacing = lerp(spacing, max(_RoadLength / repeatCount, 0.01), step(0.5, _ClosedLoop));
                float repeatedY = frac((i.uv.y - _FollowerOffset) / actualSpacing);
                float bodyLength = saturate(_FollowerLength);
                float inset = (1 - bodyLength) * 0.5;
                float localY = (repeatedY - inset) / max(bodyLength, 0.0001);
                float shapeY = saturate(localY);
                float2 shapeUv = TRANSFORM_TEX(float2(shapeX, shapeY), _LineShapeTex);
                fixed4 shapeSample = tex2D(_LineShapeTex, shapeUv);
                float shapeMask = max(shapeSample.a, max(shapeSample.r, max(shapeSample.g, shapeSample.b)));
                float boundsMask = GetAxisMask(shapeX, _LineSoftness);
                float cellMask = step(inset, repeatedY) * step(repeatedY, inset + bodyLength);
                float edgeFade = GetAxisMask(shapeY, _FollowerSoftness);
                float alpha = shapeMask * boundsMask * cellMask * edgeFade * saturate(_FollowerAlpha) * revealMask;
                fixed3 color = _FollowerColor.rgb * _FollowerEmission;
                fixed3 finalColor = lerp(roadColor, color, saturate(alpha));
                float finalAlpha = saturate(roadBodyMask * _RoadAlpha + alpha);
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
