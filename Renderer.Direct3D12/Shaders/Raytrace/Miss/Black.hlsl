#include "../Common.hlsl"

[shader("miss")]
void Miss(inout MeshHit payload : SV_RayPayload)
{
    payload.colorAndDistance = float4(0.0f, 0.0f, 0.0f, 0.0f);
}
