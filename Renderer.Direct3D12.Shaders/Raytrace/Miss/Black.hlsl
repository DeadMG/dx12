#include "../Common.hlsl"

[shader("miss")]
void Miss(inout RayPayload payload : SV_RayPayload)
{
    // Direct camera ray
    if (payload.Depth == 1)
    {
        payload.IncomingLight = float3(0, 0, 0);
    }
    else
    {
        //payload.IncomingLight = float3(0.1, 0.1, 0.1);        
    }
}
