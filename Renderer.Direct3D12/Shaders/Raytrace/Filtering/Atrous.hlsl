#include "../Structured.hlsl"
#include "../Power.hlsl"

struct AtrousRootParameters
{
    uint InputDataIndex;
    uint InputTextureIndex;
    uint OutputTextureIndex;
    
    uint ImageHeight;
    uint ImageWidth;
    int StepWidth;
    
    float CPhi;
    float NPhi;
};

ConstantBuffer<AtrousRootParameters> Parameters : register(b0);

float selfDot(float3 vec)
{
    return dot(vec, vec);
}

float selfDot(float4 vec)
{
    return dot(vec, vec);
}

float4 atrous(int2 dimensions, int2 location, int stepwidth, float cPhi, float nPhi, RWTexture2D<float4> input, RWStructuredBuffer<RaytracingOutputData> data)
{    
    const float kernel[5][5] =
    {
        { 1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256 },
        { 1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64 },
        { 3.0f / 128, 3.0f / 32, 9.0f / 64, 3.0f / 32, 3.0f / 128 },
        { 1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64 },
        { 1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256 }
    };
        
    float4 sum = float4(0, 0, 0, 0);
    
    RaytracingOutputData pixelData = data[index(location, dimensions.x)];
    
    float cumulativeWeight = 0.0;
    
    for (int i = 0; i < 5; i++)
    {
        for (int j = 0; j < 5; j++)
        {
            int2 kernelLocation = location + (int2(i - 2, j - 2) * stepwidth);
            
            if (any(kernelLocation < int2(0, 0)) || any(kernelLocation >= dimensions))
                continue;
            
            RaytracingOutputData kernelData = data[index(kernelLocation, dimensions.x)];
            
            if (!filterData(kernelData))
                continue;
            
            float colourWeight = min(exp(-(selfDot(input[location] - input[kernelLocation])) / cPhi), 1.0);
        
            float normalDistance = max(selfDot(directionToCartesian(pixelData.Normal) - directionToCartesian(kernelData.Normal)) / (stepwidth * stepwidth), 0.0);
            float normalWeight = min(exp(-(normalDistance) / nPhi), 1.0);        
       
            float weight = colourWeight * normalWeight;
            
            sum += input[kernelLocation] * weight * kernel[i][j];
            cumulativeWeight += weight * kernel[i][j];
        }
    }
    
    return sum / cumulativeWeight;
}

[numthreads(32, 32, 1)]
void compute(int2 id : SV_DispatchThreadID)
{
    RWStructuredBuffer<RaytracingOutputData> inputData = ResourceDescriptorHeap[Parameters.InputDataIndex];
    RWTexture2D<float4> input = ResourceDescriptorHeap[Parameters.InputTextureIndex];
    RWTexture2D<float4> output = ResourceDescriptorHeap[Parameters.OutputTextureIndex];
    
    int2 dimensions = int2(Parameters.ImageWidth, Parameters.ImageHeight);
    
    if (any(id >= dimensions))
        return;
    
    int dataIndex = index(id, Parameters.ImageWidth);
    
    if (!filterData(inputData[dataIndex]))
    {
        output[id] = input[id];
    }
    else
    {
        output[id] = atrous(dimensions, id, Parameters.StepWidth, Parameters.CPhi, Parameters.NPhi, input, inputData);
    }
}
