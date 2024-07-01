#include "../Ray.hlsl"

[shader("miss")]
void Miss(inout ShadowPayload payload)
{
    payload.Colour = float3(0, 0, 0);
}
