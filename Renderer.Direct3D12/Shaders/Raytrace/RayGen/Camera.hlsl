#include "../Ray.hlsl"
#include "../Structured.hlsl"

struct CameraMatrices
{
    float3 WorldTopLeft;
    float3 WorldTopRight;
    float3 WorldBottomLeft;
    
    float3 Origin;
    
    uint DataIndex;
    uint OutputIndex;
    uint PreviousIndex;
    uint SceneBVHIndex;
};

// Camera matrices
ConstantBuffer<CameraMatrices> Camera : register(b0);

[shader("raygeneration")]
void RayGen()
{
// Raytracing output texture, accessed as a UAV
    RWTexture2D<float4> output = ResourceDescriptorHeap[Camera.OutputIndex];

// Bilteral filter texture as UAV
    RWTexture2D<float4> previousFilter = ResourceDescriptorHeap[Camera.PreviousIndex];

// Raytracing acceleration structure, accessed as a SRV
    RaytracingAccelerationStructure SceneBVH = ResourceDescriptorHeap[Camera.SceneBVHIndex];
    
    RWStructuredBuffer<RaytracingOutputData> data = ResourceDescriptorHeap[Camera.DataIndex];
    
    // Get the location within the dispatched 2D grid of work items
    // (often maps to pixels, so this could represent a pixel coordinate).
    uint2 launchIndex = DispatchRaysIndex().xy;
    // 0.5f puts us in the center of that pixel
    float2 pixelCoordinate = launchIndex.xy + 0.5f;
    // The given dimensions are the absolute maximum of the frustum; they are on the boundaries. There's 1 more boundary than there is pixels.
    float2 dims = float2(DispatchRaysDimensions().xy) + 1.0f;
    
    float3 top = (Camera.WorldTopRight - Camera.WorldTopLeft) / dims.x;
    float3 down = (Camera.WorldBottomLeft - Camera.WorldTopLeft) / dims.y;
    
    float3 target = Camera.WorldTopLeft + (top * pixelCoordinate.x) + (down * pixelCoordinate.y);    
    
    float3 tracedColour = float3(0, 0, 0);
    
    // Initialize the ray payload
    RadiancePayload payload;
    payload.IncomingLight = float3(0, 0, 0);
    payload.Depth = 1;
    payload.Filter = true;
    
    RayDesc ray;
    ray.Origin = Camera.Origin;
    ray.Direction = normalize(target - Camera.Origin);
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
        payload);
    
    float3 thisFrame = lerp(payload.IncomingLight, previousFilter[launchIndex].rgb, float3(0.7, 0.7, 0.7));
    RaytracingOutputData outputData;
    outputData.Filter = payload.Filter;
    
    int dataIndex = index(launchIndex, DispatchRaysDimensions().x);
    data[dataIndex] = outputData;
    output[launchIndex] = float4(thisFrame, 1.0f);
}
