#pragma once

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

struct RaytracingOutputData
{
    bool Filter;
};

struct Triangle
{
    half3 Normal;
    half3 Colour;
    half3 EmissionColour;
    half EmissionStrength;
    
    half pad0;
    half pad1;
};

void fakeUse(inout RadiancePayload payload, StarCategory cat)
{
    payload.IncomingLight += cat.Colour;
}

void fakeUse(inout RadiancePayload payload, Vertex cat)
{
    payload.IncomingLight += cat.Position;
}

void fakeUse(inout RadiancePayload payload, Triangle m)
{
    payload.IncomingLight += m.Normal;
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
