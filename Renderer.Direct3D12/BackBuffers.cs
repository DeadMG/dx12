using Util;

namespace Renderer.Direct3D12
{
    internal class BackBuffers : IDisposable
    {
        private readonly DisposeTracker disposeTracker = new DisposeTracker();

        public readonly List<Vortice.Direct3D12.ID3D12Resource> backBuffers = new List<Vortice.Direct3D12.ID3D12Resource>();
        public readonly List<Vortice.Direct3D11.ID3D11Resource> wrappedResources = new List<Vortice.Direct3D11.ID3D11Resource>();
        public readonly List<Vortice.Direct2D1.ID2D1Bitmap> d2dRenderTargets = new List<Vortice.Direct2D1.ID2D1Bitmap>();

        private readonly Vortice.Direct3D11.ID3D11DeviceContext? immediateContext;

        public BackBuffers(bool D3D11on12,
            Vortice.DXGI.IDXGISwapChain3 swapChain,
            Vortice.Direct3D11on12.ID3D11On12Device? on12,
            Vortice.Direct2D1.ID2D1DeviceContext? deviceContext,
            Vortice.Direct3D11.ID3D11DeviceContext? immediateContext)
        {
            this.immediateContext = immediateContext;

            for (int i = 0; i < swapChain.Description1.BufferCount; ++i)
            {
                var backBuffer = disposeTracker.Track(swapChain.GetBuffer<Vortice.Direct3D12.ID3D12Resource>(i).Name($"Back buffer {i}"));

                backBuffers.Add(backBuffer);

                if (D3D11on12)
                {
                    var resource11 = disposeTracker.Track(on12.CreateWrappedResource<Vortice.Direct3D11.ID3D11Resource>(backBuffer,
                        new Vortice.Direct3D11on12.ResourceFlags { BindFlags = Vortice.Direct3D11.BindFlags.RenderTarget },
                        Vortice.Direct3D12.ResourceStates.RenderTarget,
                        Vortice.Direct3D12.ResourceStates.Present)
                        .Name($"11on12 back buffer {i}"));

                    wrappedResources.Add(resource11);

                    using (var surface = resource11.QueryInterface<Vortice.DXGI.IDXGISurface>())
                    {
                        d2dRenderTargets.Add(disposeTracker.Track(deviceContext.CreateBitmapFromDxgiSurface(surface, new Vortice.Direct2D1.BitmapProperties1
                        {
                            BitmapOptions = Vortice.Direct2D1.BitmapOptions.Target | Vortice.Direct2D1.BitmapOptions.CannotDraw,
                            PixelFormat = new Vortice.DCommon.PixelFormat { AlphaMode = Vortice.DCommon.AlphaMode.Premultiplied, Format = swapChain.Description1.Format }
                        })));
                    }
                }
            }
        }

        public void Dispose()
        {
            disposeTracker.Dispose();
            immediateContext?.Flush();
        }
    }
}
