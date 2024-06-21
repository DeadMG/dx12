using System.Numerics;
using Util;

namespace Renderer.Direct3D12
{
    internal class PooledCommandList
    {
        private readonly CommandListPool pool;
        private readonly Vortice.Direct3D12.ID3D12GraphicsCommandList4 commandList;
        private readonly Vortice.Direct3D12.ID3D12CommandAllocator commandAllocator;

        public PooledCommandList(CommandListPool pool, Vortice.Direct3D12.ID3D12GraphicsCommandList4 list, Vortice.Direct3D12.ID3D12CommandAllocator allocator )
        {
            this.pool = pool;
            this.commandList = list;
            this.commandAllocator = allocator;
        }
        
        public Vortice.Direct3D12.ID3D12GraphicsCommandList4 List => commandList;
        public CommandListPool Pool => pool;

        public FenceWait Execute() => pool.Execute(commandList, commandAllocator);

        public T DisposeAfterExecution<T>(T resource)
            where T : IDisposable
        {
            DisposeAfterExecutionAsync(resource);
            return resource;
        }

        private async void DisposeAfterExecutionAsync(IDisposable resource)
        {
            using (resource)
            {
                await pool.Wait().AsTask();
            }
        }

        // Yep; no need for the caller to wait on disposal of this resource
        public void UploadData<T>(Vortice.Direct3D12.ID3D12Resource resource, IReadOnlyList<T> data)
            where T : unmanaged
        {
            var tempResource = DisposeAfterExecution(CreateUploadBuffer(data));
            commandList.CopyResource(resource, tempResource);
            commandList.ResourceBarrier(new Vortice.Direct3D12.ResourceBarrier(new Vortice.Direct3D12.ResourceTransitionBarrier(resource, Vortice.Direct3D12.ResourceStates.CopyDest, Vortice.Direct3D12.ResourceStates.Common)));
        }

        public Vortice.Direct3D12.ID3D12Resource CreateUploadBuffer<T>(IReadOnlyList<T> data, uint alignment = 1)
            where T : unmanaged
        {
            var size = alignment == 1 ? data.SizeOf() : data.SizeOf().Align(alignment);
            var tempResource = pool.Device.CreateCommittedResource(new Vortice.Direct3D12.HeapProperties(Vortice.Direct3D12.HeapType.Upload),
                    Vortice.Direct3D12.HeapFlags.None,
                    Vortice.Direct3D12.ResourceDescription.Buffer(new Vortice.Direct3D12.ResourceAllocationInfo { Alignment = 65536, SizeInBytes = size }),
                    Vortice.Direct3D12.ResourceStates.GenericRead);

            var destSpan = tempResource.Map<T>(0, (int)data.SizeOf());
            for (int i = 0; i < data.Count; i++)
            {
                destSpan[i] = data[i];
            }
            tempResource.Unmap(0);

            return tempResource;
        }
    }
}
