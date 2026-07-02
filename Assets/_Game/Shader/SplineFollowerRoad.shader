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

            fixed4 frag(v2f i) : SV_Target
            {
                float progress = i.uv.y / max(_RoadLength, 0.0001);
                float revealMask = step(progress, _RevealProgress);
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
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
