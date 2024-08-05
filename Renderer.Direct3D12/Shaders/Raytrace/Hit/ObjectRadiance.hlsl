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

half3 alignWith(half3 normal, half3 direction)
{
    return direction * sign(dot(normal, direction));
}

half3 normalMul(half3 objectNormal, float4x4 mat)
{
    float4 result = mul(float4(objectNormal, 0), mat);
    return normalize(result.xyz);
}

half3 monteCarlo(RaytracingAccelerationStructure SceneBVH, uint depth, half3 normal, half3 startPosition, inout uint seed)
{
    if (depth >= Settings.MaxBounces)
        return half3(0, 0, 0); // We can't afford to sample this further
    
    SampledLight lights[numLights];
    bool anyLights = prepareLights(lights, Settings.Light, seed, startPosition, normal);
    
    half3 incomingLight = half3(0, 0, 0);
    
    // Due to the low likelihood of BRDF samples hitting, we allocate 1 sample there, and 3 for NEE.
    // Unless there are no lights in which case all 4 samples goes to BRDF.
    int16_t brdfSamples = 1;
    int16_t neeSamples = 3;
    
    if (!anyLights)
    {
        neeSamples = 0;
    }
    
    int16_t totalSamples = brdfSamples + neeSamples;
    
    for (int16_t i = 0; i < totalSamples; ++i)
    {
        RayDesc ray;
        ray.Origin = startPosition;
        ray.Direction = i < brdfSamples ? cosineHemisphere(seed, normal) : sampleLights(lights, seed).direction;
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
            
        incomingLight += newPayload.IncomingLight;
    }
    
    // This is not a valid implementation of MIS. To be fixed.
    return incomingLight / totalSamples;
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
    
    half3 startPosition = WorldRayOrigin() + (RayTCurrent() * WorldRayDirection());
    half3 normal = alignWith(-WorldRayDirection(), normalMul(t.Normal, Settings.WorldMatrix));
    
    uint seed = Settings.Seed * pow2(index.x + 1u) * pow2(index.y + 1u);
    
    half3 incomingLight = monteCarlo(ResourceDescriptorHeap[Settings.TLASIndex], GetDepth(payload), normal, startPosition, seed);
    
    payload.IncomingLight = min(payload.IncomingLight + (incomingLight * t.Colour) + (t.EmissionStrength * t.EmissionColour), half3(1, 1, 1));
}
