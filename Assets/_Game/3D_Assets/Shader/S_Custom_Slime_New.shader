// Toony Colors Pro+Mobile 2
// (c) 2014-2023 Jean Moreno

Shader "Custom_Cube1_OutlineToggle"
{
	Properties
	{
		[Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
		[TCP2ToggleNoKeyword] _ZWrite ("Depth Write", Float) = 1.0
		[HideInInspector] _RenderingMode ("rendering mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("blending source", Float) = 1.0
		[HideInInspector] _DstBlend ("blending destination", Float) = 0.0
		[TCP2Separator]

		[TCP2HeaderHelp(Base)]
		_Color ("Color", Color) = (1,1,1,1)
		[TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
		[TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
		[MainTexture] _MainTex ("Albedo", 2D) = "white" {}
		[TCP2Separator]

		[TCP2Header(Ramp Shading)]
		_RampThreshold ("Threshold", Range(0.01,1)) = 0.5
		_RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
		[IntRange] _BandsCount ("Bands Count", Range(1,20)) = 4
		_BandsSmoothing ("Bands Smoothing", Range(0.001,1)) = 0.1
		[TCP2Separator]
		
		[TCP2HeaderHelp(Specular)]
		[Toggle(TCP2_SPECULAR)] _UseSpecular ("Enable Specular", Float) = 0
		[TCP2ColorNoAlpha] _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
		_SpecularIntensity ("Specular Intensity", Range(0, 5)) = 1.0
		_SpecularToonSize ("Toon Size", Range(0,1)) = 0.25
		_SpecularToonSmoothness ("Toon Smoothness", Range(0.001,0.5)) = 0.05
		[TCP2Separator]
		
		[TCP2HeaderHelp(Rim Lighting)]
		[Toggle(TCP2_RIM_LIGHTING)] _UseRim ("Enable Rim Lighting", Float) = 0
		[TCP2ColorNoAlpha] _RimColor ("Rim Color", Color) = (0.8,0.8,0.8,0.5)
		_RimMin ("Rim Min", Range(0,2)) = 0.5
		_RimMax ("Rim Max", Range(0,2)) = 1
		[TCP2Separator]
		
		[TCP2HeaderHelp(MatCap)]
		[Toggle(TCP2_MATCAP)] _UseMatCap ("Enable MatCap", Float) = 0
		[NoScaleOffset] _MatCapTex ("MatCap (RGB)", 2D) = "gray" {}
		[TCP2ColorNoAlpha] _MatCapColor ("MatCap Color", Color) = (1,1,1,1)
		[TCP2Separator]

		[TCP2HeaderHelp(HDRI Reflection)]
		[Toggle] _UseHDRI ("Enable HDRI Reflection", Float) = 1
		[NoScaleOffset] _Cube ("HDRI Cubemap", Cube) = "_Skybox" {}
		_ReflectColor ("Reflection Tint", Color) = (1,1,1,1)
		_ReflectIntensity ("Reflection Intensity", Range(0, 5)) = 0.5
		_Roughness ("Reflection Roughness (Blur)", Range(0, 1)) = 0.0
		_HDRIFresnelPower ("HDRI Fresnel Power", Range(0.1, 10)) = 2.0
		[TCP2Separator]
		
		[TCP2HeaderHelp(Outline)]
		[TCP2ToggleNoKeyword] _UseOutline ("Enable Outline", Float) = 1
		_OutlineWidth ("Width", Range(0.1,4)) = 1
		_OutlineColorVertex ("Color", Color) = (0,0,0,1)

		[TCP2MaterialKeywordEnumNoPrefix(Regular, _, Vertex Colors, TCP2_COLORS_AS_NORMALS, Tangents, TCP2_TANGENT_AS_NORMALS, UV1, TCP2_UV1_AS_NORMALS, UV2, TCP2_UV2_AS_NORMALS, UV3, TCP2_UV3_AS_NORMALS, UV4, TCP2_UV4_AS_NORMALS)]
		_NormalsSource ("Outline Normals Source", Float) = 0

		[TCP2MaterialKeywordEnumNoPrefix(Full XYZ, TCP2_UV_NORMALS_FULL, Compressed XY, _, Compressed ZW, TCP2_UV_NORMALS_ZW)]
		_NormalsUVType ("UV Data Type", Float) = 0
		[TCP2Separator]

		[HideInInspector] __dummy__ ("unused", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}

		CGINCLUDE

		#include "UnityCG.cginc"
		#include "UnityLightingCommon.cginc"

		#define TCP2_TEX2D_WITH_SAMPLER(tex)						UNITY_DECLARE_TEX2D(tex)
		#define TCP2_TEX2D_NO_SAMPLER(tex)							UNITY_DECLARE_TEX2D_NOSAMPLER(tex)
		#define TCP2_TEX2D_SAMPLE(tex, samplertex, coord)			UNITY_SAMPLE_TEX2D_SAMPLER(tex, samplertex, coord)
		#define TCP2_TEX2D_SAMPLE_LOD(tex, samplertex, coord, lod)	UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex, samplertex, coord, lod)

		TCP2_TEX2D_WITH_SAMPLER(_MainTex);
		TCP2_TEX2D_WITH_SAMPLER(_MatCapTex);

		float _UseOutline;
		float _OutlineWidth;
		fixed4 _OutlineColorVertex;

		float4 _MainTex_ST;
		fixed4 _Color;
		fixed4 _MatCapColor;

		float _RampThreshold;
		float _RampSmoothing;
		float _BandsCount;
		float _BandsSmoothing;
		fixed4 _SColor;
		fixed4 _HColor;

		float _SpecularToonSize;
		float _SpecularToonSmoothness;
		fixed4 _SpecularColor;
		float _SpecularIntensity;

		float _RimMin;
		float _RimMax;
		fixed4 _RimColor;

		float _UseHDRI;
		samplerCUBE _Cube;
		fixed4 _ReflectColor;
		float _ReflectIntensity;
		float _Roughness;
		float _HDRIFresnelPower;

		inline float3 SpecSafeNormalize(float3 inVec)
		{
			half dp3 = max(0.001f, dot(inVec, inVec));
			return inVec * rsqrt(dp3);
		}

		ENDCG

		// ================================================================
		// OUTLINE INCLUDE
		// ================================================================
		CGINCLUDE

		struct appdata_outline
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;

			#if TCP2_UV1_AS_NORMALS
				float4 texcoord0 : TEXCOORD0;
			#elif TCP2_UV2_AS_NORMALS
				float4 texcoord1 : TEXCOORD1;
			#elif TCP2_UV3_AS_NORMALS
				float4 texcoord2 : TEXCOORD2;
			#elif TCP2_UV4_AS_NORMALS
				float4 texcoord3 : TEXCOORD3;
			#endif

			#if TCP2_COLORS_AS_NORMALS
				float4 vertexColor : COLOR;
			#endif

			#if TCP2_TANGENT_AS_NORMALS
				float4 tangent : TANGENT;
			#endif

			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f_outline
		{
			float4 vertex : SV_POSITION;
			float4 vcolor : TEXCOORD0;
			float3 pack1 : TEXCOORD1;
			UNITY_VERTEX_OUTPUT_STEREO
		};

		v2f_outline vertex_outline (appdata_outline v)
		{
			v2f_outline output;
			UNITY_INITIALIZE_OUTPUT(v2f_outline, output);
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			float __outlineWidth = _OutlineWidth * _UseOutline;
			float4 __outlineColorVertex = _OutlineColorVertex.rgba;

			float3 worldNormalUv = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
			output.pack1.xyz = worldNormalUv;

			#ifdef TCP2_COLORS_AS_NORMALS
				float3 normal = (v.vertexColor.xyz * 2) - 1;
			#elif TCP2_TANGENT_AS_NORMALS
				float3 normal = v.tangent.xyz;
			#elif TCP2_UV1_AS_NORMALS || TCP2_UV2_AS_NORMALS || TCP2_UV3_AS_NORMALS || TCP2_UV4_AS_NORMALS

				#if TCP2_UV1_AS_NORMALS
					#define uvChannel texcoord0
				#elif TCP2_UV2_AS_NORMALS
					#define uvChannel texcoord1
				#elif TCP2_UV3_AS_NORMALS
					#define uvChannel texcoord2
				#elif TCP2_UV4_AS_NORMALS
					#define uvChannel texcoord3
				#endif

				#if TCP2_UV_NORMALS_FULL
					float3 normal = v.uvChannel.xyz;
				#else
					#if TCP2_UV_NORMALS_ZW
						#define ch1 z
						#define ch2 w
					#else
						#define ch1 x
						#define ch2 y
					#endif

					float3 n;
					v.uvChannel.ch1 = v.uvChannel.ch1 * 255.0 / 16.0;
					n.x = floor(v.uvChannel.ch1) / 15.0;
					n.y = frac(v.uvChannel.ch1) * 16.0 / 15.0;
					n.z = v.uvChannel.ch2;
					n = n * 2 - 1;
					float3 normal = n;
				#endif

			#else
				float3 normal = v.normal;
			#endif

			float size = 1;

			#if !defined(SHADOWCASTER_PASS)
				output.vertex = UnityObjectToClipPos(v.vertex.xyz);

				normal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;
				float2 clipNormals = normalize(mul(UNITY_MATRIX_VP, float4(normal, 0)).xy);

				half2 screenRatio = half2(1.0, _ScreenParams.x / _ScreenParams.y);
				half2 outlineWidth = (__outlineWidth / 100) * screenRatio;

				output.vertex.xy += clipNormals.xy * outlineWidth;
			#else
				v.vertex = v.vertex + float4(normal, 0) * __outlineWidth * size * 0.01;
				output.vertex = UnityObjectToClipPos(v.vertex.xyz);
			#endif

			output.vcolor.xyzw = __outlineColorVertex;

			return output;
		}

		float4 fragment_outline (v2f_outline input) : SV_Target
		{
			if (_UseOutline < 0.5)
				discard;

			half4 outlineColor = input.vcolor.xyzw;
			return outlineColor;
		}

		ENDCG

		// ================================================================
		// MAIN SURFACE SHADER
		// ================================================================

		Blend [_SrcBlend] [_DstBlend]
		Cull [_Cull]
		ZWrite [_ZWrite]

		CGPROGRAM

		#pragma surface surf ToonyColorsCustom vertex:vertex_surface exclude_path:deferred exclude_path:prepass keepalpha nolightmap nofog nolppv keepalpha
		#pragma target 3.0

		#pragma shader_feature_local_fragment TCP2_SPECULAR
		#pragma shader_feature_local_vertex TCP2_VERTEX_DISPLACEMENT
		#pragma shader_feature_local_fragment TCP2_RIM_LIGHTING
		#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
		#pragma shader_feature_local TCP2_MATCAP

		struct appdata_tcp2
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord0 : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;

			#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
				half4 tangent : TANGENT;
			#endif

			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Input
		{
			half3 viewDir;
			half3 worldNormal; INTERNAL_DATA
			half2 matcap;
			float2 texcoord0;
			float3 worldRefl;
		};

		struct SurfaceOutputCustom
		{
			half atten;
			half3 Albedo;
			half3 Normal;
			half3 worldNormal;
			half3 Emission;
			half Specular;
			half Gloss;
			half Alpha;
			half ndv;
			half ndvRaw;

			Input input;

			float __rampThreshold;
			float __rampSmoothing;
			float __bandsCount;
			float __bandsSmoothing;
			float3 __shadowColor;
			float3 __highlightColor;
			float __ambientIntensity;
			float __specularToonSize;
			float __specularToonSmoothness;
			float3 __specularColor;
			float __rimMin;
			float __rimMax;
			float3 __rimColor;
			float __rimStrength;
		};

		void vertex_surface(inout appdata_tcp2 v, out Input output)
		{
			UNITY_INITIALIZE_OUTPUT(Input, output);

			output.texcoord0.xy = v.texcoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

			float4 clipPos = UnityObjectToClipPos(v.vertex);
			float4 screenPos = ComputeScreenPos(clipPos);

			#if defined(TCP2_MATCAP)
				float3 worldNorm = normalize(
					unity_WorldToObject[0].xyz * v.normal.x +
					unity_WorldToObject[1].xyz * v.normal.y +
					unity_WorldToObject[2].xyz * v.normal.z
				);

				worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);

				float3 perspectiveOffset = (screenPos.xyz / screenPos.w) - 0.5;
				worldNorm.xy -= (perspectiveOffset.xy * perspectiveOffset.z) * 0.5;

				output.matcap = worldNorm.xy * 0.5 + 0.5;
			#endif
		}

		void surf(Input input, inout SurfaceOutputCustom output)
		{
			float4 __albedo = TCP2_TEX2D_SAMPLE(_MainTex, _MainTex, input.texcoord0.xy).rgba;
			float4 __mainColor = _Color.rgba;
			float __alpha = __albedo.a * __mainColor.a;

			float3 __matcapColor = _MatCapColor.rgb;

			output.__rampThreshold = _RampThreshold;
			output.__rampSmoothing = _RampSmoothing;
			output.__bandsCount = _BandsCount;
			output.__bandsSmoothing = _BandsSmoothing;
			output.__shadowColor = _SColor.rgb;
			output.__highlightColor = _HColor.rgb;
			output.__ambientIntensity = 1.0;

			output.__specularToonSize = _SpecularToonSize;
			output.__specularToonSmoothness = _SpecularToonSmoothness;
			output.__specularColor = _SpecularColor.rgb;

			output.__rimMin = _RimMin;
			output.__rimMax = _RimMax;
			output.__rimColor = _RimColor.rgb;
			output.__rimStrength = 1.0;

			output.input = input;

			half3 worldNormal = WorldNormalVector(input, output.Normal);
			output.worldNormal = worldNormal;

			half ndv = abs(dot(input.viewDir, normalize(output.Normal.xyz)));
			output.ndv = ndv;
			output.ndvRaw = ndv;

			output.Albedo = __albedo.rgb;
			output.Alpha = __alpha;

			output.Albedo *= __mainColor.rgb;

			#if defined(TCP2_MATCAP)
				half2 capCoord = input.matcap;
				half3 matcap = TCP2_TEX2D_SAMPLE(_MatCapTex, _MatCapTex, capCoord).rgb * __matcapColor;
				output.Emission += matcap;
			#endif

			if (_UseHDRI > 0.5)
			{
				float3 worldRefl = WorldReflectionVector(input, output.Normal);
				half4 reflData = texCUBElod(_Cube, float4(worldRefl, _Roughness * 7.0));

				float fresnelMask = pow(1.0 - saturate(output.ndv), _HDRIFresnelPower);

				half3 finalReflection = reflData.rgb * _ReflectColor.rgb * _ReflectIntensity * fresnelMask;
				output.Emission += finalReflection;
			}
		}

		inline half4 LightingToonyColorsCustom(inout SurfaceOutputCustom surface, half3 viewDir, UnityGI gi)
		{
			half ndv = surface.ndv;
			half3 lightDir = gi.light.dir;

			#if defined(UNITY_PASS_FORWARDBASE)
				half3 lightColor = _LightColor0.rgb;
				half atten = surface.atten;
			#else
				half3 lightColor = _LightColor0.rgb;
				half atten = max(gi.light.color.r, max(gi.light.color.g, gi.light.color.b)) / max(_LightColor0.r, max(_LightColor0.g, _LightColor0.b));
			#endif

			half3 normal = normalize(surface.Normal);
			half ndl = dot(normal, lightDir);
			half3 ramp;

			#define RAMP_THRESHOLD		surface.__rampThreshold
			#define RAMP_SMOOTH			surface.__rampSmoothing
			#define RAMP_BANDS			surface.__bandsCount
			#define RAMP_BANDS_SMOOTH	surface.__bandsSmoothing

			ndl = saturate(ndl);

			half bandsNdl = smoothstep(
				RAMP_THRESHOLD - RAMP_SMOOTH * 0.5,
				RAMP_THRESHOLD + RAMP_SMOOTH * 0.5,
				ndl
			);

			half bandsSmooth = RAMP_BANDS_SMOOTH * 0.5;

			ramp = saturate(
				(
					smoothstep(
						0.5 - bandsSmooth,
						0.5 + bandsSmooth,
						frac(bandsNdl * RAMP_BANDS)
					)
					+ floor(bandsNdl * RAMP_BANDS)
				)
				/ RAMP_BANDS
			).xxx;

			ramp *= atten;

			surface.Albedo = lerp(surface.__shadowColor, surface.Albedo, ramp);
			ramp = lerp(half3(1,1,1), surface.__highlightColor, ramp);

			half4 color;
			color.rgb = surface.Albedo * lightColor.rgb * ramp;
			color.a = surface.Alpha;

			half occlusion = 1;

			#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
				half3 ambient = gi.indirect.diffuse;
				ambient *= surface.Albedo * occlusion * surface.__ambientIntensity;
				color.rgb += ambient;
			#endif

			#if defined(_ALPHAPREMULTIPLY_ON)
				color.rgb *= color.a;
			#endif

			half3 halfDir = SpecSafeNormalize(float3(lightDir) + float3(viewDir));

			#if defined(TCP2_SPECULAR)
				float ndh = max(0, dot(normal, halfDir));

				float spec = smoothstep(
					surface.__specularToonSize + surface.__specularToonSmoothness,
					surface.__specularToonSize - surface.__specularToonSmoothness,
					1 - (ndh / (1 + surface.__specularToonSmoothness))
				);

				spec *= ndl;
				spec *= atten;

				color.rgb += spec * lightColor.rgb * surface.__specularColor * _SpecularIntensity;
			#endif

			#if defined(TCP2_RIM_LIGHTING)
				half rim = 1 - surface.ndvRaw;
				half rimMin = surface.__rimMin;
				half rimMax = surface.__rimMax;

				rim = smoothstep(rimMin, rimMax, rim);

				half3 rimColor = surface.__rimColor;
				half rimStrength = surface.__rimStrength;

				color.rgb += ndl * atten * rim * rimColor * rimStrength;
			#endif

			#if defined(_ALPHABLEND_ON) && defined(UNITY_PASS_FORWARDADD)
				color.rgb *= color.a;
			#endif

			return color;
		}

		void LightingToonyColorsCustom_GI(inout SurfaceOutputCustom surface, UnityGIInput data, inout UnityGI gi)
		{
			half3 normal = surface.Normal;

			gi = UnityGlobalIllumination(data, 1.0, normal);

			surface.atten = data.atten;
			gi.light.color = _LightColor0.rgb;
		}

		ENDCG

		// ================================================================
		// OUTLINE PASS
		// ================================================================
		Pass
		{
			Name "Outline"

			Tags
			{
				"LightMode"="ForwardBase"
			}

			Cull Front

			CGPROGRAM

			#pragma vertex vertex_outline
			#pragma fragment fragment_outline
			#pragma target 3.0

			#pragma multi_compile _ TCP2_COLORS_AS_NORMALS TCP2_TANGENT_AS_NORMALS TCP2_UV1_AS_NORMALS TCP2_UV2_AS_NORMALS TCP2_UV3_AS_NORMALS TCP2_UV4_AS_NORMALS
			#pragma multi_compile _ TCP2_UV_NORMALS_FULL TCP2_UV_NORMALS_ZW
			#pragma multi_compile_instancing
			#pragma shader_feature_local_vertex TCP2_VERTEX_DISPLACEMENT

			ENDCG
		}
	}

	Fallback "Diffuse"
	CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}