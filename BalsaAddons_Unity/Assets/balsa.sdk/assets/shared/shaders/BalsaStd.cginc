#ifndef __BALSA_STD__
#define __BALSA_STD__


float2 GetUVMapping(float2 uv, float4 mapST)
{
	return float2(uv.x * mapST.x + mapST.z, uv.y * mapST.y + mapST.w);
}

float2 GetPlanarMapping(float3 vpos, float4 mapST)
{
	return float2(vpos.x * mapST.x + mapST.z, vpos.z * mapST.y + mapST.w);
}

#endif
