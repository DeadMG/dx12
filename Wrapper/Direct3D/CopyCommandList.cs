namespace Wrapper.Direct3D
{
    public class CopyCommandList : CommandList
    {
        private readonly Device device;

        internal CopyCommandList(Device device, CommandQueue queue) : base(queue)
        {
            this.device = device;
        }

        public async Task UploadData<T>(Resource resource, T[] data)
            where T : unmanaged
        {
            using (var tempResource = device.CreateUploadBuffer(data.SizeOf()))
            {
                tempResource.Upload(data);

                List.CopyResource(resource.Native, tempResource.Native);

                await queue.Wait().AsTask();
            }
        }
    }
}
