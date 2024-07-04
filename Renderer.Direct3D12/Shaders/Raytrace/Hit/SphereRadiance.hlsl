#include "SphereHitGroup.hlsl"
#include "../Ray.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);
 
[shader("closesthit")]
void ClosestHit(inout RadiancePayload payload, SphereAttributes attrib)
{
    if (payload.Depth == 1)
    {
        payload.IncomingLight = payload.IncomingLight * Sphere.Colour;
        payload.RayColour = Sphere.Colour;
    }
    
    // We're not interested in path trace bounces
}
