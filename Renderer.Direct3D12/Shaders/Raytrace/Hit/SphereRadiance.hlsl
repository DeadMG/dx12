#include "SphereHitGroup.hlsl"
#include "../Structured.hlsl"
#include "../Ray.hlsl"
#include "../GBuffer.hlsl"

ConstantBuffer<SphereHitGroupParameters> Sphere : register(b0);
 
[shader("closesthit")]
void SphereRadianceClosestHit(inout RadiancePayload payload, SphereAttributes attrib)
{
    if (GetDepth(payload) == 1)
    {
        RWStructuredBuffer<RaytracingOutputData> dataBuffer = ResourceDescriptorHeap[Sphere.DataIndex];
        RaytracingOutputData data;
        data.Albedo = asColour(float4(Sphere.Colour, 1));
        data.Emission = asColour(float4(0, 0, 0, 0));
        dataBuffer[raytracingIndex()] = data;
        
        RWTexture2D<uint2> AtrousTexture = ResourceDescriptorHeap[Sphere.AtrousDataTextureIndex];
        AtrousData atrous;
        atrous.Normal = zeroDirection();
        atrous.Depth = 0;
        AtrousTexture[DispatchRaysIndex().xy] = packAtrous(atrous);
        
        RWTexture2D<float4> illuminanceTexture = ResourceDescriptorHeap[Sphere.IlluminanceTextureIndex];
        illuminanceTexture[DispatchRaysIndex().xy] = float4(1, 1, 1, 1);
    }
    else
    {
        Return(payload, float4(Sphere.EmissionStrength * Sphere.EmissionColour, 1));
    }
}
