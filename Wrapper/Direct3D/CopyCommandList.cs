using System.Runtime.InteropServices;

namespace Wrapper.Direct3D
{
    public class CopyCommandList : CommandList
    {
        private readonly List<GCHandle> list = new List<GCHandle>();
        private readonly SharpDX.Direct3D12.Device device;

        internal CopyCommandList(SharpDX.Direct3D12.Device device, CommandQueue queue) : base(queue)
        {
            this.device = device;
        }

        public unsafe Resource CreateResource<T>(T[] data)
            where T : unmanaged
        {
            var size = Marshal.SizeOf<T>() * data.Length;

            var finalResource = device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Default),
                SharpDX.Direct3D12.HeapFlags.None,
                SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = size }),
                SharpDX.Direct3D12.ResourceStates.Common);

            var tempResource = device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Upload),
                SharpDX.Direct3D12.HeapFlags.None,
                SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = size }),
                SharpDX.Direct3D12.ResourceStates.GenericRead);

            fixed (T* dataPtr = data)
            {
                var destPtr = (T*)tempResource.Map(0);
                Buffer.MemoryCopy(dataPtr, destPtr, size, size);
                tempResource.Unmap(0);
            }

            list.Add(GCHandle.Alloc(tempResource, GCHandleType.Normal));
            List.CopyResource(finalResource, tempResource);

            return new Resource(finalResource);
        }
    }
}
