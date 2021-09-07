#ifndef __BALSA_DECALS__
#define __BALSA_DECALS__



#include "ColorBlending.cginc"



fixed3 BalsaStd_DecalBlending(fixed4 decals, fixed4 c, fixed alphaBoost, fixed alphaMax)
{
	fixed3 dc = fixed3(1,1,1);


#if _DECALBLEND_NORMAL
	dc = decals.rgb;
#elif _DECALBLEND_MULTIPLY
	dc = Blending_Multiply(decals, c).rgb;
#elif _DECALBLEND_OVERLAY
	dc = Blending_Overlay(decals, c).rgb;
#elif _DECALBLEND_COLORBURN
	dc = Blending_ColorBurn(decals, c).rgb;
#endif


	dc = lerp(c.rgb, dc.rgb, min(decals.a * alphaBoost, alphaMax));

	return dc;
}

#endif