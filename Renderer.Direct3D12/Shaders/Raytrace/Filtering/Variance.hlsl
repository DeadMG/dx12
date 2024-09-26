#include "../Structured.hlsl"
#include "../Power.hlsl"
#include "../GBuffer.hlsl"

struct VarianceRootParameters
{
    uint ImageWidth;
    uint ImageHeight;
        
    uint AtrousDataIndex;
    uint IlluminanceTextureIndex;
    uint VarianceTextureIndex;
    uint MeanTextureIndex;
    uint StdDevTextureIndex;
};

static const int kernelSize = 7;
float4 meanKernel(RWTexture2D<float4> illuminance, RWTexture2D<uint2> atrousData, int2 dimensions, int2 location)
{
    float4 value = 0;
    int totalPixels = 0;
    
    AtrousData data = unpackAtrous(atrousData[location]);
                
    int loopSize = (kernelSize - 1) / 2;
    for (int i = -kernelSize; i < kernelSize; i++)
    {
        for (int j = -kernelSize; j < kernelSize; j++)
        {
            int2 kernelLocation = location + int2(i, j);
            
            AtrousData kernelData = unpackAtrous(atrousData[kernelLocation]);
            
            if (!filterData(kernelData))
                continue;
            
            if (any(directionToCartesian(kernelData.Normal) != directionToCartesian(data.Normal)))
                continue;
            
            totalPixels += 1;
            value += illuminance[kernelLocation];
        }
    }
    
    return value / totalPixels;
}

float4 stdDevKernel(RWTexture2D<float4> illuminance, RWTexture2D<uint2> atrousData, float4 mean, int2 dimensions, int2 location)
{
    float4 value = 0;
    int totalPixels = 0;
    
    AtrousData data = unpackAtrous(atrousData[location]);
    
    int loopSize = (kernelSize - 1) / 2;
    for (int i = -kernelSize; i < kernelSize; i++)
    {
        for (int j = -kernelSize; j < kernelSize; j++)
        {
            int2 kernelLocation = location + int2(i, j);
            
            AtrousData kernelData = unpackAtrous(atrousData[kernelLocation]);
            
            if (!filterData(kernelData))
                continue;
            
            if (any(directionToCartesian(kernelData.Normal) != directionToCartesian(data.Normal)))
                continue;
            
            totalPixels += 1;
            value += pow2(illuminance[kernelLocation] - mean);
        }
    }
    
    return sqrt(value / totalPixels);
}

ConstantBuffer<VarianceRootParameters> Parameters : register(b0);

[numthreads(32, 32, 1)]
void compute(int2 id : SV_DispatchThreadID)
{
    RWTexture2D<uint2> atrousData = ResourceDescriptorHeap[Parameters.AtrousDataIndex];
    RWTexture2D<float4> illuminance = ResourceDescriptorHeap[Parameters.IlluminanceTextureIndex];
    RWTexture2D<float4> variance = ResourceDescriptorHeap[Parameters.VarianceTextureIndex];
    RWTexture2D<float4> meanTexture = ResourceDescriptorHeap[Parameters.MeanTextureIndex];
    RWTexture2D<float4> stdDevTexture = ResourceDescriptorHeap[Parameters.StdDevTextureIndex];
    
    int2 dimensions = int2(Parameters.ImageWidth, Parameters.ImageHeight);
    
    if (any(id >= dimensions))
        return;
            
    if (!filterData(unpackAtrous(atrousData[id])))
    {
        meanTexture[id] = float4(0, 0, 0, 0);
        stdDevTexture[id] = float4(0, 0, 0, 0);
        variance[id] = float4(0, 0, 0, 0);
        return;
    }
    
    float4 mean = meanKernel(illuminance, atrousData, dimensions, id);
    
    meanTexture[id] = mean = lerp(meanTexture[id], mean, 0.2);
    //stdDevTexture[id] = stddev = lerp(stdDevTexture[id], stddev, 0.2);

    variance[id] = stdDevKernel(illuminance, atrousData, mean, dimensions, id);
}
