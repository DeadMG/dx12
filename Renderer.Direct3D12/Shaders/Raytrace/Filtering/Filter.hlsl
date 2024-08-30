#include "../Structured.hlsl"
#include "../Power.hlsl"

struct FilterParameters
{
    uint ImageWidth;
    uint ImageHeight;
        
    uint InputDataIndex;
    uint InputTextureIndex;
    uint OutputTextureIndex;
};

ConstantBuffer<FilterParameters> Parameters : register(b0);

[numthreads(32, 32, 1)]
void compute(int2 id: SV_DispatchThreadID)
{
    RWStructuredBuffer<RaytracingOutputData> inputData = ResourceDescriptorHeap[Parameters.InputDataIndex];
    RWTexture2D<float4> input = ResourceDescriptorHeap[Parameters.InputTextureIndex];
    RWTexture2D<float4> output = ResourceDescriptorHeap[Parameters.OutputTextureIndex];
    
    int2 dimensions = int2(Parameters.ImageWidth, Parameters.ImageHeight);
    
    if (any(id >= dimensions))
        return;
    
    RaytracingOutputData rtData = inputData[index(id, Parameters.ImageWidth)];
    
    output[id] = input[id] * asFloat(rtData.Albedo) + asFloat(rtData.Emission);
}
