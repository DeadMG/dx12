#include "../Common.hlsl"

struct SunColour
{
    float3 hitColor;
};

ConstantBuffer<SunColour> Sun : register(b0);

[shader("ClosestHit")]
void ClosestSunHit(inout HitInfo payload, Attributes attr)
{
    payload.colorAndDistance = float4(Sun.hitColor, RayTCurrent());
}
