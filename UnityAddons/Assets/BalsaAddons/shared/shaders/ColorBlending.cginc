// source: https://elringus.me/blend-modes-in-unity/

fixed Blending_G(fixed4 c) { return .299 * c.r + .587 * c.g + .114 * c.b; }

fixed4 Blending_Darken(fixed4 a, fixed4 b)
{
	fixed4 r = min(a, b);
	r.a = b.a;
	return r;
}

fixed4 Blending_Multiply(fixed4 a, fixed4 b)
{
	fixed4 r = a * b;
	r.a = b.a;
	return r;
}

fixed4 Blending_ColorBurn(fixed4 a, fixed4 b)
{
	fixed4 r = 1.0 - (1.0 - a) / b;
	r.a = b.a;
	return r;
}

fixed4 Blending_LinearBurn(fixed4 a, fixed4 b)
{
	fixed4 r = a + b - 1.0;
	r.a = b.a;
	return r;
}

fixed4 Blending_DarkerColor(fixed4 a, fixed4 b)
{
	fixed4 r = Blending_G(a) < Blending_G(b) ? a : b;
	r.a = b.a;
	return r;
}

fixed4 Blending_Lighten(fixed4 a, fixed4 b)
{
	fixed4 r = max(a, b);
	r.a = b.a;
	return r;
}

fixed4 Blending_Screen(fixed4 a, fixed4 b)
{
	fixed4 r = 1.0 - (1.0 - a) * (1.0 - b);
	r.a = b.a;
	return r;
}

fixed4 Blending_ColorDodge(fixed4 a, fixed4 b)
{
	fixed4 r = a / (1.0 - b);
	r.a = b.a;
	return r;
}

fixed4 Blending_LinearDodge(fixed4 a, fixed4 b)
{
	fixed4 r = a + b;
	r.a = b.a;
	return r;
}

fixed4 Blending_LighterColor(fixed4 a, fixed4 b)
{
	fixed4 r = Blending_G(a) > Blending_G(b) ? a : b;
	r.a = b.a;
	return r;
}

fixed4 Blending_Overlay(fixed4 a, fixed4 b)
{
	fixed4 r = a > .5 ? 1.0 - 2.0 * (1.0 - a) * (1.0 - b) : 2.0 * a * b;
	r.a = b.a;
	return r;
}

fixed4 Blending_SoftLight(fixed4 a, fixed4 b)
{
	fixed4 r = (1.0 - a) * a * b + a * (1.0 - (1.0 - a) * (1.0 - b));
	r.a = b.a;
	return r;
}

fixed4 Blending_HardLight(fixed4 a, fixed4 b)
{
	fixed4 r = b > .5 ? 1.0 - (1.0 - a) * (1.0 - 2.0 * (b - .5)) : a * (2.0 * b);
	r.a = b.a;
	return r;
}

fixed4 Blending_VividLight(fixed4 a, fixed4 b)
{
	fixed4 r = b > .5 ? a / (1.0 - (b - .5) * 2.0) : 1.0 - (1.0 - a) / (b * 2.0);
	r.a = b.a;
	return r;
}

fixed4 Blending_LinearLight(fixed4 a, fixed4 b)
{
	fixed4 r = b > .5 ? a + 2.0 * (b - .5) : a + 2.0 * b - 1.0;
	r.a = b.a;
	return r;
}

fixed4 Blending_PinLight(fixed4 a, fixed4 b)
{
	fixed4 r = b > .5 ? max(a, 2.0 * (b - .5)) : min(a, 2.0 * b);
	r.a = b.a;
	return r;
}

fixed4 Blending_HardMix(fixed4 a, fixed4 b)
{
	fixed4 r = (b > 1.0 - a) ? 1.0 : .0;
	r.a = b.a;
	return r;
}

fixed4 Blending_Difference(fixed4 a, fixed4 b)
{
	fixed4 r = abs(a - b);
	r.a = b.a;
	return r;
}

fixed4 Blending_Exclusion(fixed4 a, fixed4 b)
{
	fixed4 r = a + b - 2.0 * a * b;
	r.a = b.a;
	return r;
}

fixed4 Blending_Subtract(fixed4 a, fixed4 b)
{
	fixed4 r = a - b;
	r.a = b.a;
	return r;
}

fixed4 Blending_Divide(fixed4 a, fixed4 b)
{
	fixed4 r = a / b;
	r.a = b.a;
	return r;
}