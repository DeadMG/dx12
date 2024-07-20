#include "../Ray.hlsl"
#include "../Random.hlsl"
#include "../Structured.hlsl"
#include "../Light.hlsl"

struct ObjectRadianceParameters
{
    uint Seed;
    uint MaxBounces;
    uint MaxSamples;
    float AmbientLight;
    float4x4 WorldMatrix;
    
    uint LightsIndex;
    uint VerticesIndex;
    uint TrianglesIndex;
    uint MaterialsIndex;
    uint TLASIndex;
};

ConstantBuffer<ObjectRadianceParameters> Settings : register(b0);

uint bufferIndex(uint2 index)
{
    uint2 dims = DispatchRaysDimensions().xy;
    
    uint x = clamp(index.x, 0, dims.x);
    uint y = clamp(index.y, 0, dims.y);
    
    return x + (dims.x * y);
}

float4x4 WorldMatrix()
{
    float3x4 existing = ObjectToWorld3x4();
    return float4x4(existing._m00, existing._m01, existing._m02, existing._m03,
        existing._m10, existing._m11, existing._m12, existing._m13,
        existing._m20, existing._m21, existing._m22, existing._m23,
        0, 0, 0, 1);
}

float3 alignWith(float3 normal, float3 direction)
{
    return direction * sign(dot(normal, direction));
}

float3 normalMul(float3 objectNormal, float4x4 mat)
{
    float4 result = mul(float4(objectNormal, 0), mat);
    return normalize(result.xyz);
}

float3 faceNormal(Triangle t, float4x4 worldMatrix)
{
    StructuredBuffer<Vertex> Vertices = ResourceDescriptorHeap[Settings.VerticesIndex];
    
    float3 a = Vertices[t.VertexIndex1].Position;
    float3 b = Vertices[t.VertexIndex2].Position;
    float3 c = Vertices[t.VertexIndex3].Position;
    
    return normalMul(normalize(cross(b - a, c - a)), worldMatrix);
}

float3 rayDirection(float3 origin, float3 normal, inout uint seed)
{
    StructuredBuffer<LightSource> Lights = ResourceDescriptorHeap[Settings.LightsIndex];
    
    uint numStructs;
    uint stride;
    Lights.GetDimensions(numStructs, stride);
    
    float3 defaultDirection = normalize(normal + sphericalToCartesian(randomSpherical(1, seed)));
    
    SampledLight lights[numLights];
    initialLights(lights, defaultDirection);
    
    for (uint i = 0; i < numStructs; i++)
    {
        LightSource light = Lights[i];
        
        if (light.VerticesIndex == 0)
        {
            addLight(sampleSphereLight(seed, origin, normal, light.Power, light.Position, light.Size, light.DistanceIndependent), lights);
        }        
    }
    
    float targetPower = uniformRand(seed) * totalPower(lights);
    float powerSoFar = 0;
    
    for (uint i = 0; i < numLights; i++)
    {
        SampledLight light = lights[i];
        
        float power = light.power;
        if (power <= 0)
            continue;
        
        powerSoFar += power;
        
        if (powerSoFar > targetPower)
        {
            return light.direction;
        }
    }
    
    return defaultDirection;
}

float3 trianglePosition(Triangle t, float2 barry, float4x4 worldMatrix)
{
    StructuredBuffer<Vertex> Vertices = ResourceDescriptorHeap[Settings.VerticesIndex];
    
    float3 a = Vertices[t.VertexIndex1].Position;
    float3 b = Vertices[t.VertexIndex2].Position;
    float3 c = Vertices[t.VertexIndex3].Position;
    
    float3 position = barrypolate(barycentric(barry), a, b, c);    
    
    return mul(float4(position, 1), worldMatrix).xyz;
}

[shader("closesthit")]
void ObjectRadianceClosestHit(inout RadiancePayload payload, TriangleAttributes attrib)
{
    StructuredBuffer<Triangle> Triangles = ResourceDescriptorHeap[Settings.TrianglesIndex];    
    StructuredBuffer<Material> Materials = ResourceDescriptorHeap[Settings.MaterialsIndex];
    RaytracingAccelerationStructure SceneBVH = ResourceDescriptorHeap[Settings.TLASIndex];
    
    Triangle t = Triangles[PrimitiveIndex()];         
    
    uint2 index = DispatchRaysIndex().xy;
    
    float3 face = faceNormal(t, Settings.WorldMatrix);
    float3 trianglePos = trianglePosition(t, attrib.bary, Settings.WorldMatrix);
    float3 startPosition = WorldRayOrigin() + (RayTCurrent() * WorldRayDirection());
    float3 normal = alignWith(-WorldRayDirection(), face);
    
    uint seed = Settings.Seed * (index.x + 1) * (index.y + 1);
    
    Material m = Materials[t.MaterialIndex];       
    
    float3 incomingLight = float3(0, 0, 0);
    
    int samples = payload.Depth == 1 ? Settings.MaxSamples : 1;
    
    if (payload.Depth < Settings.MaxBounces)
    {
        for (int i = 0; i < samples; ++i)
        {        
            RadiancePayload newPayload;
            newPayload.IncomingLight = float3(0, 0, 0);
            newPayload.Depth = payload.Depth + 1;
        
            float3 direction = rayDirection(startPosition, normal, seed);
        
            RayDesc ray;
            ray.Origin = startPosition;
            ray.Direction = direction;
            ray.TMin = 0.01;
            ray.TMax = 10000;
    
            TraceRay(
                SceneBVH,
                0,
                0xFF,
                0,
                0,
                0,
                ray,
                newPayload);
            
            incomingLight += newPayload.IncomingLight;
        }
    }
    
    incomingLight = (incomingLight / samples);
    
    payload.IncomingLight += min((incomingLight * m.Colour) + (m.EmissionStrength * m.EmissionColour), float3(1, 1, 1));
}
