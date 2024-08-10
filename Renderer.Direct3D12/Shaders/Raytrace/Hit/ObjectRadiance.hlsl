#include "../Ray.hlsl"
#include "../Random.hlsl"
#include "../Structured.hlsl"
#include "../Light.hlsl"
#include "../Sampling.hlsl"
#include "../Power.hlsl"

struct ObjectRadianceParameters
{
    uint MaxBounces;
    float AmbientLight;
    float4x4 WorldMatrix;
    LightSource Light;
    uint Seed;
    uint TrianglesIndex;
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

float3 alignWith(float3 normal, float3 direction)
{
    return direction * sign(dot(normal, direction));
}

float3 normalMul(float3 objectNormal, float4x4 mat)
{
    float4 result = mul(float4(objectNormal, 0), mat);
    return normalize(result.xyz);
}

float3 sample(RaytracingAccelerationStructure SceneBVH, float3 direction, float3 startPosition, uint depth)
{
    RayDesc ray;
    ray.Origin = startPosition;
    ray.Direction = direction;
    ray.TMin = 0.01;
    ray.TMax = 10000;
        
    RadiancePayload newPayload;
    IncreaseDepth(newPayload, depth);
    
    TraceRay(
        SceneBVH,
        0,
        0xFF,
        0,
        0,
        0,
        ray,
        newPayload);
            
    return newPayload.IncomingLight;    
}

float3 monteCarlo(RaytracingAccelerationStructure SceneBVH, uint16_t depth, float3 normal, float3 startPosition, inout uint seed)
{
    if (depth >= Settings.MaxBounces)
        return float3(0, 0, 0); // We can't afford to sample this further
    
    SampledLight lights[numLights];
    bool anyLights = prepareLights(lights, Settings.Light, seed, startPosition, normal);
    
    float3 incomingLight = float3(0, 0, 0);
    
    // Due to the low likelihood of BRDF samples hitting, we allocate 1 sample there, and 3 for NEE.
    // Unless there are no lights in which case all 4 samples goes to BRDF.
    int16_t brdfSamples = 1;
    int16_t neeSamples = 3;
    
    if (!anyLights)
    {
        brdfSamples += neeSamples;
        neeSamples = 0;
    }
    
    for (int16_t i = 0; i < neeSamples; i++)
    {
        float3 direction = sampleSphereLight(seed, startPosition, normal, Settings.Light.Power, Settings.Light.Position, Settings.Light.Size, Settings.Light.DistanceIndependent).direction;
        
        if (all(direction == float3(0, 0, 0)) || dot(direction, normal) < 0)
        {
            incomingLight += float3(Settings.AmbientLight, Settings.AmbientLight, Settings.AmbientLight);
            brdfSamples += 1;
            continue;
        }
        
        incomingLight += sample(SceneBVH, direction, startPosition, depth);
    }
        
    for (int16_t i = 0; i < brdfSamples; ++i)
    {
        incomingLight += sample(SceneBVH, cosineHemisphere(seed, normal), startPosition, depth);
    }
    
    // This is not a valid implementation of MIS. To be fixed.
    return incomingLight / (brdfSamples + neeSamples);
}

Triangle LoadTriangle(int index)
{
    StructuredBuffer<Triangle> Triangles = ResourceDescriptorHeap[Settings.TrianglesIndex];
    
    return Triangles[index];
}

[shader("closesthit")]
void ObjectRadianceClosestHit(inout RadiancePayload payload, TriangleAttributes attrib)
{   
    Triangle t = LoadTriangle(PrimitiveIndex());
    
    uint2 index = DispatchRaysIndex().xy;
    
    float3 startPosition = WorldRayOrigin() + (RayTCurrent() * WorldRayDirection());
    float3 normal = alignWith(-WorldRayDirection(), normalMul(t.Normal, Settings.WorldMatrix));
    
    uint seed = Settings.Seed * pow2(index.x + 1u) * pow2(index.y + 1u);
    
    float3 incomingLight = monteCarlo(ResourceDescriptorHeap[Settings.TLASIndex], GetDepth(payload), normal, startPosition, seed);
    
    payload.IncomingLight = min(payload.IncomingLight + (incomingLight * t.Colour) + (t.EmissionStrength * t.EmissionColour), float3(1, 1, 1));
}
