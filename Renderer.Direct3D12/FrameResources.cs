using Util;

namespace Renderer.Direct3D12
{
    internal class FrameResources : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();
        private readonly DescriptorHeapAccumulator heapAccumulator;
        private readonly Vortice.Direct3D12.ID3D12CommandAllocator directCommandAllocator;
        private readonly FrameUploadBufferPool uploadBufferPool;
        private readonly PermanentResources resources;
        private readonly FrameTLASPool frameTlasPool;
        private readonly FrameUnorderedAccessPool frameUnorderedAccessPool;

        public FrameResources(PermanentResources resources)
        {
            this.resources = resources;
            frameUnorderedAccessPool = disposeTracker.Track(new FrameUnorderedAccessPool(Permanent.Device));
            frameTlasPool = disposeTracker.Track(new FrameTLASPool(Permanent.Device));
            heapAccumulator = disposeTracker.Track(new DescriptorHeapAccumulator(Permanent.Device));
            directCommandAllocator = disposeTracker.Track(Permanent.Device.CreateCommandAllocator(Vortice.Direct3D12.CommandListType.Direct)).Name("Frame direct allocator");
            uploadBufferPool = disposeTracker.Track(new FrameUploadBufferPool(Permanent.Device));
        }

        public FrameResources Reset()
        {
            HeapAccumulator.Reset();
            directCommandAllocator.Reset();
            uploadBufferPool.Reset();
            frameTlasPool.Reset();
            frameUnorderedAccessPool.Reset();
            Permanent.CommandList.Reset(DirectCommandAllocator);
            return this;
        }

        public PermanentResources Permanent => resources;
        public DescriptorHeapAccumulator HeapAccumulator => heapAccumulator;
        public Vortice.Direct3D12.ID3D12CommandAllocator DirectCommandAllocator => directCommandAllocator;
        public FrameUploadBufferPool UploadBufferPool => uploadBufferPool;
        public FrameTLASPool FrameTLASPool => frameTlasPool;

        public BufferView TransferToUpload<T>(T[] items, uint? alignment = null)
            where T : unmanaged
        {
            return TransferToUpload(new ReadOnlySpan<T>(items), alignment);
        }

        public BufferView TransferToUpload<T>(ReadOnlySpan<T> items, uint? alignment = null)
            where T : unmanaged
        {
            var uploadBuffer = uploadBufferPool.AllocateFor(items, alignment);
            uploadBuffer.Resource.SetData(items, (int)uploadBuffer.StartOffset);
            return uploadBuffer;
        }

        public BufferView TransferToUnorderedAccess<T>(T[] items, uint? alignment = null)
            where T : unmanaged
        {
            return TransferToUnorderedAccess(new ReadOnlySpan<T>(items), alignment);
        }

        public BufferView TransferToUnorderedAccess<T>(ReadOnlySpan<T> items, uint? alignment = null)
            where T : unmanaged
        {
            var buffer = TransferToUpload<T>(items, alignment);
            var uaBuffer = frameUnorderedAccessPool.AllocateFor(items, alignment);
            Permanent.CommandList.CopyBufferRegion(uaBuffer.Resource, uaBuffer.StartOffset, buffer.Resource, buffer.StartOffset, buffer.Size);
            return uaBuffer;
        }

        public BufferView BuildAS(RTASPool asPool, Vortice.Direct3D12.BuildRaytracingAccelerationStructureInputs asDesc)
        {
            var prebuild = Permanent.Device.GetRaytracingAccelerationStructurePrebuildInfo(asDesc);
            var scratch = frameUnorderedAccessPool.Allocate(256, (uint)prebuild.ScratchDataSizeInBytes, 1);
            var result = asPool.Allocate(256, (uint)prebuild.ResultDataMaxSizeInBytes, 1);

            Permanent.CommandList.BuildRaytracingAccelerationStructure(new Vortice.Direct3D12.BuildRaytracingAccelerationStructureDescription
            {
                DestinationAccelerationStructureData = result.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress,
                Inputs = asDesc,
                SourceAccelerationStructureData = 0,
            });

            Permanent.CommandList.Barrier(new Vortice.Direct3D12.BarrierGroup([
                new Vortice.Direct3D12.BufferBarrier
                {
                    Resource = result.Resource,
                    Offset = 0,
                    Size = result.Resource.Description.Width,
                    SyncBefore = Vortice.Direct3D12.BarrierSync.BuildRaytracingAccelerationStructure,
                    SyncAfter = Vortice.Direct3D12.BarrierSync.Raytracing |
                        Vortice.Direct3D12.BarrierSync.BuildRaytracingAccelerationStructure,
                    AccessBefore = Vortice.Direct3D12.BarrierAccess.RaytracingAccelerationStructureWrite,
                    AccessAfter = Vortice.Direct3D12.BarrierAccess.RaytracingAccelerationStructureWrite |
                        Vortice.Direct3D12.BarrierAccess.RaytracingAccelerationStructureRead,
                }
            ]));

            return result;
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
        }
    }

    internal class FrameUploadBufferPool : BufferPool
    {
        public FrameUploadBufferPool(Vortice.Direct3D12.ID3D12Device10 device) : base(device, 10 * 1024 * 1024, Vortice.Direct3D12.HeapType.Upload, Vortice.Direct3D12.ResourceFlags.None, "Frame upload buffer pool")
        {
        }
    }

    internal class FrameUnorderedAccessPool : BufferPool
    {
        public FrameUnorderedAccessPool(Vortice.Direct3D12.ID3D12Device10 device) : base(device, 10 * 1024 * 1024, Vortice.Direct3D12.HeapType.Default, Vortice.Direct3D12.ResourceFlags.AllowUnorderedAccess, "Frame unordered access pool")
        {
        }
    }

    internal class FrameTLASPool : RTASPool
    {
        public FrameTLASPool(Vortice.Direct3D12.ID3D12Device10 device) : base(device, 10 * 1024 * 1024, "Frame TLAS pool")
        {
        }
    }
}
