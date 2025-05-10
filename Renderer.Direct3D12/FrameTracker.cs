using Util;

namespace Renderer.Direct3D12
{
    internal class FrameTracker : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly Queue<FrameResourcesEntry> frames = new Queue<FrameResourcesEntry>();
        private readonly object queueLock = new object();
        private ulong fenceValue = 0;

        private readonly PermanentResources resources;
        private readonly Vortice.Direct3D12.ID3D12Fence fence;
        private readonly Vortice.Direct3D12.ID3D12CommandQueue directQueue;

        public FrameTracker(PermanentResources resources, Vortice.Direct3D12.ID3D12CommandQueue directQueue, Vortice.Direct3D12.ID3D12Fence fence)
        {
            this.resources = resources;
            this.fence = fence;
            this.directQueue = directQueue;
        }

        public FrameLease Get()
        {
            return new FrameLease { Resources = GetResources(), Tracker = this };
        }

        private FrameResources GetResources()
        {
            lock (queueLock)
            {
                if (frames.TryPeek(out var next))
                {
                    if (fence.CompletedValue >= next.FenceId)
                    {
                        return next.Resources.Reset();
                    }
                }

                return tracker.Track(new FrameResources(resources).Reset());
            }
        }

        public void Return(FrameResources resources)
        {
            var waitValue = Interlocked.Increment(ref fenceValue);
            directQueue.Signal(fence, waitValue);

            lock (queueLock)
            {
                frames.Enqueue(new FrameResourcesEntry(waitValue, resources));
            }
        }

        public void Dispose() => tracker.Dispose();

        public FenceWait Wait() => new FenceWait(fence, fenceValue);

        private record class FrameResourcesEntry(ulong FenceId, FrameResources Resources)
        {
        }
    }

    internal class FrameLease : IDisposable
    {
        public required FrameResources Resources { get; init; }
        public required FrameTracker Tracker { get; init; }

        public void Dispose() => Tracker.Return(Resources);
    }
}
