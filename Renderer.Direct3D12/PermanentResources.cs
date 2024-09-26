using Util;

namespace Renderer.Direct3D12
{
    class PermanentResources : IDisposable
    {
        private readonly DisposeTracker tracker = new DisposeTracker();

        private readonly Vortice.Direct3D12.ID3D12Device10 device;
        private readonly Vortice.Direct3D12.ID3D12GraphicsCommandList7 commandList;
        private readonly ReadonlyPool readonlyPool;
        private readonly BLASPool blasPool;

        public PermanentResources(Vortice.Direct3D12.ID3D12Device10 device)
        {
            this.device = device;
            commandList = tracker.Track(device.CreateCommandList1<Vortice.Direct3D12.ID3D12GraphicsCommandList7>(Vortice.Direct3D12.CommandListType.Direct));
            readonlyPool = tracker.Track(new ReadonlyPool(device));
            blasPool = tracker.Track(new BLASPool(device));            
        }

        public MapResourceCache MapResourceCache { get; } = new MapResourceCache();
        public MeshResourceCache MeshResourceCache { get; } = new MeshResourceCache();
        public PrimitiveBlasCache PrimitiveBlasCache { get; } = new PrimitiveBlasCache();

        public Vortice.Direct3D12.ID3D12GraphicsCommandList7 CommandList => commandList;
        public BLASPool BLASPool => blasPool;
        public Vortice.Direct3D12.ID3D12Device10 Device => device;

        public BufferView UploadReadonly<T>(FrameUploadBufferPool temporaryPool, T[] items)
            where T : unmanaged
        {
            return UploadReadonly(temporaryPool, new ReadOnlySpan<T>(items));
        }

        public BufferView UploadReadonly<T>(FrameUploadBufferPool temporaryPool, ReadOnlySpan<T> items)
            where T : unmanaged
        {
            var uploadBuffer = temporaryPool.AllocateFor(items);
            uploadBuffer.Resource.SetData(items, (int)uploadBuffer.StartOffset);
            var defaultBuffer = readonlyPool.AllocateFor(items);
            commandList.CopyBufferRegion(defaultBuffer.Resource, defaultBuffer.StartOffset, uploadBuffer.Resource, uploadBuffer.StartOffset, uploadBuffer.Size);
            return defaultBuffer;
        }

        public void Dispose() => tracker.Dispose();
    }

    class ReadonlyPool : BufferPool
    {
        public ReadonlyPool(Vortice.Direct3D12.ID3D12Device10 device) 
            : base(device, 10 * 1024 * 1024, Vortice.Direct3D12.HeapType.Default, Vortice.Direct3D12.ResourceFlags.None, "Permanent readonly")
        {
        }
    }

    class BLASPool : RTASPool
    {
        public BLASPool(Vortice.Direct3D12.ID3D12Device10 device)
            : base(device, 10 * 1024 * 1024, "Permanent BLAS")
        {
        }
    }
}
