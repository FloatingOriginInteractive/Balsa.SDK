Shader "Floating Origin Studios/Standard with Paint Map" 
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}

		_Glossiness ("Smoothness", Range(0,1)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.34

        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionColor("Emissive Color", Color) = (0,0,0)

		_FresnelColor("Fresnel Color", Color) = (0,0,0)
		_FresnelPower("Fresnel Power", Float) = 1.0

		_PaintMap ("Paint Map", 2D) = "black" {}
		_PaintColor0 ("1st Paint Color", Color) = (1,1,1,1)
		_PaintColor1 ("2nd Paint Color", Color) = (1,1,1,1)
		_PaintColor2 ("3rd Paint Color", Color) = (1,1,1,1)
		_PaintColor3 ("4th Paint Color", Color) = (1,1,1,1)

		_PaintTarget0 ("Paint Target 1", Color) = (1,0,0,1)
		_PaintTarget1 ("Paint Target 2", Color) = (0,1,0,1)
		_PaintTarget2 ("Paint Target 3", Color) = (0,0,1,1)
		_PaintTarget3 ("Paint Target 4", Color) = (1,0,1,1)
		_PTThreshold ("Paint Target Threshold", Range(0,1)) = 0.36

		_ZWrite("ZWrite", Int) = 1
		_ZTest("ZTest", Int) = 4 //LEQual
		_Cull("Culling", Int) = 2 //Back
		_StencilPass("Stencil Pass Op", Int) = 0

	}

	SubShader
	{
		Cull[_Cull]
		ZTest[_ZTest]
		ZWrite[_ZWrite]

		//Blend One OneMinusSrcAlpha

		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

		Stencil
		{
			Ref 128
			Comp Always
			Pass[_StencilPass]
		}

		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows keepalpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "Dithering.cginc"
		#include "BalsaStd.cginc"
			
		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _PaintMap;

		struct Input
		{
			float2 uv_MainTex : TEXCOORD;
		   float2 uv_BumpMap : TEXCOORD;
		   float4 screenPos;
		   float3 viewDir;

		};
		
		half _Glossiness;
		half _Metallic;
		sampler2D _MetallicGlossMap;

		half _OcclusionStrength;
		sampler2D _OcclusionMap;

		half4 _EmissionColor;
		sampler2D _EmissionMap;
		float4 _EmissionMap_ST;

		fixed4 _Color;

		half _PTThreshold;
		fixed4 _PaintColor0;
		fixed4 _PaintColor1;
		fixed4 _PaintColor2;		
		fixed4 _PaintColor3;
		fixed4 _PaintTarget0;
		fixed4 _PaintTarget1;
		fixed4 _PaintTarget2;
		fixed4 _PaintTarget3;


		float4 _FresnelColor;
		float _FresnelPower;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

			fixed4 paintmap = tex2D(_PaintMap, IN.uv_MainTex);
			fixed4 baseColor = fixed4(c.rgb,1);
			float d0 = dot(paintmap.xyz, _PaintTarget0.xyz);
			if (d0 > _PTThreshold)
				baseColor =  lerp(baseColor, _PaintColor0, d0);
			else
			{
				float d1 = dot(paintmap.xyz, _PaintTarget1.xyz);
				if (d1 > d0)
					baseColor = lerp(baseColor, _PaintColor1, d1);
				else
				{			
					
					float d2 = dot(paintmap.xyz, _PaintTarget2.xyz);
					if (d2 > d1)
						baseColor= lerp(baseColor, _PaintColor2, d2);
					else
					{			
					
						float d3 = dot(paintmap.xyz, _PaintTarget3.xyz);
						if (d3 > d2)
							baseColor= lerp(baseColor, _PaintColor3, d3);
					}	
				}	
			}

			c *= baseColor;

			o.Albedo = c.rgb * _Color.a;
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));

			half4 ms = tex2D (_MetallicGlossMap, IN.uv_MainTex);	
			o.Metallic = _Metallic * (ms.r + ms.g + ms.b);
			o.Smoothness =  _Glossiness * ms.a;
			o.Alpha = c.a;

			o.Occlusion = tex2D (_OcclusionMap, IN.uv_MainTex) *(1 -  _OcclusionStrength);

			half fresnel = 1.0 - saturate(dot(IN.viewDir, o.Normal));
			float3 fresnelColor = _FresnelColor.rgb * pow(fresnel, _FresnelPower);
			o.Emission = tex2D(_EmissionMap, GetUVMapping(IN.uv_MainTex, _EmissionMap_ST)).rgb * _EmissionColor.rgb * _EmissionColor.a + fresnelColor;

			ditherClip(IN.screenPos.xy / IN.screenPos.w, c.a);

		}


		ENDCG
	}
	FallBack "Diffuse"
}
