namespace Wrapper.Direct3D
{
    internal class DescriptorHeapSlot
    {
        public required SharpDX.Direct3D12.DescriptorHeap Heap { get; init; }
        public required SharpDX.Direct3D12.CpuDescriptorHandle CpuDescriptorHandle { get; init; }
    }
}
