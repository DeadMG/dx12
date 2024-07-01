#include "../Ray.hlsl"
#include "../Material.hlsl"

ConstantBuffer<Material> mat : register(b0);
 
[shader("closesthit")]
void ClosestHit(inout RadiancePayload payload, SphereAttributes attrib)
{
    if (payload.Depth == 1)
    {
        payload.IncomingLight = payload.IncomingLight * mat.Colour;
        payload.RayColour = mat.Colour;
    }
    
    // We're not interested in path trace bounces
}
