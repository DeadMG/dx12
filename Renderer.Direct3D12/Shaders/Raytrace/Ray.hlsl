#pragma once

struct RadiancePayload
{
    uint8_t4_packed IncomingDepthOutgoingLight;
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

RadiancePayload Outgoing(uint currentDepth)
{
    RadiancePayload payload;
    payload.IncomingDepthOutgoingLight = currentDepth + 1;
    return payload;
}

uint GetDepth(RadiancePayload payload)
{
    return payload.IncomingDepthOutgoingLight;
}

void Return(inout RadiancePayload payload, float4 colour)
{
    payload.IncomingDepthOutgoingLight = pack_u8(colour * 255.0f);
}

void Return(inout RadiancePayload payload, float3 colour)
{
    Return(payload, float4(colour, 1));
}

float4 GetColour(RadiancePayload payload)
{
    return unpack_u8u32(payload.IncomingDepthOutgoingLight) / 255.0f;
}
