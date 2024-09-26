#include "../Structured.hlsl"
#include "../Power.hlsl"
#include "../GBuffer.hlsl"

struct AtrousRootParameters
{
    uint InputDataIndex;
    uint InputIlluminanceTextureIndex;
    uint OutputIlluminanceTextureIndex;
    uint InputVarianceTextureIndex;
    uint OutputVarianceTextureIndex;
    
    uint ImageHeight;
    uint ImageWidth;
    int StepWidth;
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

struct FilterOutput
{
    float4 Illuminance;
    float4 Variance;
};

float luminance(float4 rgba)
{
    return 0.299 * rgba.r + 0.587 * rgba.g + 0.114 * rgba.b;
}

float4 gaussian(RWTexture2D<float4> input, int2 location)
{
    float4 kernelSum =
        input[location + int2(-1, -1)] +
        2 * input[location + int2(0, -1)] +
        input[location = int2(1, -1)] +
        2 * input[location + int2(-1, 0)] +
        4 * input[location] +
        2 * input[location + int2(1, 0)] +
        input[location + int2(-1, 1)] +
        2 * input[location + int2(0, 1)] +
        input[location + int2(1, 1)];
    
    return kernelSum / 16;
}

float2 calculateDepthGradient(RWTexture2D<uint2> data, int2 location)
{
    AtrousData pixelData = unpackAtrous(data[location]);
    
    float2 gradient = float2(0, 0);
    
    for (int i = -1; i < 1; i++)
    {
        for (int j = -1; j < 1; j++)
        {
            AtrousData kernelData = unpackAtrous(data[location + int2(i, j)]);
            if (!filterData(kernelData))
                continue;
            
            gradient += int2(i, j) * (kernelData.Depth - pixelData.Depth);
        }
    }
    
    return normalize(gradient);
}

FilterOutput atrous(int2 dimensions, int2 location, int stepwidth, RWTexture2D<float4> illuminance, RWTexture2D<float4> variance, RWTexture2D<uint2> data)
{    
    const float epsilon = (1 / 1000.0f);
    const float kernel[5][5] =
    {
        { 1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256 },
        { 1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64 },
        { 3.0f / 128, 3.0f / 32, 9.0f / 64, 3.0f / 32, 3.0f / 128 },
        { 1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64 },
        { 1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256 }
    };
        
    
    AtrousData pixelData = unpackAtrous(data[location]);    
    float4 gaussianVariance = gaussian(variance, location);
    float2 depthGradient = calculateDepthGradient(data, location);
    float3 pixelNormal = directionToCartesian(pixelData.Normal);
    
    float4 illuminanceSum = float4(0, 0, 0, 0);
    float4 illuminanceWeightSum = 0.0;
    
    float4 varianceSum = float4(0, 0, 0, 0);
    float varianceWeightSum = 0;
    
    for (int i = 0; i < 5; i++)
    {
        for (int j = 0; j < 5; j++)
        {
            int2 kernelLocation = location + (int2(i - 2, j - 2) * stepwidth);
            
            if (any(kernelLocation < int2(0, 0)) || any(kernelLocation >= dimensions))
                continue;
            
            AtrousData kernelData = unpackAtrous(data[kernelLocation]);
            
            if (!filterData(kernelData))
                continue;
            
            float depthNumerator = abs(pixelData.Depth - kernelData.Depth);
            float depthDenominator = abs(dot(depthGradient, float2(location - kernelLocation))) + epsilon;
            float depthWeight = 1; // exp(-(depthNumerator / depthDenominator));
            
            float3 kernelNormal = directionToCartesian(kernelData.Normal);
            
            float angle = dot(pixelNormal, kernelNormal);
            
            float normalWeight = zeroSafePow(max(angle, 0), 128);
            
            float4 luminanceNumerator = abs(illuminance[location] - illuminance[kernelLocation]);
            float4 luminanceDenominator = 4 * sqrt(gaussianVariance) + epsilon;
            float4 luminanceWeight = exp(-(luminanceNumerator / luminanceDenominator));
            
            float4 weight = luminanceWeight * normalWeight * depthWeight;
            
            illuminanceSum += kernel[i][j] * weight * illuminance[kernelLocation];
            varianceSum += pow2(kernel[i][j]) * pow2(weight) * variance[kernelLocation];
            
            varianceWeightSum += weight * kernel[i][j];
            illuminanceWeightSum += weight * kernel[i][j];
        }
    }
    
    FilterOutput output;
    output.Illuminance = illuminanceSum / illuminanceWeightSum;
    output.Variance = varianceSum / pow2(varianceWeightSum);
    return output;
}

[numthreads(32, 32, 1)]
void compute(int2 id : SV_DispatchThreadID)
{
    RWTexture2D<uint2> inputData = ResourceDescriptorHeap[Parameters.InputDataIndex];
    RWTexture2D<float4> inputIlluminance = ResourceDescriptorHeap[Parameters.InputIlluminanceTextureIndex];
    RWTexture2D<float4> outputIlluminance = ResourceDescriptorHeap[Parameters.OutputIlluminanceTextureIndex];
    RWTexture2D<float4> inputVariance = ResourceDescriptorHeap[Parameters.InputVarianceTextureIndex];
    RWTexture2D<float4> outputVariance = ResourceDescriptorHeap[Parameters.OutputVarianceTextureIndex];
    
    int2 dimensions = int2(Parameters.ImageWidth, Parameters.ImageHeight);
    
    if (any(id >= dimensions))
        return;
        
    if (!filterData(unpackAtrous(inputData[id])))
    {
        outputIlluminance[id] = inputIlluminance[id];
        outputVariance[id] = inputVariance[id];
    }
    else
    {
        FilterOutput output = atrous(dimensions, id, Parameters.StepWidth, inputIlluminance, inputVariance, inputData);
        outputIlluminance[id] = output.Illuminance;
        outputVariance[id] = output.Variance;
    }
}
