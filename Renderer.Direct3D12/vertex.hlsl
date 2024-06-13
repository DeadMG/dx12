struct VertexPosColor
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float3 Color : COLOR;
    float4x4 ModelMatrix : INSTANCE_TRANSFORM;
};

struct ModelViewProjection
{
    matrix VP;
};

struct VertexShaderOutput
{
    float4 Color : COLOR;
    float4 Position : SV_Position;
};

ConstantBuffer<ModelViewProjection> ModelViewProjectionCB : register(b0);

VertexShaderOutput main(VertexPosColor IN)
{
    VertexShaderOutput OUT;
    
    OUT.Position = mul(ModelViewProjectionCB.VP, mul(IN.ModelMatrix, float4(IN.Position, 1.0f)));
    OUT.Color = float4(IN.Color, 1.0f);
 
    return OUT;
}
