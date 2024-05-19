namespace Wrapper.Direct3D
{
    public class CopyCommandQueue : CommandQueue
    {
        internal CopyCommandQueue(SharpDX.Direct3D12.Device device, SharpDX.Direct3D12.CommandQueue queue) : base(device, queue)
        {
        }

        public CopyCommandList CreateCommandList()
        {
            return new CopyCommandList(device, this);
        }
    }
}
