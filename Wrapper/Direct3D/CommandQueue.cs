namespace Wrapper.Direct3D
{
    public class CommandQueue
    {
        private readonly Queue<SharpDX.Direct3D12.GraphicsCommandList> commandLists = new Queue<SharpDX.Direct3D12.GraphicsCommandList>();
        private readonly Queue<CommandAllocatorEntry> commandAllocators = new Queue<CommandAllocatorEntry>();
        private readonly object queueLock = new object();

        protected readonly SharpDX.Direct3D12.Fence fence;
        protected readonly SharpDX.Direct3D12.Device device;
        protected readonly SharpDX.Direct3D12.CommandQueue queue;

        private long fenceValue = 0;

        internal CommandQueue(SharpDX.Direct3D12.Device device, SharpDX.Direct3D12.CommandQueue queue)
        {
            this.device = device;
            this.queue = queue;

            this.fence = device.CreateFence(0, SharpDX.Direct3D12.FenceFlags.None);
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
                    commandList = device.CreateCommandList(queue.Description.Type, allocator, null);
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

            return device.CreateCommandAllocator(queue.Description.Type);
        }

        public void Dispose()
        {
            foreach (var list in commandLists)
            {
                list.Dispose();
            }

            foreach (var entry in commandAllocators)
            {
                entry.Allocator.Dispose();
            }

            fence.Dispose();
            queue.Dispose();
        }

        private class CommandAllocatorEntry
        {
            public long FenceId { get; set; }
            public SharpDX.Direct3D12.CommandAllocator Allocator { get; set; }
        }
    }
}
