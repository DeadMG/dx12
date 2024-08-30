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
        data.Depth = 0;
        data.Albedo = Sphere.Colour;
        data.Emission = float3(0, 0, 0);        
        dataBuffer[raytracingIndex()] = data;
        
        RWTexture2D<float4> illuminanceTexture = ResourceDescriptorHeap[Sphere.IlluminanceTextureIndex];
        illuminanceTexture[DispatchRaysIndex().xy] = float4(1, 1, 1, 1);
    }
    else
    {
        Return(payload, Sphere.EmissionStrength * Sphere.EmissionColour);
    }
}
