#pragma once

#include "Spherical.hlsl"
#include "Power.hlsl"

struct Direction
{
    int X: 10;
    int Y: 10;
    int Z: 10;
};

static const float conversion = 512.0f;

float3 directionToCartesian(Direction direction)
{
    float3 result = float3(0, 0, 0);
    result.x = direction.X / conversion;
    result.y = direction.Y / conversion;
    result.z = direction.Z / conversion;
    return result;
}

Direction cartesianToDirection(float3 cartesian)
{
    Direction d;
    d.X = cartesian.x * conversion;
    d.Y = cartesian.y * conversion;
    d.Z = cartesian.z * conversion;
    return d;
}

Direction zeroDirection()
{
    Direction d;
    d.X = 0;
    d.Y = 0;
    d.Z = 0;
    return d;
}
