﻿Shader "Floating Origin Studios/Standard with Planar Map" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)

		// uv maps
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}		
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_Glossiness("Smoothness", Range(0,1)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

		_OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

		_EmissionMap("Emission", 2D) = "white" {}
		_EmissionColor("Emissive Color", Color) = (0,0,0)


			// planar maps
		_PlanarMask("Planar Mask", 2D) = "white" {}
		_PlanarMainTex("Planar Albedo (RGB), Planar Mask (A)", 2D) = "white" {}
		_PlanarBumpMap("Planar Bumpmap", 2D) = "bump" {}
		_PlanarMetallicGlossMap("Planar Metallic", 2D) = "white" {}

		_PlanarGlossiness("Planar Smoothness", Range(0,1)) = 0.5
		[Gamma] _PlanarMetallic("Planar Metallic", Range(0.0, 1.0)) = 0.0

		[Toggle(COMBINE_NORMALS)] _CombineNormals("Combine Normals", Int) = 0

		_ZWrite("ZWrite", Int) = 1
		_ZTest("ZTest", Int) = 4 //LEQual
		_Cull("Culling", Int) = 2 //Back
		_StencilPass("Stencil Pass Op", Int) = 0
	}



	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Cull[_Cull]
		ZTest[_ZTest]
		ZWrite[_ZWrite]

		Stencil
		{
			Ref 128
			Comp Always
			Pass[_StencilPass]
		}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard vertex:vert fullforwardshadows 

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		#pragma shader_feature COMBINE_NORMALS

		struct Input 
		{
		   float2 uv_MainTex : TEXCOORD;
		   float2 uv_BumpMap : TEXCOORD;
		   float3 vpos;
		};

		sampler2D _MainTex;
		sampler2D _BumpMap;

		sampler2D _MetallicGlossMap;
		float4 _MetallicGlossMap_ST;
		
		sampler2D _PlanarMask;
		float4 _PlanarMask_ST;

		sampler2D _PlanarMainTex;
		float4 _PlanarMainTex_ST;

		sampler2D _PlanarBumpMap;
		float4 _PlanarBumpMap_ST;

		sampler2D _PlanarMetallicGlossMap;
		float4 _PlanarMetallicGlossMap_ST;

		half _OcclusionStrength;
		sampler2D _OcclusionMap;
		float4 _OcclusionMap_ST;

		half4 _EmissionColor;
		sampler2D _EmissionMap;
		float4 _EmissionMap_ST;


		half _Glossiness;
		half _Metallic;


		half _PlanarGlossiness;
		half _PlanarMetallic;

		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		float2 GetPlanarMapping(float3 vpos, float4 mapST)
		{
			return float2(vpos.x * mapST.x + mapST.z, vpos.z * mapST.y + mapST.w);
		}
		float2 GetUVMapping(float2 uv, float4 mapST)
		{
			return float2(uv.x * mapST.x + mapST.z, uv.y * mapST.y + mapST.w);
		}
		
		
			
		void vert(inout appdata_full v, out Input data) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.vpos = v.vertex.xyz;
		}


		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			// sample uv maps
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			half3 nrm = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			half4 met = tex2D(_MetallicGlossMap, GetUVMapping(IN.uv_MainTex, _MetallicGlossMap_ST));

			half4 m = tex2D(_PlanarMask, GetUVMapping(IN.uv_MainTex, _PlanarMask_ST));
			half mask = (m.r + m.g + m.b) * 0.3334 * m.a;

			// sample planar maps
			fixed4 cPl = tex2D(_PlanarMainTex, GetPlanarMapping(IN.vpos, _PlanarMainTex_ST)) * _Color;
			half3 nrmPl = UnpackNormal(tex2D(_PlanarBumpMap, GetPlanarMapping(IN.vpos, _PlanarBumpMap_ST)));
			half4 metPl = tex2D(_PlanarMetallicGlossMap, GetPlanarMapping(IN.vpos, _PlanarMetallicGlossMap_ST));



			// lerp using planar mask
			o.Albedo = lerp(c, cPl, mask).rgb;

#if COMBINE_NORMALS
			o.Normal = BlendNormals(nrm, nrmPl);
#else
			o.Normal = lerp(nrm, nrmPl, mask);
#endif

			half4 ms = lerp(met, metPl, mask);
			half metallic = lerp(_Metallic, _PlanarMetallic, mask);
			half glossy = lerp(_Glossiness, _PlanarGlossiness, mask);

			o.Metallic = metallic * (ms.r + ms.g + ms.b);
			o.Smoothness = glossy * ms.a;

			o.Alpha = c.a;
			o.Emission = tex2D(_EmissionMap, GetUVMapping(IN.uv_MainTex, _EmissionMap_ST)).rgb * _EmissionColor.rgb * _EmissionColor.a;
			o.Occlusion = tex2D(_OcclusionMap, GetUVMapping(IN.uv_MainTex, _OcclusionMap_ST)) * ( _OcclusionStrength);
		}

		


		ENDCG
	}
	FallBack "Diffuse"
}
