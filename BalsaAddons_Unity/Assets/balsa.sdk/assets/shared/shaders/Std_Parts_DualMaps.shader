Shader "Floating Origin Studios/Standard (Parts, Dual Maps)" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)

		// uv maps
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_MainTex2("Albedo 2 (RGB)", 2D) = "white" {}
		_AlbedoBlend("Albedo Blend", Color) = (0,0,0,0)

		_BumpMap("Bumpmap", 2D) = "bump" {}		
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

		_OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

		_EmissionMap("Emission", 2D) = "white" {}
		[HDR]_EmissionColor("Emissive Color", Color) = (0,0,0)

		_EmissionMap2("Emission 2", 2D) = "white" {}
		[HDR]_EmissionColor2("Emissive Color 2", Color) = (0,0,0)

		_FresnelColor("Fresnel Color", Color) = (0,0,0)
		_FresnelPower("Fresnel Power", Float) = 1.0

		_ZWrite("ZWrite", Int) = 1
		_ZTest("ZTest", Int) = 4 //LEQual
		_Cull("Culling", Int) = 2 //Back
		_StencilPass("Stencil Pass Op", Int) = 0

	}
	SubShader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 200

		Cull[_Cull]
		ZTest[_ZTest]
		ZWrite[_ZWrite]

		//GrabPass { "_GrabTex" }

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

		#include "Dithering.cginc"
		#include "BalsaStd.cginc"


		struct Input 
		{
		   float2 uv_MainTex : TEXCOORD;
		   float2 uv_BumpMap : TEXCOORD;
		   float4 screenPos;
		   float3 viewDir;
		};

		sampler2D _MainTex;
		sampler2D _MainTex2;
		float4 _MainTex2_ST;

		float4 _AlbedoBlend;

		sampler2D _BumpMap;

		sampler2D _MetallicGlossMap;
		float4 _MetallicGlossMap_ST;
		

		half _OcclusionStrength;
		sampler2D _OcclusionMap;
		float4 _OcclusionMap_ST;

		half4 _EmissionColor;
		half4 _EmissionColor2;
		sampler2D _EmissionMap;
		sampler2D _EmissionMap2;
		float4 _EmissionMap_ST;
		float4 _EmissionMap2_ST;

		float4 _FresnelColor;
		float _FresnelPower;

		half _Glossiness;
		half _Metallic;

		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)




		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			// sample uv maps
			fixed4 c = lerp(tex2D (_MainTex, IN.uv_MainTex) * _Color
							, tex2D(_MainTex2, GetUVMapping(IN.uv_MainTex, _MainTex2_ST)) * _Color
							, _AlbedoBlend.a);

			half3 nrm = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			half4 met = tex2D(_MetallicGlossMap, GetUVMapping(IN.uv_MainTex, _MetallicGlossMap_ST));


			o.Albedo = c.rgb * _Color.a;
			o.Normal = nrm;
				
			o.Metallic = _Metallic * (met.r + met.g + met.b);
			o.Smoothness = _Glossiness * met.a;

			o.Alpha = c.a;

			half fresnel = 1.0 - saturate(dot(IN.viewDir, o.Normal));
			float3 fresnelColor = _FresnelColor.rgb * pow(fresnel, _FresnelPower);
			o.Emission = saturate(tex2D(_EmissionMap, GetUVMapping(IN.uv_MainTex, _EmissionMap_ST)).rgb * _EmissionColor.rgb * _EmissionColor.a
									+ tex2D(_EmissionMap2, GetUVMapping(IN.uv_MainTex, _EmissionMap2_ST)).rgb * _EmissionColor2.rgb * _EmissionColor2.a)
										+ fresnelColor;

			o.Occlusion = tex2D(_OcclusionMap, GetUVMapping(IN.uv_MainTex, _OcclusionMap_ST)) * ( _OcclusionStrength);
			
			ditherClip(IN.screenPos.xy / IN.screenPos.w, c.a);
		}

		


		ENDCG
	}
	FallBack "Diffuse"
}
