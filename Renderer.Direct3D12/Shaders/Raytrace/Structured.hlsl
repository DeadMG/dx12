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

struct PrimaryLight
{
    float3 Position;
    float Size;
};

struct Material
{
    float3 Colour;
    float3 EmissionColour;
    float EmissionStrength;
};

void fakeUse(inout RadiancePayload payload, StarCategory cat)
{
    payload.IncomingLight += cat.Colour;
}

void fakeUse(inout RadiancePayload payload, Vertex v)
{
    payload.IncomingLight += v.Position;
}

void fakeUse(inout RadiancePayload payload, PrimaryLight p)
{
    payload.IncomingLight += p.Position;
}

void fakeUse(inout RadiancePayload payload, Material m)
{
    payload.IncomingLight += m.Colour;
}
