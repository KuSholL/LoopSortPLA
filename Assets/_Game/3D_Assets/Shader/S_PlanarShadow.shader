Shader "Custom/PlanarShadowInstance"
{
    Properties
    {
        _FakeShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _FakeShadowDir ("Shadow Direction (XYZ)", Vector) = (0, -1, 0, 0)
        _FakeShadowHeight ("Shadow Ground Height", Float) = 0.0
        _FakeShadowSizeX ("Shadow Size X", Range(0.1, 3.0)) = 1.0
        _FakeShadowSizeZ ("Shadow Size Z", Range(0.1, 3.0)) = 1.0

        [Header(Stencil Settings)]
        _StencilRef ("Stencil Ref", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "PlanarShadow"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ZTest LEqual

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Định nghĩa chính xác buffer dữ liệu Instancing bằng macro chuẩn của Unity
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FakeShadowColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FakeShadowDir)
                UNITY_DEFINE_INSTANCED_PROP(float, _FakeShadowHeight)
                UNITY_DEFINE_INSTANCED_PROP(float, _FakeShadowSizeX)
                UNITY_DEFINE_INSTANCED_PROP(float, _FakeShadowSizeZ)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // 1. Lấy vị trí gốc của đối tượng trong World Space
                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

                // 2. Chuyển đổi vertex sang World Space
                float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
                
                // Áp dụng scale X và Z từ Instancing Buffer
                float sizeX = UNITY_ACCESS_INSTANCED_PROP(Props, _FakeShadowSizeX);
                float sizeZ = UNITY_ACCESS_INSTANCED_PROP(Props, _FakeShadowSizeZ);
                posWorld.x = worldOrigin.x + (posWorld.x - worldOrigin.x) * sizeX;
                posWorld.z = worldOrigin.z + (posWorld.z - worldOrigin.z) * sizeZ;

                // 3. Chuẩn hóa hướng ánh sáng
                float3 L = UNITY_ACCESS_INSTANCED_PROP(Props, _FakeShadowDir).xyz;
                if (length(L) < 0.001)
                {
                    L = float3(0.0, -1.0, 0.0);
                }
                else
                {
                    L = normalize(L);
                }

                L.y = min(L.y, -0.01); 

                // 4. Chiếu lên mặt phẳng bóng
                float shadowHeight = UNITY_ACCESS_INSTANCED_PROP(Props, _FakeShadowHeight);
                float t = (shadowHeight - posWorld.y) / L.y;
                posWorld.xyz = posWorld.xyz + L * t;

                // Tránh Z-fighting
                posWorld.y = shadowHeight + 0.001;

                // 5. Chuyển sang Clip Space
                o.pos = mul(UNITY_MATRIX_VP, float4(posWorld.xyz, 1.0));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return UNITY_ACCESS_INSTANCED_PROP(Props, _FakeShadowColor);
            }
            ENDCG
        }
    }
}