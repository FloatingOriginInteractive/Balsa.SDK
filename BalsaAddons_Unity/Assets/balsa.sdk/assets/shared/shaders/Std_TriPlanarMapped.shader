Shader "Floating Origin Studios/Standard with Triplanar Map" {
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
		

		_FresnelColor("Fresnel Color", Color) = (0,0,0)
		_FresnelPower("Fresnel Power", Float) = 1.0


			// planar maps
		_PlanarMask("Planar Mask", 2D) = "white" {}
		_PlanarMainTex("Planar Albedo (RGB), Planar Mask (A)", 2D) = "white" {}
		_PlanarBumpMap("Planar Bumpmap", 2D) = "bump" {}
		_PlanarMetallicGlossMap("Planar Metallic", 2D) = "white" {}

		_PlanarGlossiness("Planar Smoothness", Range(0,1)) = 0.5
		[Gamma] _PlanarMetallic("Planar Metallic", Range(0.0, 1.0)) = 0.0

		[Toggle(COMBINE_NORMALS)] _CombineNormals("Combine Normals", Int) = 0

		[Toggle(SWAPX)] _SwapX("Swap X Plane coords", Int) = 0
		[Toggle(SWAPY)] _SwapY("Swap Y Plane coords", Int) = 0
		[Toggle(SWAPZ)] _SwapZ("Swap Z Plane coords", Int) = 0

		X_ST("X Plane Scale/Offset", Vector) = (1,1,0,0)
		Y_ST("Y Plane Scale/Offset", Vector) = (1,1,0,0)
		Z_ST("Z Plane Scale/Offset", Vector) = (1,1,0,0)


		_PlanarBlendPower("Planar Blend Power", Float) = 1.0
		_PlanarBlendShift("Planar Blend Shift", Float) = 1.9
		_PlanarColorFloor("Planar Color Floor", Color) = (0,0,0)

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
			Pass [_StencilPass]
		}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard vertex:vert fullforwardshadows 

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 4.0
		#pragma shader_feature COMBINE_NORMALS
		#pragma shader_feature SWAPX
		#pragma shader_feature SWAPY
		#pragma shader_feature SWAPZ

		#include "Dithering.cginc"
		#include "BalsaStd.cginc"

		struct Input 
		{
		   float2 uv_MainTex : TEXCOORD;
		   float2 uv_BumpMap : TEXCOORD;
		   float3 vpos;
		   float3 nrm;
		   float4 screenPos;
		   float3 viewDir;

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

		float4 X_ST;
		float4 Y_ST;
		float4 Z_ST;

		half _PlanarGlossiness;
		half _PlanarMetallic;
		half _PlanarBlendPower;
		half _PlanarBlendShift;


		float4 _FresnelColor;
		float _FresnelPower;
		
		fixed4 _Color;
		fixed4 _PlanarColorFloor;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		float2 GetSTMapping(float2 uv, float4 mapST)
		{
			return float2(uv.x * mapST.x + mapST.z, uv.y * mapST.y + mapST.w);
		}
		
		half ramp(half x, half ex, half shift)
		{
			return saturate(pow(x*shift, ex));
		}
			
		void vert(inout appdata_full v, out Input data) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.vpos = v.vertex.xyz;
			data.nrm = v.normal.xyz;
		}


		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			// sample uv maps
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			half3 nrm = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			half4 met = tex2D(_MetallicGlossMap, GetSTMapping(IN.uv_MainTex, _MetallicGlossMap_ST));

			half4 m = tex2D(_PlanarMask, GetSTMapping(IN.uv_MainTex, _PlanarMask_ST));
			half mask = (m.r + m.g + m.b) * 0.3334 * m.a;

			// sample triplanar maps
#if SWAPX
			fixed4 cPlYZ = tex2D(_PlanarMainTex, GetSTMapping(IN.vpos.zy * X_ST.xy + X_ST.zw, _PlanarMainTex_ST)) * _Color;
			half3 nrmPlYZ = UnpackNormal(tex2D(_PlanarBumpMap, GetSTMapping(IN.vpos.zy * X_ST.xy + X_ST.zw, _PlanarMainTex_ST)));
			half4 metPlYZ = tex2D(_PlanarMetallicGlossMap, GetSTMapping(IN.vpos.zy * X_ST.xy + X_ST.zw, _PlanarMainTex_ST));
#else			
			fixed4 cPlYZ = tex2D(_PlanarMainTex, GetSTMapping(IN.vpos.yz * X_ST.xy + X_ST.zw, _PlanarMainTex_ST)) * _Color;
			half3 nrmPlYZ = UnpackNormal(tex2D(_PlanarBumpMap, GetSTMapping(IN.vpos.yz * X_ST.xy + X_ST.zw, _PlanarMainTex_ST)));
			half4 metPlYZ = tex2D(_PlanarMetallicGlossMap, GetSTMapping(IN.vpos.yz * X_ST.xy + X_ST.zw, _PlanarMainTex_ST));
#endif
#if SWAPY
			fixed4 cPlXZ = tex2D(_PlanarMainTex, GetSTMapping(IN.vpos.xz * Y_ST.xy + Y_ST.zw, _PlanarMainTex_ST)) * _Color;
			half3 nrmPlXZ = UnpackNormal(tex2D(_PlanarBumpMap, GetSTMapping(IN.vpos.xz * Y_ST.xy + Y_ST.zw, _PlanarMainTex_ST)));
			half4 metPlXZ = tex2D(_PlanarMetallicGlossMap, GetSTMapping(IN.vpos.xz * Y_ST.xy + Y_ST.zw, _PlanarMainTex_ST));
#else
			fixed4 cPlXZ = tex2D(_PlanarMainTex, GetSTMapping(IN.vpos.zx * Y_ST.xy + Y_ST.zw, _PlanarMainTex_ST)) * _Color;
			half3 nrmPlXZ = UnpackNormal(tex2D(_PlanarBumpMap, GetSTMapping(IN.vpos.zx * Y_ST.xy + Y_ST.zw, _PlanarMainTex_ST)));
			half4 metPlXZ = tex2D(_PlanarMetallicGlossMap, GetSTMapping(IN.vpos.zx * Y_ST.xy + Y_ST.zw, _PlanarMainTex_ST));
#endif
#if SWAPZ
			fixed4 cPlXY = tex2D(_PlanarMainTex, GetSTMapping(IN.vpos.yx * Z_ST.xy + Z_ST.zw, _PlanarMainTex_ST)) * _Color;
			half3 nrmPlXY = UnpackNormal(tex2D(_PlanarBumpMap, GetSTMapping(IN.vpos.yx * Z_ST.xy + Z_ST.zw, _PlanarMainTex_ST)));
			half4 metPlXY = tex2D(_PlanarMetallicGlossMap, GetSTMapping(IN.vpos.yx * Z_ST.xy + Z_ST.zw, _PlanarMainTex_ST));
#else			
			fixed4 cPlXY = tex2D(_PlanarMainTex, GetSTMapping(IN.vpos.xy * Z_ST.xy + Z_ST.zw, _PlanarMainTex_ST)) * _Color;
			half3 nrmPlXY = UnpackNormal(tex2D(_PlanarBumpMap, GetSTMapping(IN.vpos.xy * Z_ST.xy + Z_ST.zw, _PlanarMainTex_ST)));
			half4 metPlXY = tex2D(_PlanarMetallicGlossMap, GetSTMapping(IN.vpos.xy * Z_ST.xy + Z_ST.zw, _PlanarMainTex_ST));
#endif
			
			

			
			half3 nrmDot = half3(ramp(abs(dot(IN.nrm, half3(1, 0, 0))), _PlanarBlendPower, _PlanarBlendShift),
											 ramp(abs(dot(IN.nrm, half3(0, 1, 0))), _PlanarBlendPower, _PlanarBlendShift),
												ramp(abs(dot(IN.nrm, half3(0, 0, 1))), _PlanarBlendPower, _PlanarBlendShift));

			half4 cPl = cPlXZ * nrmDot.y + cPlXY * nrmDot.z + cPlYZ * nrmDot.x;
			cPl = half4(max(cPl.r, _PlanarColorFloor.r), max(cPl.g, _PlanarColorFloor.g), max(cPl.b, _PlanarColorFloor.b), max(cPl.a, _PlanarColorFloor.a));


			half4 metPl = metPlXZ * nrmDot.y + metPlXY * nrmDot.z + metPlYZ * nrmDot.x;
			half3 nrmPl = nrmPlXZ * nrmDot.y + nrmPlXY * nrmDot.z +	nrmPlYZ * nrmDot.x;


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

			half fresnel = 1.0 - saturate(dot(IN.viewDir, o.Normal));
			float3 fresnelColor = _FresnelColor.rgb * pow(fresnel, _FresnelPower);
			o.Emission = tex2D(_EmissionMap, GetUVMapping(IN.uv_MainTex, _EmissionMap_ST)).rgb * _EmissionColor.rgb * _EmissionColor.a + fresnelColor;
			o.Occlusion = tex2D(_OcclusionMap, GetSTMapping(IN.uv_MainTex, _OcclusionMap_ST)) * ( _OcclusionStrength);


			ditherClip(IN.screenPos.xy / IN.screenPos.w, c.a);
		}

		


		ENDCG
	}
	FallBack "Diffuse"
}
