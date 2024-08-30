#include "../Raytrace/Structured.hlsl"

StructuredBuffer<RaytracingOutputData> Data : PixExt_SelectedResourceRegister;

[numthreads(32, 1, 1)]
void main(int2 id : SV_DispatchThreadID)
{
    uint numstructs;
    uint stride;
    Data.GetDimensions(numstructs, stride);
    
    uint dataIndex = index(id, 1920);
    if (dataIndex >= numstructs)
        return;
    
    PixExt_StorePixel_Float(id, float4(Data[dataIndex].Normal, 1));
}
