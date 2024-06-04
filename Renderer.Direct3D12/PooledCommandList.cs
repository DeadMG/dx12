using System.Numerics;
using Util;

namespace Renderer.Direct3D12
{
    public class PooledCommandList
    {
        private readonly CommandListPool pool;
        private readonly SharpDX.Direct3D12.GraphicsCommandList commandList;
        private readonly SharpDX.Direct3D12.CommandAllocator commandAllocator;

        public PooledCommandList(CommandListPool pool, SharpDX.Direct3D12.GraphicsCommandList list, SharpDX.Direct3D12.CommandAllocator allocator )
        {
            this.pool = pool;
            this.commandList = list;
            this.commandAllocator = allocator;
        }
        
        public SharpDX.Direct3D12.GraphicsCommandList List => commandList;
        public CommandListPool Pool => pool;

        public FenceWait Execute() => pool.Execute(commandList, commandAllocator);

        // Yep; no need for the caller to wait on disposal of this resource
        public async void UploadData<T>(SharpDX.Direct3D12.Resource resource, T[] data)
            where T : unmanaged
        {
            using (var tempResource = CreateUploadBuffer(data))
            {
                commandList.CopyResource(resource, tempResource);
                commandList.ResourceBarrier(new SharpDX.Direct3D12.ResourceBarrier(new SharpDX.Direct3D12.ResourceTransitionBarrier(resource, 0, SharpDX.Direct3D12.ResourceStates.CopyDestination, SharpDX.Direct3D12.ResourceStates.Common)));

                await pool.Wait().AsTask();
            }
        }

        public SharpDX.Direct3D12.Resource CreateUploadBuffer<T>(T[] data)
            where T : unmanaged
        {
            var tempResource = pool.Device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Upload),
                    SharpDX.Direct3D12.HeapFlags.None,
                    SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = data.SizeOf() }),
                    SharpDX.Direct3D12.ResourceStates.GenericRead);

            UploadDataToBuffer(tempResource, data);

            return tempResource;
        }

        private static unsafe void UploadDataToBuffer<T>(SharpDX.Direct3D12.Resource resource, T[] data)
            where T : unmanaged
        {
            fixed (T* dataPtr = data)
            {
                var destPtr = (Matrix4x4*)resource.Map(0);
                Buffer.MemoryCopy(dataPtr, destPtr, data.SizeOf(), data.SizeOf());
                resource.Unmap(0);
            }
        }
    }
}
