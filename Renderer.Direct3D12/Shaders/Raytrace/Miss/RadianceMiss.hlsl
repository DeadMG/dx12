#include "../Ray.hlsl"
#include "../Constants.hlsl"
#include "../Structured.hlsl"

struct StarfieldParameters
{
    float NoiseScale;
    float NoiseCutoff;
    float TemperatureScale;
    uint StarCategories;
    uint Seed;
    float AmbientLight;
    uint CategoryIndex;
};

ConstantBuffer<StarfieldParameters> Parameters : register(b0);

float3 permute(float3 x)
{
    return fmod((x * 34.0 + 1.0) * x, 289);
}

static const float simplexX = (3.0 - sqrt(3.0)) / 6.0;
static const float simplexY = 0.5 * (sqrt(3.0) - 1.0);
static const float simplexZ = -1.0 + 2.0 * simplexX;
static const float simplexW = 1.0 / 41.0;

// output noise is in range [-1, 1]
float snoise(float2 v)
{
    const float4 C = float4(simplexX,
                            simplexY,
                            simplexZ,
                            simplexW);

    // First corner
    float2 i = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);

    // Other corners
    float2 i1;
    i1.x = step(x0.y, x0.x);
    i1.y = 1.0 - i1.x;

    float2 x1 = x0 - i1 + 1.0 * C.xx;
    float2 x2 = x0 - 1.0 + 2.0 * C.xx;

    // Permutations
    i = fmod(i, 289); // Avoid truncation effects in permutation
    float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));

    float3 m = max(0.5 - float3(dot(x0, x0), dot(x1, x1), dot(x2, x2)), 0.0);
    m = m * m;
    m = m * m;

    // Gradients: 41 points uniformly over a line, mapped onto a diamond.
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;

    // Normalise gradients implicitly by scaling m
    m *= rsqrt(a0 * a0 + h * h);

    // Compute final noise value at P
    float3 g = float3(
        a0.x * x0.x + h.x * x0.y,
        a0.y * x1.x + h.y * x1.y,
        g.z = a0.z * x2.x + h.z * x2.y
    );
    return 100.0 * dot(m, g);
}

float snoise01(float2 v)
{
    return snoise(v) * 0.5 + 0.5;
}

float spotNoise(float2 spherical, float size, float cutoff)
{
    float increase = 1 / (1 - cutoff);
    float noise = snoise01(spherical * size);
    return clamp(max(noise - cutoff, 0) * increase, 0, 1);
}

float3 colour(float distribution)
{
    StructuredBuffer<StarCategory> Categories = ResourceDescriptorHeap[Parameters.CategoryIndex];
    
    for (int i = 0; i < Parameters.StarCategories; i++)
    {
        if (distribution < Categories[i].Cutoff)
            return Categories[i].Colour;
    }
    
    return float3(0, 0, 0);
}

[shader("miss")]
void Miss(inout RadiancePayload payload)
{
    // Direct camera ray
    if (payload.Depth == 1)
    {
        float3 direction = normalize(WorldRayDirection());
        
        float theta = acos(direction.z) / (2 * PI);
        float phi = atan(direction.y / direction.x) / (2 * PI);
        
        float2 spherical = float2(theta, phi);
        float brightness = spotNoise(spherical, Parameters.NoiseScale, Parameters.NoiseCutoff);
        float distribution = spotNoise(spherical, Parameters.TemperatureScale, 0);
        
        payload.IncomingLight = brightness * colour(distribution);
    }
    else
    {
        payload.IncomingLight = float3(Parameters.AmbientLight, Parameters.AmbientLight, Parameters.AmbientLight);
    }
}
