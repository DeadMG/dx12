#include "SphereHitGroup.hlsl"
#include "../Structured.hlsl"
#include "../Ray.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);
 
[shader("closesthit")]
void SphereRadianceClosestHit(inout RadiancePayload payload, SphereAttributes attrib)
{
    if (GetDepth(payload) == 1)
    {
        RWStructuredBuffer<RaytracingOutputData> dataBuffer = ResourceDescriptorHeap[Sphere.DataIndex];
        RaytracingOutputData data;
        data.Filter = false;
        dataBuffer[raytracingIndex()] = data;
        
        payload.IncomingLight = Sphere.Colour;
    }
    else
    {
        payload.IncomingLight = Sphere.EmissionStrength * Sphere.EmissionColour;
    }
}
