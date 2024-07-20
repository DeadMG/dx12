#include "Ray.hlsl"

struct StarCategory
{
    float3 Colour;
    float Cutoff;
};

struct Vertex
{
    float3 Position;
    float3 Normal;
};

struct Material
{
    float3 Colour;
    float3 EmissionColour;
    float EmissionStrength;
};

struct RaytracingOutputData
{
    bool Filter;
};

struct Triangle
{
    uint VertexIndex1;
    uint VertexIndex2;
    uint VertexIndex3;
    uint MaterialIndex;
    float Power;
};

struct LightSource
{
    float Power;
    uint VerticesIndex;
    uint TrianglesIndex;
    float4x4 WorldMatrix;
    float3 Position;
    float Size;
    bool DistanceIndependent;
};

void fakeUse(inout RadiancePayload payload, StarCategory cat)
{
    payload.IncomingLight += cat.Colour;
}

void fakeUse(inout RadiancePayload payload, Vertex v)
{
    payload.IncomingLight += v.Position;
}

void fakeUse(inout RadiancePayload payload, Material m)
{
    payload.IncomingLight += m.Colour;
}

void fakeUse(inout RadiancePayload payload, LightSource m)
{
    payload.IncomingLight += m.Position;
}

void fakeUse(inout RadiancePayload payload, Triangle m)
{
    payload.IncomingLight += m.Power;
}

void fakeUse(inout RadiancePayload payload, RaytracingOutputData cat)
{
    if (cat.Filter)
    {
        payload.IncomingLight *= 2;        
    }
}

int index(int2 location, int width)
{
    return (location.y * width) + location.x;
}
