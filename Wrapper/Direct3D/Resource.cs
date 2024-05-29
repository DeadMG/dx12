namespace Wrapper.Direct3D
{
    public class Resource : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        protected readonly SharpDX.Direct3D12.Resource resource;

        public Resource(SharpDX.Direct3D12.Resource resource)
        {
            this.resource = tracker.Track(resource);
        }

        internal SharpDX.Direct3D12.Resource Native => resource;

        public long GPUHandle => Native.GPUVirtualAddress;

        public void Dispose()
        {
            tracker.Dispose();
        }
    }
}
