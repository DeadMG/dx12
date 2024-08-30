#pragma once

#include "Spherical.hlsl"
#include "Power.hlsl"

struct Direction
{
    int X: 15;
    int Y: 15;
    uint ZPositive: 1;
};

static const float conversion = 16383.0f;

float3 directionToCartesian(Direction direction)
{
    float3 result = float3(0, 0, 0);
    result.x = direction.X / conversion;
    result.y = direction.Y / conversion;
    result.z = (direction.ZPositive == 1 ? 1 : -1) * sqrt(1 - pow2(result.x) - pow2(result.y));
    return result;
}

Direction cartesianToDirection(float3 cartesian)
{
    Direction d;
    d.ZPositive = cartesian.z >= 0 ? 1 : 0;
    d.X = cartesian.x * conversion;
    d.Y = cartesian.y * conversion;
    return d;
}

Direction zeroDirection()
{
    Direction d;
    d.X = 0;
    d.Y = 0;
    d.ZPositive = 0;
    return d;
}
