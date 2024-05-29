namespace Wrapper.Direct3D
{
    public class RenderTargetView
    {
        private readonly SharpDX.Direct3D12.Resource buffer;
        private readonly SharpDX.Direct3D12.CpuDescriptorHandle descriptorHandle;

        internal RenderTargetView(SharpDX.Direct3D12.Resource buffer, SharpDX.Direct3D12.CpuDescriptorHandle descriptorHandle)
        {
            this.descriptorHandle = descriptorHandle;
            this.buffer = buffer;
        }

        public SharpDX.Direct3D12.Resource Native => buffer;
        public SharpDX.Direct3D12.CpuDescriptorHandle Handle => descriptorHandle;
        internal SharpDX.DXGI.Format Format => buffer.Description.Format;
    }
}
