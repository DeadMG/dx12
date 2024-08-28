namespace Renderer.Direct3D12
{
    internal sealed record class StructuredBuffer(Vortice.Direct3D12.ID3D12Resource Buffer, Vortice.Direct3D12.BufferShaderResourceView SRV)
    {
        public StructuredBuffer Name(string name)
        {
            Buffer.Name = name;
            return this;
        }
    }
}
