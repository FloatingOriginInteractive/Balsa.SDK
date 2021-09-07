Shader "Floating Origin Studios/Standard Parts Paintable" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)

		// uv maps
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}		
		_MetallicGlossMap("Metallic", 2D) = "black" {}

		
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

		_OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

		_EmissionMap("Emission", 2D) = "white" {}
		_EmissionColor("Emissive Color", Color) = (0,0,0)

		_FresnelColor("Fresnel Color", Color) = (0,0,0)
		_FresnelPower("Fresnel Power", Float) = 1.0


		_ZWrite("ZWrite", Int) = 1
		_ZTest("ZTest", Int) = 4 //LEQual
		_Cull("Culling", Int) = 2 //Back
		_StencilPass("Stencil Pass Op", Int) = 0

		[Space(20)]
		[Header(Decals)]
		[Space]
		_DecalMap("Decal Map (RGBA | for testing. Leave empty for export) ", 2D) = "black" {}
		[HideInInspector]_DirtMap("Dirt (RGBA)", 2D) = "black" {}
		_DecalAlphaBoost("Decal Blend Boost", Float) = 1.5
		_DecalAlphaBoostMax("Decal Blend Boost Max", Float) = 1.0
		[KeywordEnum(Normal, Multiply, Overlay, ColorBurn)] _DecalBlend("Decal Blending Mode", Int) = 0		
		[Toggle(UV2_DECALS)] _UV2ForDecals("Use UV2 for decals and dirt", Float) = 0
		[Toggle(USE_FRESNEL)] _UseFresnel("Enable Fresnel Colour", Float) = 0
		//[Toggle(USE_ALPHA)] _UseAlpha("Enable Transparency", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 200

		Cull[_Cull]
		ZTest[_ZTest]
		ZWrite[_ZWrite]
			
		//Blend One OneMinusSrcAlpha

		Stencil
		{
			Ref 128
			Comp Always
			Pass[_StencilPass]
		}



		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows keepalpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#pragma shader_feature UV2_DECALS  
		#pragma shader_feature USE_FRESNEL  
		#pragma multi_compile _DECALBLEND_NORMAL _DECALBLEND_MULTIPLY _DECALBLEND_OVERLAY _DECALBLEND_COLORBURN 
		
		#include "Dithering.cginc"
		#include "BalsaStd.cginc"
		#include "BalsaStd_Decals.cginc"

		struct Input
		{
			float2 uv_MainTex : TEXCOORD;
			float2 uv_BumpMap : TEXCOORD;
			float4 screenPos;
			float3 viewDir;
#if UV2_DECALS
			float2 uv_2 : TEXCOORD1;
#endif
		};

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _DecalMap;
		sampler2D _DirtMap;

		sampler2D _MetallicGlossMap;
		float4 _MetallicGlossMap_ST;


		half _OcclusionStrength;
		sampler2D _OcclusionMap;
		float4 _OcclusionMap_ST;

		half4 _EmissionColor;
		sampler2D _EmissionMap;
		float4 _EmissionMap_ST;


		half _Glossiness;
		half _Metallic;

		fixed4 _Color;
		float _DecalAlphaBoost;
		float _DecalAlphaBoostMax;

		float4 _FresnelColor;
		float _FresnelPower;




		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)




		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// sample uv maps

			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color ;

			half3 nrm = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			half4 met = tex2D(_MetallicGlossMap, GetUVMapping(IN.uv_MainTex, _MetallicGlossMap_ST));

#if UV2_DECALS
			fixed4 decals = tex2D(_DecalMap, IN.uv_2);
			fixed4 dirt = tex2D(_DirtMap, IN.uv_2);
#else
			fixed4 decals = tex2D(_DecalMap, IN.uv_MainTex);
			fixed4 dirt = tex2D(_DirtMap, IN.uv_MainTex);
#endif

			o.Albedo = lerp(BalsaStd_DecalBlending(decals, c, _DecalAlphaBoost, _DecalAlphaBoostMax).rgb, dirt.rgb, dirt.a) * _Color.a;
			o.Normal = nrm;

			o.Metallic = lerp(_Metallic, met.rgb, (met.r * met.g * met.b));
			o.Smoothness = lerp(_Glossiness, _Glossiness + met.a, (met.r * met.g * met.b));
			o.Occlusion = tex2D(_OcclusionMap, GetUVMapping(IN.uv_MainTex, _OcclusionMap_ST)) * (_OcclusionStrength);


			o.Alpha = c.a;

			half fresnel = 1.0 - saturate(dot(IN.viewDir, o.Normal));
			float3 fresnelColor = _FresnelColor.rgb * pow(fresnel, _FresnelPower);
			o.Emission = tex2D(_EmissionMap, GetUVMapping(IN.uv_MainTex, _EmissionMap_ST)).rgb * _EmissionColor.rgb * _EmissionColor.a + fresnelColor;

			ditherClip(IN.screenPos.xy / IN.screenPos.w, c.a);

		}




		ENDCG
	}
	FallBack "Diffuse"
		
}
