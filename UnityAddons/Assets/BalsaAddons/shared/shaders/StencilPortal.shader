Shader "Floating Origin Studios/Stencil Portal" {
   SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1"}
        Pass {
            Stencil {
                Ref 2
                Comp always
                Pass replace
            }
			
			Blend SrcAlpha One
			ZWrite Off
			Cull Back
			ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata {
                float4 vertex : POSITION;
            };
            struct v2f {
                float4 pos : SV_POSITION;
            };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            half4 frag(v2f i) : SV_Target {
                return half4(0,0,0,0);
            }
            ENDCG
        }
    } 
}