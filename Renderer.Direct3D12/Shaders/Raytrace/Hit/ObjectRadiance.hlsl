#include "../Ray.hlsl"
#include "../Random.hlsl"
#include "../Structured.hlsl"

struct ObjectRadianceParameters
{
    uint Seed;
    uint MaxBounces;
    uint MaxSamples;
    
    uint VerticesIndex;
    uint VertexIndicesIndex;
    uint MaterialIndicesIndex;
    uint MaterialsIndex;
    uint TLASIndex;
};

ConstantBuffer<ObjectRadianceParameters> Settings : register(b0);

uint bufferIndex(uint2 index)
{
    uint2 dims = DispatchRaysDimensions().xy;
    
    uint x = clamp(index.x, 0, dims.x);
    uint y = clamp(index.y, 0, dims.y);
    
    return x + (dims.x * y);
}

float4x4 WorldMatrix()
{
    float3x4 existing = ObjectToWorld3x4();
    return float4x4(existing._m00, existing._m01, existing._m02, existing._m03,
        existing._m10, existing._m11, existing._m12, existing._m13,
        existing._m20, existing._m21, existing._m22, existing._m23,
        0, 0, 0, 1);
}

float3 alignWith(float3 normal, float3 direction)
{
    return direction * sign(dot(normal, direction));
}

float3 positionMul(float3 pos, float4x4 mat)
{    
    float4 result = mul(float4(pos, 1), mat);
    return result.xyz;
}

float3 normalMul(float3 objectNormal, float4x4 mat)
{
    float4 result = mul(float4(objectNormal, 0), mat);
    return normalize(result.xyz / result.w);
}

float3 faceNormal(uint vertId, float4x4 worldMatrix)
{
    StructuredBuffer<Vertex> Vertices = ResourceDescriptorHeap[Settings.VerticesIndex];
    StructuredBuffer<uint> VertexIndices = ResourceDescriptorHeap[Settings.VertexIndicesIndex];
    
    float3 a = Vertices[VertexIndices[vertId]].Position;
    float3 b = Vertices[VertexIndices[vertId + 1]].Position;
    float3 c = Vertices[VertexIndices[vertId + 2]].Position;
    
    float3 result = normalMul(normalize(cross(b - a, c - a)), worldMatrix);
    return float3(result.xy, -result.z);
}

[shader("closesthit")]
void ObjectRadianceClosestHit(inout RadiancePayload payload, TriangleAttributes attrib)
{
    float3 barycentrics = barycentric(attrib);
    
    uint vertId = 3 * PrimitiveIndex();
    
    float3 rayDirection = WorldRayDirection();
    
    float4x4 worldMatrix = WorldMatrix();
    float3 face = faceNormal(vertId, worldMatrix);
    float3 startPosition = WorldRayOrigin() + (RayTCurrent() * rayDirection);
    
    float3 normal = alignWith(-rayDirection, face);
    
    uint2 index = DispatchRaysIndex().xy;
    
    uint seed = Settings.Seed * (index.x + 1) * (index.y + 1);
    
    StructuredBuffer<uint> MaterialIndices = ResourceDescriptorHeap[Settings.MaterialIndicesIndex];
    StructuredBuffer<Material> Materials = ResourceDescriptorHeap[Settings.MaterialsIndex];
    RaytracingAccelerationStructure SceneBVH = ResourceDescriptorHeap[Settings.TLASIndex];
    
    Material m = Materials[MaterialIndices[PrimitiveIndex()]];
            
    float3 incomingLight = float3(0, 0, 0);
    float3 rayColour = float3(1, 1, 1);
    
    int samples = payload.Depth == 1 ? Settings.MaxSamples : 1;
    
    if (payload.Depth < Settings.MaxBounces)
    {
        for (int i = 0; i < samples; ++i)
        {        
            RadiancePayload newPayload;
            newPayload.IncomingLight = float3(0, 0, 0);
            newPayload.RayColour = payload.RayColour;
            newPayload.Depth = payload.Depth + 1;
        
            float3 direction = normalize(normal + directionRand(seed));
        
            RayDesc ray;
            ray.Origin = startPosition;
            ray.Direction = direction;
            ray.TMin = 0.01;
            ray.TMax = 10000;
    
            TraceRay(
                SceneBVH,
                0,
                0xFF,
                0,
                0,
                0,
                ray,
                newPayload);
            
            incomingLight += newPayload.IncomingLight;
            rayColour += newPayload.RayColour;
        }
    }
    
    rayColour = (rayColour / samples) * m.Colour;
    incomingLight = (incomingLight / samples) * rayColour;
    
    payload.IncomingLight += incomingLight + (payload.RayColour * m.EmissionStrength * m.EmissionColour * rayColour);
    payload.RayColour *= rayColour;
}
