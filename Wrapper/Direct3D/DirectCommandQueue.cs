using Data;

namespace Wrapper.Direct3D
{
    public class DirectCommandQueue : CopyCommandQueue
    {
        private readonly SharpDX.DXGI.Factory5 factory;

        internal DirectCommandQueue(SharpDX.DXGI.Factory5 factory, Device device, SharpDX.Direct3D12.CommandQueue queue)
            : base(device, queue)
        {            
            this.factory = factory;
        }

        public new DirectCommandList CreateCommandList()
        {
            return new DirectCommandList(device, this);
        }

        public SwapChain CreateSwapChain(IntPtr hWnd, int backBuffers, ScreenSize size)
        {
            var description = new SharpDX.DXGI.SwapChainDescription1
            {
                Width = Math.Max(size.Width, 1),
                Height = Math.Max(size.Height, 1),
                BufferCount = backBuffers,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipDiscard,
                AlphaMode = SharpDX.DXGI.AlphaMode.Unspecified,
                Flags = 0,
            };

            return new SwapChain(device.Native, queue, new SharpDX.DXGI.SwapChain1(factory, queue, hWnd, ref description).QueryInterface<SharpDX.DXGI.SwapChain3>());
        }
    }
}
