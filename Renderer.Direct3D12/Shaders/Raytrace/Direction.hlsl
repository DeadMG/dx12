#pragma once

#include "Spherical.hlsl"

struct Direction
{
    uint16_t elevation;
    uint16_t azimuth;
};

float3 directionToCartesian(Direction direction)
{
    Spherical sphere;
    sphere.r = 1;
    sphere.elevation = (direction.elevation / 65536.0f) * PI;
    sphere.azimuth = (direction.azimuth / 65536.0f) * 2 * PI;
    return sphericalToCartesian(sphere);
}

Direction cartesianToDirection(float3 cartesian)
{
    Spherical sphere = cartesianToSpherical(cartesian);
    Direction d;
    d.elevation = (sphere.elevation / PI) * 65536u;
    d.azimuth = (sphere.azimuth / (2 * PI)) * 65536u;
    return d;
}
