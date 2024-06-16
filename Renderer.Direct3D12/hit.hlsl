// Hit information, aka ray payload
// This sample only carries a shading color and hit distance.
// Note that the payload should be kept as small as possible,
// and that its size must be declared in the corresponding
// D3D12_RAYTRACING_SHADER_CONFIG pipeline subobjet.
struct HitInfo
{
    float4 colorAndDistance;
};

// Attributes output by the raytracing when hitting a surface,
// here the barycentric coordinates
struct Attributes
{
    float2 bary;
};

struct Vertex
{
    float3 Position;
    float3 Normal;
    float3 Colour;
};

StructuredBuffer<Vertex> Vertices : register(t0);
StructuredBuffer<uint> Indices : register(t1);

[shader("closesthit")]
void ClosestHit(inout HitInfo payload, Attributes attrib)
{
    float3 barycentrics = float3(1.f - attrib.bary.x - attrib.bary.y, attrib.bary.x, attrib.bary.y);
    uint vertId = 3 * PrimitiveIndex();
    
    float3 hitColor = Vertices[Indices[vertId + 0]].Colour * barycentrics.x +
                    Vertices[Indices[vertId + 1]].Colour * barycentrics.y +
                    Vertices[Indices[vertId + 2]].Colour * barycentrics.z;
    
    payload.colorAndDistance = float4(hitColor, RayTCurrent());
}
