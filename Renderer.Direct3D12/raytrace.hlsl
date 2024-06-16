// Hit information, aka ray payload
// This sample only carries a shading color and hit distance.
// Note that the payload should be kept as small as possible,
// and that its size must be declared in the corresponding
// D3D12_RAYTRACING_SHADER_CONFIG pipeline subobjet.
struct HitInfo
{
    float4 colorAndDistance;
};

// Attributes output by the raytracing when hitting a surface,
// here the barycentric coordinates
struct Attributes
{
    float2 bary;
};


// Raytracing output texture, accessed as a UAV
RWTexture2D<float4> output : register(u0);

// Raytracing acceleration structure, accessed as a SRV
RaytracingAccelerationStructure SceneBVH : register(t0);

struct CameraMatrices
{
    float4x4 InverseView;
    float4x4 InverseProjection;
    float3 Origin;
};

// Camera matrices
ConstantBuffer<CameraMatrices> Camera : register(b0);

[shader("raygeneration")]
void RayGen()
{
    // Initialize the ray payload
    HitInfo payload;
    payload.colorAndDistance = float4(0, 0, 0, 0);

    // Get the location within the dispatched 2D grid of work items
    // (often maps to pixels, so this could represent a pixel coordinate).
    uint2 launchIndex = DispatchRaysIndex().xy;
    float2 dims = float2(DispatchRaysDimensions().xy);
    float2 d = ((launchIndex.xy / dims.xy) * 2.f) - 1.f;
    
   
    RayDesc ray;
    ray.Origin = Camera.Origin;
    float4 target = mul(Camera.InverseView, mul(Camera.InverseProjection, float4(d.x, -d.y, 1, 1)));
    target = target / target.w;
    ray.Direction = normalize(target.xyz);
    ray.TMin = 0;
    ray.TMax = 1000;
    
    TraceRay(
        SceneBVH,
        0,
        0xFF,
        0,
        0,
        0,
        ray,
        payload);

    output[launchIndex] = float4(payload.colorAndDistance.rgb, 1.f);
}

[shader("miss")]
void Miss(inout HitInfo payload : SV_RayPayload)
{    
    payload.colorAndDistance = float4(0.0f, 0.0f, 0.0f, -1.0f);
}
