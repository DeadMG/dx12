#include "SphereHitGroup.hlsl"
#include "../Ray.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);
 
[shader("closesthit")]
void SphereRadianceClosestHit(inout RadiancePayload payload, SphereAttributes attrib)
{
    if (GetDepth(payload) == 1)
    {
        payload.IncomingLight = Sphere.Colour;
    }
    else
    {
        payload.IncomingLight = Sphere.EmissionStrength * Sphere.EmissionColour;
    }
}
