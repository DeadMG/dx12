#include "../Structured.hlsl"
#include "../Power.hlsl"

struct AtrousRootParameters
{
    uint InputTextureIndex;
    uint InputDataIndex;
    uint OutputTextureIndex;
    uint ImageHeight;
    uint ImageWidth;
    int StepWidth;
    
    float CPhi;
    float NPhi;
    float PPhi;
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

float4 atrous(int2 dimensions, int2 location, int stepwidth, float cPhi, float nPhi, float pPhi, Texture2D<float4> colour, StructuredBuffer<RaytracingOutputData> data)
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

    float4 pixelColour = colour[location];
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
            
            if (!kernelData.Filter)
                continue;
            
            float4 kernelColour = colour[kernelLocation];
            
            float colourWeight = min(exp(-(selfDot(pixelColour - kernelColour)) / cPhi), 1.0);
        
            float normalDistance = max(selfDot(pixelData.Normal - kernelData.Normal) / (stepwidth * stepwidth), 0.0);
            float normalWeight = min(exp(-(normalDistance) / nPhi), 1.0);
        
            float positionWeight = min(exp(-(selfDot(pixelData.Position - kernelData.Position)) / pPhi), 1.0);
        
            float weight = colourWeight * normalWeight * positionWeight;
            
            sum += kernelColour * weight * kernel[i][j];
            cumulativeWeight += weight * kernel[i][j];
        }
    }
    
    return sum / cumulativeWeight;
}

/*
uniform sampler2D colorMap, normalMap, posMap;
uniform float c_phi, n_phi, p_phi, stepwidth;
uniform float kernel[25];
uniform vec2 offset[25];

void main(void) {
    vec4 sum = vec4(0.0);
    vec2 step = vec2(1./512., 1./512.); // resolution
    vec4 cval = texture2D(colorMap, gl_TexCoord[0].st);
    vec4 nval = texture2D(normalMap, gl_TexCoord[0].st);
    vec4 pval = texture2D(posMap, gl_TexCoord[0].st);
    float cum_w = 0.0;
    for(int i = 0; i < 25; i++) {
        vec2 uv = gl_TexCoord[0].st + offset[i]*step*stepwidth;
        vec4 ctmp = texture2D(colorMap, uv);
        vec4 t = cval - ctmp;
        float dist2 = dot(t,t);
        float c_w = min(exp(-(dist2)/c_phi), 1.0);
        vec4 ntmp = texture2D(normalMap, uv);
        t = nval - ntmp;
        dist2 = max(dot(t,t)/(stepwidth*stepwidth),0.0);
        float n_w = min(exp(-(dist2)/n_phi), 1.0);
        vec4 ptmp = texture2D(posMap, uv);
        t = pval - ptmp;
        dist2 = dot(t,t);
        float p_w = min(exp(-(dist2)/p_phi),1.0);
        float weight = c_w * n_w * p_w;
        sum += ctmp * weight * kernel[i];
        cum_w += weight*kernel[i];
    }
    gl_FragData[0] = sum/cum_w;
}
*/

[numthreads(32, 32, 1)]
void compute(int2 id : SV_DispatchThreadID)
{
    StructuredBuffer<RaytracingOutputData> data = ResourceDescriptorHeap[Parameters.InputDataIndex];
    RWTexture2D<float4> output = ResourceDescriptorHeap[Parameters.OutputTextureIndex];
    Texture2D<float4> input = ResourceDescriptorHeap[Parameters.InputTextureIndex];
    
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
        output[id] = atrous(dimensions, id, Parameters.StepWidth, Parameters.CPhi, Parameters.NPhi, Parameters.PPhi, input, data);
    }
}