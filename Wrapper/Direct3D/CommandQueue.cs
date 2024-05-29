namespace Wrapper.Direct3D
{
    public class CommandQueue : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly Queue<SharpDX.Direct3D12.GraphicsCommandList> commandLists = new Queue<SharpDX.Direct3D12.GraphicsCommandList>();
        private readonly Queue<CommandAllocatorEntry> commandAllocators = new Queue<CommandAllocatorEntry>();
        private readonly object queueLock = new object();

        protected readonly SharpDX.Direct3D12.Fence fence;
        protected readonly Device device;
        protected readonly SharpDX.Direct3D12.CommandQueue queue;

        private long fenceValue = 0;

        internal CommandQueue(Device device, SharpDX.Direct3D12.CommandQueue queue)
        {
            this.device = device;
            this.queue = tracker.Track(queue);
            this.fence = tracker.Track(device.Native.CreateFence(0, SharpDX.Direct3D12.FenceFlags.None));
        }

        public FenceWait Wait()
        {
            return new FenceWait(fence, fenceValue + 1);
        }

        public FenceWait Flush()
        {
            return new FenceWait(fence, Signal());
        }

        internal (SharpDX.Direct3D12.GraphicsCommandList, SharpDX.Direct3D12.CommandAllocator) GetCommandList()
        {
            lock (queueLock)
            {
                var allocator = DequeueAllocator();

                if (!commandLists.TryDequeue(out var commandList))
                {
                    commandList = tracker.Track(device.Native.CreateCommandList(queue.Description.Type, allocator, null));
                }
                else
                {
                    commandList.Reset(allocator, null);
                }

                return (commandList, allocator);
            }
        }

        internal FenceWait Execute(SharpDX.Direct3D12.GraphicsCommandList? list, SharpDX.Direct3D12.CommandAllocator? allocator)
        {
            if (list == null || allocator == null) return new FenceWait(fence, 0);

            list.Close();

            queue.ExecuteCommandList(list);
            var waitValue = Signal();

            lock (queueLock)
            {
                commandAllocators.Enqueue(new CommandAllocatorEntry
                {
                    Allocator = allocator,
                    FenceId = waitValue,
                });
                commandLists.Enqueue(list);
            }

            return new FenceWait(fence, waitValue);
        }

        private long Signal()
        {
            var waitValue = Interlocked.Increment(ref fenceValue);
            queue.Signal(fence, waitValue);
            return waitValue;
        }

        private SharpDX.Direct3D12.CommandAllocator DequeueAllocator()
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

            return tracker.Track(device.Native.CreateCommandAllocator(queue.Description.Type));
        }

        public void Dispose()
        {
            tracker.Dispose();
        }

        private class CommandAllocatorEntry
        {
            public long FenceId { get; set; }
            public SharpDX.Direct3D12.CommandAllocator Allocator { get; set; }
        }
    }
}
