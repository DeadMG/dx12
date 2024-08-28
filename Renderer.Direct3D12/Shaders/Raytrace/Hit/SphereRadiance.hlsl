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
        data.Normal = float3(0, 0, 0);
        data.Position = float3(0, 0, 0);
        dataBuffer[raytracingIndex()] = data;
        
        Return(payload, Sphere.Colour);
    }
    else
    {
        Return(payload, Sphere.EmissionStrength * Sphere.EmissionColour);
    }
}
