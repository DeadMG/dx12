#pragma once

struct RadiancePayload
{
    float3 IncomingLight;
    uint Depth;
};

// Attributes output by the raytracing when hitting a surface, here the barycentric coordinates
struct TriangleAttributes
{
    float2 bary;
};

struct SphereAttributes
{
    float dummy;
};

float3 barrypolate(float3 barry, float3 in1, float3 in2, float3 in3)
{
    return barry.x * in1 + barry.y * in2 + barry.z * in3;
}

float3 barycentric(float2 bary)
{
    return float3((1.f - bary.x) - bary.y, bary.x, bary.y);
}

void IncreaseDepth(inout RadiancePayload payload, uint16_t depth)
{
    payload.IncomingLight = float3(0, 0, 0);
    payload.Depth = depth + 1;
}

uint16_t GetDepth(RadiancePayload payload)
{
    return payload.Depth;
}
