using Util;

namespace Renderer.Direct3D12
{
    internal class CommandListPool : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly Queue<Vortice.Direct3D12.ID3D12GraphicsCommandList4> commandLists = new Queue<Vortice.Direct3D12.ID3D12GraphicsCommandList4>();
        private readonly Queue<CommandAllocatorEntry> commandAllocators = new Queue<CommandAllocatorEntry>();
        private readonly object queueLock = new object();

        protected readonly Vortice.Direct3D12.ID3D12Fence fence;
        protected readonly Vortice.Direct3D12.ID3D12Device5 device;
        protected readonly Vortice.Direct3D12.ID3D12CommandQueue queue;

        private ulong fenceValue = 0;

        internal CommandListPool(Vortice.Direct3D12.ID3D12Device5 device, Vortice.Direct3D12.ID3D12CommandQueue queue)
        {
            this.device = device;
            this.queue = tracker.Track(queue);
            this.fence = tracker.Track(device.CreateFence(0, Vortice.Direct3D12.FenceFlags.None).Name($"Fence for {queue.Name}"));
        }

        public Vortice.Direct3D12.ID3D12CommandQueue Queue => queue;
        public Vortice.Direct3D12.ID3D12Device5 Device => device;

        public FenceWait Wait()
        {
            return new FenceWait(fence, fenceValue + 1);
        }

        public FenceWait Flush()
        {
            return new FenceWait(fence, Signal());
        }

        public PooledCommandList GetCommandList()
        {
            lock (queueLock)
            {
                var allocator = DequeueAllocator();

                if (!commandLists.TryDequeue(out var commandList))
                {
                    commandList = tracker.Track(device.CreateCommandList<Vortice.Direct3D12.ID3D12GraphicsCommandList4>(queue.GetDescription().Type, allocator, null).Name($"Command list for {queue.Name}"));
                }
                else
                {
                    commandList.Reset(allocator, null);
                }

                return new PooledCommandList(this, commandList, allocator);
            }
        }

        public FenceWait Execute(Vortice.Direct3D12.ID3D12GraphicsCommandList4 list, Vortice.Direct3D12.ID3D12CommandAllocator allocator)
        {
            list.Close();

            queue.ExecuteCommandList(list);
            var waitValue = Signal();

            lock (queueLock)
            {
                commandAllocators.Enqueue(new CommandAllocatorEntry(waitValue, allocator));
                commandLists.Enqueue(list);
            }

            return new FenceWait(fence, waitValue);
        }

        private ulong Signal()
        {
            var waitValue = Interlocked.Increment(ref fenceValue);
            queue.Signal(fence, waitValue);
            return waitValue;
        }

        private Vortice.Direct3D12.ID3D12CommandAllocator DequeueAllocator()
        {
            if (commandAllocators.TryPeek(out var entry))
            {
                if (fence.CompletedValue >= entry.FenceId)
                {
                    commandAllocators.Dequeue();
                    entry.Allocator.Reset();
                    return entry.Allocator;
                }
            }

            return tracker.Track(device.CreateCommandAllocator(queue.GetDescription().Type).Name($"Command allocator for {queue.Name}"));
        }

        public void Dispose()
        {
            tracker.Dispose();
        }

        private record class CommandAllocatorEntry(ulong FenceId, Vortice.Direct3D12.ID3D12CommandAllocator Allocator)
        {
        }
    }
}
