namespace Wrapper.Direct3D
{
    public class UploadResource : Resource
    {
        public UploadResource(SharpDX.Direct3D12.Resource resource) : base(resource)
        {
        }

        public unsafe void Upload<T>(T[] data)
            where T : unmanaged
        {
            fixed (T* dataPtr = data)
            {
                var destPtr = (T*)resource.Map(0);
                Buffer.MemoryCopy(dataPtr, destPtr, data.SizeOf(), data.SizeOf());
                resource.Unmap(0);
            }
        }
    }
}
