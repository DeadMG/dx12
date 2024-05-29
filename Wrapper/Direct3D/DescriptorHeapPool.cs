using System.Collections.Concurrent;

namespace Wrapper.Direct3D
{
    class DescriptorHeapPool : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly SharpDX.Direct3D12.Device device;
        private readonly ConcurrentQueue<DescriptorHeapSlot> availableSlots = new ConcurrentQueue<DescriptorHeapSlot>();
        private readonly SharpDX.Direct3D12.DescriptorHeapDescription desc;

        internal DescriptorHeapPool(SharpDX.Direct3D12.Device device, SharpDX.Direct3D12.DescriptorHeapDescription desc)
        {
            this.device = device;
            this.desc = desc;
        }

        public DescriptorHeapEntry GetSlot()
        {
            if (availableSlots.TryDequeue(out var slot))
            {
                return new DescriptorHeapEntry(this, slot);
            }

            var heap = tracker.Track(device.CreateDescriptorHeap(desc));

            var startHandle = heap.CPUDescriptorHandleForHeapStart;
            var size = device.GetDescriptorHandleIncrementSize(desc.Type);
            for (int i = 1; i < desc.DescriptorCount; i++)
            {
                availableSlots.Enqueue(new DescriptorHeapSlot { CpuDescriptorHandle = startHandle + (size * i), Heap = heap });
            }

            return new DescriptorHeapEntry(this, new DescriptorHeapSlot { CpuDescriptorHandle = startHandle, Heap = heap });
        }

        internal void Return(DescriptorHeapSlot slot)
        {
            availableSlots.Enqueue(slot);
        }

        public void Dispose()
        {
            tracker.Dispose();
        }
    }
}
