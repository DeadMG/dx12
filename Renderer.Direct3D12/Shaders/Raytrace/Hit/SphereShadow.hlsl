#include "../Ray.hlsl"
#include "../Material.hlsl"

ConstantBuffer<Material> mat : register(b0);

[shader("closesthit")]
void ClosestHit(inout ShadowPayload payload, SphereAttributes attrib)
{
    payload.Colour = mat.EmissionStrength * mat.EmissionColour;
}
