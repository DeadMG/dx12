namespace Wrapper.Direct3D
{
    public class RenderTargetView : IDisposable
    {
        private readonly Resource buffer;
        private readonly SharpDX.DXGI.Format format;
        private readonly SharpDX.Direct3D12.CpuDescriptorHandle descriptorHandle;

        internal RenderTargetView(SharpDX.Direct3D12.Resource buffer, SharpDX.DXGI.Format format, SharpDX.Direct3D12.CpuDescriptorHandle descriptorHandle)
        {
            this.descriptorHandle = descriptorHandle;
            this.format = format;
            this.buffer = new Resource(buffer);
        }

        public SharpDX.Direct3D12.CpuDescriptorHandle Handle => descriptorHandle;
        internal SharpDX.DXGI.Format Format => format;

        public Resource AsResource => buffer;

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
