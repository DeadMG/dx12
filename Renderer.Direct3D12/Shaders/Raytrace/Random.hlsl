#pragma once

#include "Constants.hlsl"

uint uintUniformRand(inout uint s)
{
    s = s * 747796405 + 2891336453;
    return ((s >> ((s >> 28) + 4)) ^ s) * 277803737;
}

// Takes our seed, updates it, and returns a pseudorandom float in [0..1]
float uniformRand(inout uint s)
{
    return uintUniformRand(s) / 4294967285.0;
}

float normalRand(inout uint s)
{
    float theta = 2 * PI * uniformRand(s);
    float rho = sqrt(-2 * log(uniformRand(s)));
    return rho * cos(theta);
}
