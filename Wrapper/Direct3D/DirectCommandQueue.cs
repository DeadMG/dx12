using System.Runtime.InteropServices;

namespace Wrapper.Direct3D
{
    public class DirectCommandQueue : CopyCommandQueue
    {
        private readonly SharpDX.DXGI.Factory5 factory;
        private readonly bool allowTearing;

        internal DirectCommandQueue(SharpDX.DXGI.Factory5 factory, Device device, SharpDX.Direct3D12.CommandQueue queue)
            : base(device, queue)
        {            
            this.factory = factory;
            allowTearing = AllowTearing();
        }

        public new DirectCommandList CreateCommandList()
        {
            return new DirectCommandList(device, this);
        }

        public SwapChain CreateSwapChain(IntPtr hWnd, int backBuffers, int width, int height)
        {
            var description = new SharpDX.DXGI.SwapChainDescription1
            {
                Width = width,
                Height = height,
                BufferCount = backBuffers,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription { Count = 1, Quality = 0 },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipDiscard,
                AlphaMode = SharpDX.DXGI.AlphaMode.Unspecified,
                Flags = allowTearing ? SharpDX.DXGI.SwapChainFlags.AllowTearing : 0,
            };

            return new SwapChain(device.Native, new SharpDX.DXGI.SwapChain1(factory, queue, hWnd, ref description).QueryInterface<SharpDX.DXGI.SwapChain3>(), allowTearing);
        }

        private unsafe bool AllowTearing()
        {
            uint allowTearing = 0;
            factory.CheckFeatureSupport(SharpDX.DXGI.Feature.PresentAllowTearing, new IntPtr((void*)&allowTearing), Marshal.SizeOf<uint>());
            return allowTearing == 1;
        }
    }
}
