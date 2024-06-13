using System.Numerics;
using Util;

namespace Renderer.Direct3D12
{
    public class PooledCommandList
    {
        private readonly CommandListPool pool;
        private readonly Vortice.Direct3D12.ID3D12GraphicsCommandList commandList;
        private readonly Vortice.Direct3D12.ID3D12CommandAllocator commandAllocator;

        public PooledCommandList(CommandListPool pool, Vortice.Direct3D12.ID3D12GraphicsCommandList list, Vortice.Direct3D12.ID3D12CommandAllocator allocator )
        {
            this.pool = pool;
            this.commandList = list;
            this.commandAllocator = allocator;
        }
        
        public Vortice.Direct3D12.ID3D12GraphicsCommandList List => commandList;
        public CommandListPool Pool => pool;

        public FenceWait Execute() => pool.Execute(commandList, commandAllocator);

        // Yep; no need for the caller to wait on disposal of this resource
        public async void UploadData<T>(Vortice.Direct3D12.ID3D12Resource resource, T[] data)
            where T : unmanaged
        {
            using (var tempResource = CreateUploadBuffer(data))
            {
                commandList.CopyResource(resource, tempResource);
                commandList.ResourceBarrier(new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(resource, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.Common)));

                await pool.Wait().AsTask();
            }
        }

        public Vortice.Direct3D12.ID3D12Resource CreateUploadBuffer<T>(T[] data)
            where T : unmanaged
        {
            var tempResource = pool.Device.CreateCommittedResource(new Vortice.Direct3D12.HeapProperties(Vortice.Direct3D12.HeapType.Upload),
                    Vortice.Direct3D12.HeapFlags.None,
                    Vortice.Direct3D12.ResourceDescription.Buffer(new Vortice.Direct3D12.ResourceAllocationInfo { Alignment = 65536, SizeInBytes = data.SizeOf() }),
                    Vortice.Direct3D12.ResourceStates.GenericRead);

            UploadDataToBuffer(tempResource, data);

            return tempResource;
        }

        private static unsafe void UploadDataToBuffer<T>(Vortice.Direct3D12.ID3D12Resource resource, T[] data)
            where T : unmanaged
        {
            fixed (T* dataPtr = data)
            {
                var destPtr = resource.Map<Matrix4x4>(0);
                Buffer.MemoryCopy(dataPtr, destPtr, data.SizeOf(), data.SizeOf());
                resource.Unmap(0);
            }
        }
    }
}
