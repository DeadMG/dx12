#include "../Common.hlsl"

struct Vertex
{
    float3 Position;
    float3 Normal;
};

struct SettingsS
{
    uint Seed;
    uint MaxRays;
};

ConstantBuffer<SettingsS> Settings : register(b0);
StructuredBuffer<Vertex> Vertices : register(t0);
StructuredBuffer<uint> VertexIndices : register(t1);
StructuredBuffer<uint> MaterialIndices : register(t2);
StructuredBuffer<Material> Materials : register(t3);
RaytracingAccelerationStructure SceneBVH : register(t4);

// Takes our seed, updates it, and returns a pseudorandom float in [0..1]
float uniformRand(inout uint s)
{
    s = s * 747796405 + 2891336453;
    uint result = ((s >> ((s >> 28) + 4)) ^ s) * 277803737;
    
    return result / 4294967285.0;
}

float normalRand(inout uint s)
{
    float theta = 2 * PI * uniformRand(s);
    float rho = sqrt(-2 * log(uniformRand(s)));
    return rho * cos(theta);
}

float3 directionRand(inout uint s)
{
    return normalize(float3(normalRand(s), normalRand(s), normalRand(s)));
}

uint bufferIndex(uint2 index)
{
    uint2 dims = DispatchRaysDimensions().xy;
    
    uint x = clamp(index.x, 0, dims.x);
    uint y = clamp(index.y, 0, dims.y);
    
    return x + (dims.x * y);
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
    return normalize(result.xyz);
}

float3 faceNormal(uint vertId)
{
    float4x4 worldMatrix = float4x4(ObjectToWorld3x4(), float4(0, 0, 0, 1));
    float3 a = Vertices[VertexIndices[vertId]].Position;
    float3 b = Vertices[VertexIndices[vertId + 1]].Position;
    float3 c = Vertices[VertexIndices[vertId + 2]].Position;
    
    return normalize(normalMul(normalize(cross(a - b, c - b)), worldMatrix));
}

[shader("closesthit")]
void ClosestObjectHit(inout RayPayload payload, Attributes attrib)
{
    float3 barycentrics = barycentric(attrib);
    
    uint vertId = 3 * PrimitiveIndex();
    
    Vertex a = Vertices[VertexIndices[vertId]];
    Vertex b = Vertices[VertexIndices[vertId + 1]];
    Vertex c = Vertices[VertexIndices[vertId + 2]];
    
    float3 objectNormal = normalize(barrypolate(barycentrics, a.Normal, b.Normal, c.Normal));
    float3 rayDirection = WorldRayDirection();
    float4x4 worldMatrix = float4x4(ObjectToWorld3x4(), float4(0, 0, 0, 1));
    float3 baseNormal = normalMul(objectNormal, worldMatrix);
    float3 face = faceNormal(vertId);
    float3 startPosition = WorldRayOrigin() + (RayTCurrent() * rayDirection);
    
    float3 normal = alignWith(-rayDirection, baseNormal);
    
    uint2 index = DispatchRaysIndex().xy;
    
    uint seed = Settings.Seed * (index.x + 1) * (index.y + 1);
    
    Material m = Materials[MaterialIndices[PrimitiveIndex()]];
        
    if (payload.Depth < Settings.MaxRays && m.EmissionStrength < 0.1)
    {
        payload.Depth += 1;
        
        float3 offset = alignWith(normal, directionRand(seed));
        float3 direction = normalize(normal + offset);
        
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
            payload);
    }

    //payload.IncomingLight = float3(1, 0, 0);
    payload.IncomingLight += m.EmissionColour * m.EmissionStrength * payload.RayColour;
    payload.RayColour *= m.Colour;
}
