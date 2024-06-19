// Hit information, aka ray payload
// This sample only carries a shading color and hit distance.
// Note that the payload should be kept as small as possible,
// and that its size must be declared in the corresponding
// D3D12_RAYTRACING_SHADER_CONFIG pipeline subobjet.
struct HitInfo
{
    float4 colorAndDistance;
};

struct Attributes
{
    float2 bary;
};

struct SunColour
{
    float3 hitColor;
};

ConstantBuffer<SunColour> Sun : register(b0);

[shader("closesthit")]
void ClosestSunHit(inout HitInfo payload, Attributes attr)
{
    payload.colorAndDistance = float4(Sun.hitColor, RayTCurrent());
}
