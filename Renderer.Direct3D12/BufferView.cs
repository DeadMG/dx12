namespace Renderer.Direct3D12
{
    internal sealed record class BufferView(Vortice.Direct3D12.ID3D12Resource Resource, uint FirstElement, uint NumElements, uint ElementSize)
    {
        public Vortice.Direct3D12.BufferShaderResourceView SRV => new Vortice.Direct3D12.BufferShaderResourceView { FirstElement = FirstElement, NumElements = NumElements, StructureByteStride = ElementSize };
        public uint StartOffset => FirstElement * ElementSize;
        public uint Size = NumElements * ElementSize;
        public ulong GPUVirtualAddress => Resource.GPUVirtualAddress + StartOffset;
    }
}
