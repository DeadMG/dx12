#include "../Ray.hlsl"

[shader("closesthit")]
void ClosestHit(inout ShadowPayload payload, TriangleAttributes attrib)
{
    payload.Colour = float3(0, 0, 0);
}
