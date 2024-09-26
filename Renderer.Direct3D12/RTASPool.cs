namespace Renderer.Direct3D12
{
    internal class RTASPool : BufferPool
    {
        public RTASPool(Vortice.Direct3D12.ID3D12Device10 device, uint bufferSize, string name) : base(device, bufferSize, Vortice.Direct3D12.HeapType.Default, Vortice.Direct3D12.ResourceFlags.RaytracingAccelerationStructure, name)
        {
        }

        public BufferView AllocateAS(uint size)
        {
            return Allocate(256, size, 1);
        }
    }
}
