#include "../Structured.hlsl"
#include "../Power.hlsl"

struct FilterParameters
{
    uint ImageWidth;
    uint ImageHeight;
        
    uint CurrentFrameIndex;
    uint CurrentDataIndex;
    uint PreviousFrameIndex;
};

ConstantBuffer<FilterParameters> Parameters : register(b0);

[numthreads(32, 32, 1)]
void compute(int2 id: SV_DispatchThreadID)
{
    StructuredBuffer<RaytracingOutputData> data = ResourceDescriptorHeap[Parameters.CurrentDataIndex];
    RWTexture2D<float4> output = ResourceDescriptorHeap[Parameters.CurrentFrameIndex];
    RWTexture2D<float4> input = ResourceDescriptorHeap[Parameters.PreviousFrameIndex];    
    
    int2 dimensions = int2(Parameters.ImageWidth, Parameters.ImageHeight);
    
    if (any(id >= dimensions))
        return;
    
    int dataIndex = index(id, Parameters.ImageWidth);
    
    if (!data[dataIndex].Filter)
    {
        output[id] = input[id];
    }
    else
    {    
        output[id] = lerp(output[id], input[id], 0.2);
    }
}
