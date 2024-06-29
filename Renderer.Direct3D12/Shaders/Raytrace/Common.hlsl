// Hit information, aka ray payload
// This sample only carries a shading color and hit distance.
// Note that the payload should be kept as small as possible,
// and that its size must be declared in the corresponding
// D3D12_RAYTRACING_SHADER_CONFIG pipeline subobjet.
struct RayPayload
{
    float3 IncomingLight;
    uint Depth;
    float3 RayColour;
};

// Attributes output by the raytracing when hitting a surface,
// here the barycentric coordinates
struct Attributes
{
    float2 bary;
};

float3 barrypolate(float3 barry, float3 in1, float3 in2, float3 in3)
{
    return barry.x * in1 + barry.y * in2 + barry.z * in3;
}

float3 barycentric(Attributes attrib)
{
    return float3((1.f - attrib.bary.x) - attrib.bary.y, attrib.bary.x, attrib.bary.y);
}

struct Material
{
    float3 Colour;
    float3 EmissionColour;
    float EmissionStrength;
};

static const float PI = 3.14159265f;

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
