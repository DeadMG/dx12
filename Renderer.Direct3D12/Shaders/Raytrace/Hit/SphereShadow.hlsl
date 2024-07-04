#include "SphereHitGroup.hlsl"
#include "../Ray.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);

[shader("closesthit")]
void ClosestHit(inout ShadowPayload payload, SphereAttributes attrib)
{
    payload.Colour = Sphere.EmissionStrength * Sphere.EmissionColour;
}
