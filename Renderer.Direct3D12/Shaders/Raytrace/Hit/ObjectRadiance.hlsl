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
    
    uint Seed;
    uint TrianglesIndex;
    uint LightsIndex;
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

float3 performSample(RaytracingAccelerationStructure SceneBVH, float3 direction, float3 startPosition, uint depth)
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

float3 monteCarlo(RaytracingAccelerationStructure SceneBVH, StructuredBuffer<LightSource> allLights, uint16_t depth, float3 normal, float3 startPosition, inout uint seed)
{
    if (depth >= Settings.MaxBounces)
        return float3(0, 0, 0); // We can't afford to sample this further
    
    LightSource lights[numLights];
    MonteCarloSample samples[numSamples];
    
    if (prepareLights(lights, allLights, seed, startPosition, normal))
    {
        [unroll]
        for (int i = 0; i < 3; i++)
        {
            samples[i] = sampleLights(lights, seed, startPosition, normal);
            if (!isValidSample(samples[i], normal))
                samples[i] = cosineHemisphere(seed, normal);
        }
    }
    else
    {
        samples[0] = cosineHemisphere(seed, normal);
        samples[1] = cosineHemisphere(seed, normal);
        samples[2] = cosineHemisphere(seed, normal);
    }

    samples[3] = cosineHemisphere(seed, normal);
    
    PreweightedMonteCarloSample preweightedSamples[numSamples];
    preweightSamples(preweightedSamples, samples, lights, startPosition, normal);
    
    float3 incomingLight = float3(0, 0, 0);
        
    for (int16_t i = 0; i < numSamples; ++i)
    {
        PreweightedMonteCarloSample psample = preweightedSamples[i];
        incomingLight = incomingLight + (performSample(SceneBVH, directionToCartesian(psample.direction), startPosition, depth) * psample.weight);
    }
    
    return incomingLight;
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
    
    payload.IncomingLight = min(payload.IncomingLight + (t.EmissionStrength * t.EmissionColour), float3(1, 1, 1));
    
    uint seed = Settings.Seed * pow2(index.x + 1u) * pow2(index.y + 1u);
    
    float3 incomingLight = monteCarlo(ResourceDescriptorHeap[Settings.TLASIndex], ResourceDescriptorHeap[Settings.LightsIndex], GetDepth(payload), normal, startPosition, seed);
    
    payload.IncomingLight = min(payload.IncomingLight + (incomingLight * t.Colour), float3(1, 1, 1));
}
