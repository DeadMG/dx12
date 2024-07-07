#include "../Structured.hlsl"

static const int sigmaD = 2;
static const int sigmaR = 1;

struct FilterParameters
{
    uint ImageWidth;
    uint ImageHeight;
        
    uint InputIndex;
    uint DataIndex;
    uint OutputIndex;
};

ConstantBuffer<FilterParameters> Parameters : register(b0);

float w(int2 target, float originalIntensity, int2 kernel, float newIntensity)
{
    float first = (pow(target.x - kernel.x, 2) + pow(target.y - kernel.y, 2)) / (2 * pow(sigmaD, 2));
    float second = pow(originalIntensity - newIntensity, 2) / (2 * pow(sigmaR, 2));
    
    return exp(-first - second);
}

float saturation(float3 colour)
{
    float cMax = max(colour.r, max(colour.g, colour.b));
    float cMin = min(colour.r, min(colour.g, colour.b));
    float d = cMax - cMin;
    if (d == 0)
        return 0;
    return d / cMax;
}

float3 filter(int2 pixel)
{
    RWTexture2D<float4> input = ResourceDescriptorHeap[Parameters.InputIndex];
    
    float weightSum = 0;
    float3 weightIntensitySum = float3(0, 0, 0);
    
    float3 originalColour = input[pixel].rgb;
    float originalIntensity = length(originalColour);
    
    [unroll]
    for (int xo = -sigmaD; xo <= sigmaD; ++xo)
    {
        [unroll]
        for (int yo = -sigmaD; yo <= sigmaD; ++yo)
        {
            if (xo != 0 && yo != 0)
            {                
                int2 kernel = pixel + int2(xo, yo);
                
                float3 newColour = input[kernel].rgb;
                float newIntensity = length(newColour);
            
                float weight = w(pixel, originalIntensity, kernel, newIntensity);
                weightSum += weight;
                weightIntensitySum += newColour * weight;
            }
        }
    }

    return weightIntensitySum / weightSum;
}

[numthreads(32, 32, 1)]
void compute(int2 id: SV_DispatchThreadID)
{
    StructuredBuffer<RaytracingOutputData> data = ResourceDescriptorHeap[Parameters.DataIndex];
    RWTexture2D<float4> output = ResourceDescriptorHeap[Parameters.OutputIndex];
    RWTexture2D<float4> input = ResourceDescriptorHeap[Parameters.InputIndex];
    
    if (id.x >= Parameters.ImageWidth) return;
    if (id.y >= Parameters.ImageHeight) return;
    
    int dataIndex = index(id, Parameters.ImageWidth);
    
    if (!data[dataIndex].Filter)
    {
        output[id] = input[id];
    }
    else
    {    
        output[id] = float4(filter(id), 1.0f);        
    }
}
