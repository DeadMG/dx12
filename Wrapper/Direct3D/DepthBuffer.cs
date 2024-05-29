using Data;

namespace Wrapper.Direct3D
{
    public class DepthBuffer : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();
        private readonly SharpDX.Direct3D12.Device device;
        private readonly DescriptorHeapEntry heapEntry;
        private SharpDX.Direct3D12.Resource depthBuffer;

        public DepthBuffer(SharpDX.Direct3D12.Device device, DescriptorHeapEntry heapEntry, ScreenSize size)
        {
            this.device = device;
            this.heapEntry = tracker.Track(heapEntry);

            depthBuffer = Create(size);
        }

        public Resource AsResource => new Resource(depthBuffer);

        public SharpDX.Direct3D12.CpuDescriptorHandle Handle => heapEntry.Descriptor;

        public void Resize(ScreenSize size)
        {
            depthBuffer.Dispose();
            depthBuffer = Create(size);
        }

        private SharpDX.Direct3D12.Resource Create(ScreenSize size)
        {
             var resource = device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Default),
                SharpDX.Direct3D12.HeapFlags.None,
                new SharpDX.Direct3D12.ResourceDescription(SharpDX.Direct3D12.ResourceDimension.Texture2D, 0, size.Width, size.Height, 1, 0, SharpDX.DXGI.Format.D32_Float, 1, 0, SharpDX.Direct3D12.TextureLayout.Unknown, SharpDX.Direct3D12.ResourceFlags.AllowDepthStencil),
                SharpDX.Direct3D12.ResourceStates.DepthWrite,
                new SharpDX.Direct3D12.ClearValue { DepthStencil = new SharpDX.Direct3D12.DepthStencilValue { Depth = 1.0f, Stencil = 0 }, Format = SharpDX.DXGI.Format.D32_Float });

            device.CreateDepthStencilView(resource, new SharpDX.Direct3D12.DepthStencilViewDescription { Format = SharpDX.DXGI.Format.D32_Float, Dimension = SharpDX.Direct3D12.DepthStencilViewDimension.Texture2D, Flags = SharpDX.Direct3D12.DepthStencilViewFlags.None }, Handle);

            return resource;
        }

        public void Dispose()
        {
            depthBuffer.Dispose();
            tracker.Dispose();
        }
    }
}
