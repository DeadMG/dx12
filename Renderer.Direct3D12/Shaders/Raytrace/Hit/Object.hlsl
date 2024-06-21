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
    uint Sources;
    uint Seed;
};

struct LightSource
{
    float3 Position;
    float Size;
};

ConstantBuffer<Light> Ambient : register(b0);
StructuredBuffer<Vertex> Vertices : register(t0);
StructuredBuffer<uint> Indices : register(t1);
StructuredBuffer<LightSource> LightSources : register(t2);
RaytracingAccelerationStructure SceneBVH : register(t3);

// Generates a seed for a random number generator from 2 inputs plus a backoff
uint initRand(uint val0, uint val1, uint backoff = 16)
{
    uint v0 = val0, v1 = val1, s0 = 0;

    [unroll]
    for (uint n = 0; n < backoff; n++)
    {
        s0 += 0x9e3779b9;
        v0 += ((v1 << 4) + 0xa341316c) ^ (v1 + s0) ^ ((v1 >> 5) + 0xc8013ea4);
        v1 += ((v0 << 4) + 0xad90777d) ^ (v0 + s0) ^ ((v0 >> 5) + 0x7e95761e);
    }
    return v0;
}

// Takes our seed, updates it, and returns a pseudorandom float in [0..1]
float nextRand(inout uint s)
{
    s = (1664525u * s + 1013904223u);
    return float(s & 0x00FFFFFF) / float(0x01000000);
}

[shader("closesthit")]
void ClosestObjectHit(inout MeshHit payload, Attributes attrib)
{
    float3 barycentrics = float3(1.f - attrib.bary.x - attrib.bary.y, attrib.bary.x, attrib.bary.y);
    uint vertId = 3 * PrimitiveIndex();
    
    float3 hitColor = Vertices[Indices[vertId + 0]].Colour * barycentrics.x +
                    Vertices[Indices[vertId + 1]].Colour * barycentrics.y +
                    Vertices[Indices[vertId + 2]].Colour * barycentrics.z;
    
    float3 startPosition = WorldRayOrigin() + RayTCurrent() * WorldRayDirection();
    
    float lightLevel = Ambient.Level;
    
    uint2 index = DispatchRaysIndex().xy;
    
    uint seed = initRand(Ambient.Seed, (index.x + 1) * (index.y + 1));
    
    float theta = nextRand(seed);
    float phi = nextRand(seed);
    
    float3 offset = float3(sin(phi) * cos(theta), sin(phi) * sin(theta), cos(phi));
    
    for (int i = 0; i < Ambient.Sources; ++i)
    {
        LightHit lightPayload;
        
        float3 target = LightSources[i].Position + (offset * LightSources[i].Size);
        
        RayDesc ray;
        ray.Origin = startPosition;
        ray.Direction = normalize(target - startPosition);
        ray.TMin = 0.01;
        ray.TMax = 10000;
    
        TraceRay(
            SceneBVH,
            0,
            0xFF, // Light
            1,
            0,
            0,
            ray,
            lightPayload);
        
        lightLevel = min(1, lightLevel + lightPayload.Intensity);
    }
    
    payload.colorAndDistance = float4(lightLevel * hitColor, RayTCurrent());
}
