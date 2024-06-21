#include "../Common.hlsl"

[shader("closesthit")]
void ObjectLight(inout LightHit payload : SV_RayPayload, Attributes attr)
{
    payload.Colour = float3(0.0f, 0.0f, 0.0f);
    payload.Intensity = 0;
}
