#include "../Common.hlsl"

struct SunLight
{
    float3 Colour;
    float Intensity;
};

ConstantBuffer<SunLight> Light : register(b0);

[shader("closesthit")]
void SunLightHit(inout LightHit hit, Attributes attr)
{
    hit.Colour = Light.Colour;
    hit.Intensity = Light.Intensity;
}
