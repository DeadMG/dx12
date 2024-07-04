struct FilterParameters
{
    uint KernelWidth;
    uint KernelHeight;
    
    uint ImageWidth;
    uint ImageHeight;
    
    float SigmaD;
    float SigmaR;
};

ConstantBuffer<FilterParameters> Parameters : register(b0);

// Raytracing output texture, accessed as a UAV
RWTexture2D<float4> input : register(u0);

// Filter output texture as UAV
RWTexture2D<float4> output : register(u1);

float w(int2 target, float originalIntensity, int2 kernel, float newIntensity)
{
    float first = (pow(target.x - kernel.x, 2) + pow(target.y - kernel.y, 2)) / Parameters.SigmaD;
    float second = pow(originalIntensity - newIntensity, 2) / Parameters.SigmaR;
    
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
    float weightSum = 0;
    float3 weightIntensitySum = 0;
    
    float3 originalColour = input[pixel].rgb;
    float originalIntensity = length(originalColour);
    
    for (int xo = -Parameters.KernelWidth; xo <= Parameters.KernelWidth; ++xo)
    {
        for (int yo = -Parameters.KernelHeight; yo <= Parameters.KernelHeight; ++yo)
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
    if (id.x >= Parameters.ImageWidth) return;
    if (id.y >= Parameters.ImageHeight) return;
    
    output[id] = float4(filter(id), input[id].a);
}
