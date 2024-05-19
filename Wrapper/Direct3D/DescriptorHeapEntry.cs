namespace Wrapper.Direct3D
{
    public class DescriptorHeapEntry : IDisposable
    {
        private readonly DescriptorHeapPool pool;
        private readonly DescriptorHeapSlot slot;

        internal DescriptorHeapEntry(DescriptorHeapPool pool, DescriptorHeapSlot slot)
        {
            this.pool = pool;
            this.slot = slot;
        }

        public SharpDX.Direct3D12.CpuDescriptorHandle Descriptor => slot.CpuDescriptorHandle;

        public void Dispose()
        {
            pool.Return(slot);            
        }
    }
}
