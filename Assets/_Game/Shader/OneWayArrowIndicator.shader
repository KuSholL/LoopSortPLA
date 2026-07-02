Shader "Custom/OneWayArrowIndicator"
{
    Properties
    {
        _Color ("Indicator Color", Color) = (1, 1, 1, 1)
        _Emission ("Emission Intensity", Range(0, 10)) = 2.0
        _Speed ("Animation Speed", Float) = 1.0
        _FadeWidth ("Fade Duration", Range(0.05, 0.5)) = 0.2
        _Spacing ("Arrow Spacing", Range(0.05, 0.3)) = 0.2
        _Interval ("Interval Delay (Seconds)", Range(0, 10)) = 1.0
        _ArrowCount ("Arrow Count", Range(1, 12)) = 5
        
        [Header(Procedural Settings)]
        [Toggle] _UseTexture ("Use Texture", Float) = 0
        _MainTex ("Arrow Texture (Alpha Mask)", 2D) = "white" {}
        
        _ArrowHeight ("Arrow Height (Slot Size)", Range(0.05, 0.8)) = 0.12
        _ArrowWidth ("Arrow Head Width", Range(0.05, 0.8)) = 0.4
        _ArrowShaftWidth ("Arrow Shaft Width", Range(0.01, 0.4)) = 0.12
        _ArrowHeadSplit ("Arrow Head Split Point", Range(0.1, 0.9)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
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

            fixed4 _Color;
            float _Emission;
            float _Speed;
            float _FadeWidth;
            float _Spacing;
            float _Interval;
            float _ArrowCount;
            
            float _UseTexture;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _ArrowHeight;
            float _ArrowWidth;
            float _ArrowShaftWidth;
            float _ArrowHeadSplit;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Time-based animation loop with interval delay
                float activeDuration = 1.0 / max(_Speed, 0.0001);
                float totalDuration = activeDuration + _Interval;
                float modTime = fmod(_Time.y, totalDuration);
                
                float t = 0.0;
                float isShowing = 0.0;
                
                if (modTime < activeDuration)
                {
                    t = modTime / activeDuration;
                    isShowing = 1.0;
                }
                
                float finalAlpha = 0;
                int arrowCount = clamp((int)_ArrowCount, 1, 12);
                float arrowHeight = _ArrowHeight;
                
                for (int idx = 0; idx < 12; idx++)
                {
                    if (idx >= arrowCount) break;
                    
                    // Center position of arrow idx along y-axis (V coordinate) using spacing
                    float posY = 0.5 + (idx - (arrowCount - 1) * 0.5) * _Spacing;
                    
                    // Half height boundaries for this arrow slot
                    float halfH = arrowHeight * 0.5;
                    float minY = posY - halfH;
                    float maxY = posY + halfH;
                    
                    if (uv.y >= minY && uv.y <= maxY)
                    {
                        // Normalize local Y to [0, 1] inside this arrow slot
                        float localY = (uv.y - minY) / arrowHeight;
                        
                        // Centered and scaled local X to [0, 1]
                        float localX = (uv.x - 0.5) / max(_ArrowWidth, 0.0001) + 0.5;
                        
                        float arrowShape = 0;
                        
                        // Clip rendering outside [0, 1] bounds of the scaled slot
                        if (localX >= 0.0 && localX <= 1.0 && localY >= 0.0 && localY <= 1.0)
                        {
                            if (_UseTexture > 0.5)
                            {
                                float2 sampledUv = float2(localX, localY) * _MainTex_ST.xy + _MainTex_ST.zw;
                                fixed4 texColor = tex2D(_MainTex, sampledUv);
                                arrowShape = texColor.a * max(texColor.r, max(texColor.g, texColor.b));
                            }
                            else
                            {
                                // Procedural arrow pointing in +Y direction
                                float dx = localX - 0.5;
                                
                                // Head triangle (localY from _ArrowHeadSplit to 0.9)
                                float headStart = _ArrowHeadSplit;
                                float headEnd = 0.9;
                                float headHalfWidth = 0.5; // normalized local width is [0, 1], so half width is 0.5
                                float head = step(headStart, localY) * step(localY, headEnd) * step(abs(dx), (headEnd - localY) / (headEnd - headStart) * headHalfWidth);
                                
                                // Shaft rectangle (localY from 0.1 to _ArrowHeadSplit)
                                float shaftWidth = _ArrowShaftWidth * 0.5;
                                float shaftStart = 0.1;
                                float shaftEnd = headStart;
                                float shaft = step(shaftStart, localY) * step(localY, shaftEnd) * step(abs(dx), shaftWidth);
                                
                                arrowShape = saturate(head + shaft);
                            }
                        }
                        
                        // Sequential fade calculation (exactly once per loop, no wrapping)
                        float startT = _FadeWidth;
                        float endT = 1.0 - _FadeWidth;
                        if (startT >= endT)
                        {
                            startT = 0.2;
                            endT = 0.8;
                        }
                        float tPeak = 0.5;
                        if (arrowCount > 1)
                        {
                            tPeak = startT + (idx / (arrowCount - 1.0)) * (endT - startT);
                        }
                        float dt = t - tPeak;
                        
                        // Compute fade intensity based on distance from peak time
                        float intensity = saturate(1.0 - abs(dt) / _FadeWidth);
                        intensity = smoothstep(0, 1, intensity); // Ease in/out transition
                        
                        finalAlpha += arrowShape * intensity * isShowing;
                    }
                }
                
                finalAlpha = saturate(finalAlpha) * _Color.a;
                fixed3 finalColor = _Color.rgb * _Emission;
                
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
