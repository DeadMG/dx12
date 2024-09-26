#include "../Ray.hlsl"
#include "../Random.hlsl"
#include "../Structured.hlsl"
#include "../Light.hlsl"
#include "../Sampling.hlsl"
#include "../Power.hlsl"
#include "../GBuffer.hlsl"

struct ObjectRadianceParameters
{
    uint MaxBounces;
    float AmbientLight;
    float4x4 WorldMatrix;
    
    uint Seed;
    uint TrianglesIndex;
    uint LightsIndex;
    uint TLASIndex;
    uint DataIndex;
    uint IlluminanceTextureIndex;
    uint AtrousDataTextureIndex;
    uint PreviousIlluminanceTextureIndex;
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
        
    RadiancePayload newPayload = Outgoing(depth);
    
    TraceRay(
        SceneBVH,
        0,
        0xFF,
        0,
        0,
        0,
        ray,
        newPayload);
            
    return GetColour(newPayload).xyz;
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
        for (int i = 0; i < numSamples - 1; i++)
        {
            samples[i] = sampleLights(lights, seed, startPosition, normal);
            if (!isValidSample(samples[i], normal))
                samples[i] = cosineHemisphere(seed, normal);
        }
    }
    else
    {
        [unroll]
        for (int i = 0; i < numSamples - 1; i++)
        {
            samples[i] = cosineHemisphere(seed, normal);
        }
    }
    
    samples[numSamples - 1] = cosineHemisphere(seed, normal);
    
    PreweightedMonteCarloSample preweightedSamples[numSamples];
    preweightSamples(preweightedSamples, samples, lights, startPosition, normal);
    
    float3 incomingLight = float3(0, 0, 0);
        
    [unroll]
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
    uint depth = GetDepth(payload);
    
    Triangle t = LoadTriangle(PrimitiveIndex());
    float3 startPosition = WorldRayOrigin() + (RayTCurrent() * WorldRayDirection());
    float3 normal = alignWith(-WorldRayDirection(), normalMul(t.Normal, Settings.WorldMatrix));
    
    uint2 index = DispatchRaysIndex().xy;
    uint seed = Settings.Seed * pow2(index.x + 1u) * pow2(index.y + 1u);
       
    if (depth == 1)
    {        
        RWStructuredBuffer<RaytracingOutputData> dataBuffer = ResourceDescriptorHeap[Settings.DataIndex];
        RaytracingOutputData data;
        data.Albedo = asColour(float4(t.Colour, 1));
        data.Emission = asColour(float4(t.EmissionStrength * t.EmissionColour, 1));
        dataBuffer[raytracingIndex()] = data;
        
        RWTexture2D<uint2> AtrousTexture = ResourceDescriptorHeap[Settings.AtrousDataTextureIndex];
        AtrousData atrous;
        atrous.Normal = cartesianToDirection(normal);
        atrous.Depth = RayTCurrent();
        AtrousTexture[index] = packAtrous(atrous);
        
        float3 incomingLight = monteCarlo(ResourceDescriptorHeap[Settings.TLASIndex], ResourceDescriptorHeap[Settings.LightsIndex], depth, normal, startPosition, seed);
        RWTexture2D<float4> illuminanceTexture = ResourceDescriptorHeap[Settings.IlluminanceTextureIndex];
        
        if (Settings.PreviousIlluminanceTextureIndex == 0xFFFFFFFF)
        {
            illuminanceTexture[index] = float4(incomingLight, 1);
        }
        else
        {
            RWTexture2D<float4> previousIlluminanceTexture = ResourceDescriptorHeap[Settings.PreviousIlluminanceTextureIndex];
            illuminanceTexture[index] = lerp(previousIlluminanceTexture[index], float4(incomingLight, 1), 0.2);            
        }
    }
    else
    {
        float3 incomingLight = monteCarlo(ResourceDescriptorHeap[Settings.TLASIndex], ResourceDescriptorHeap[Settings.LightsIndex], depth, normal, startPosition, seed);
        Return(payload, float4(min(t.EmissionStrength * t.EmissionColour + incomingLight * t.Colour, float3(1, 1, 1)), 1));
    }
}
