#include "Constants.hlsl"

// Takes our seed, updates it, and returns a pseudorandom float in [0..1]
float uniformRand(inout uint s)
{
    s = s * 747796405 + 2891336453;
    uint result = ((s >> ((s >> 28) + 4)) ^ s) * 277803737;
    
    return result / 4294967285.0;
}

float normalRand(inout uint s)
{
    float theta = 2 * PI * uniformRand(s);
    float rho = sqrt(-2 * log(uniformRand(s)));
    return rho * cos(theta);
}

float3 directionRand(inout uint s)
{
    float theta = acos((2 * uniformRand(s)) - 1) - (PI / 2);
    float phi = uniformRand(s) * 2 * PI;
    
    return normalize(float3(cos(phi) * cos(theta), cos(phi) * sin(theta), sin(phi)));
}
