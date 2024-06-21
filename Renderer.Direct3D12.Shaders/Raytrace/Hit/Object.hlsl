#include "../Common.hlsl"

struct Vertex
{
    float3 Position;
    float3 Normal;
    float3 Colour;
};

struct Light
{
    float Level;
};

ConstantBuffer<Light> Ambient : register(b0);
StructuredBuffer<Vertex> Vertices : register(t0);
StructuredBuffer<uint> Indices : register(t1);

[shader("ClosestHit")]
void ClosestObjectHit(inout HitInfo payload, Attributes attrib)
{
    float3 barycentrics = float3(1.f - attrib.bary.x - attrib.bary.y, attrib.bary.x, attrib.bary.y);
    uint vertId = 3 * PrimitiveIndex();
    
    float3 hitColor = Vertices[Indices[vertId + 0]].Colour * barycentrics.x +
                    Vertices[Indices[vertId + 1]].Colour * barycentrics.y +
                    Vertices[Indices[vertId + 2]].Colour * barycentrics.z;
    
    payload.colorAndDistance = float4(Ambient.Level * hitColor, RayTCurrent());
}
