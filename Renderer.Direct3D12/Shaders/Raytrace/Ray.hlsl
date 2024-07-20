#pragma once

struct RadiancePayload
{
    float3 IncomingLight;
    uint Depth;
    bool Filter;
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
