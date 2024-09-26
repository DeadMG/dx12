#pragma once

#include "Ray.hlsl"
#include "Colour.hlsl"

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

struct Triangle
{
    float3 Normal;
    float3 Colour;
    float3 EmissionColour;
    float EmissionStrength;
    
    float pad0;
    float pad1;
};

struct LightSource
{
    float Power;
    float3 Position;
    float Size;
};

struct RaytracingOutputData
{
    Colour Emission;
    Colour Albedo;
};

void fakeUse(inout RadiancePayload payload, RaytracingOutputData l)
{
    payload.IncomingDepthOutgoingLight += l.Emission.value;
}

void fakeUse(inout RadiancePayload payload, LightSource l)
{
    payload.IncomingDepthOutgoingLight += l.Power;
}

void fakeUse(inout RadiancePayload payload, StarCategory cat)
{
    payload.IncomingDepthOutgoingLight += cat.Colour;
}

void fakeUse(inout RadiancePayload payload, Vertex cat)
{
    payload.IncomingDepthOutgoingLight += cat.Position;
}

void fakeUse(inout RadiancePayload payload, Triangle m)
{
    payload.IncomingDepthOutgoingLight += m.Normal;
}

int index(int2 location, int width)
{
    return (location.y * width) + location.x;
}

int raytracingIndex()
{
    return index(DispatchRaysIndex().xy, DispatchRaysDimensions().x);
}
